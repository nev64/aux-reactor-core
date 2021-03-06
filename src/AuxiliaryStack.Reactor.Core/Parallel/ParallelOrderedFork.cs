﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Parallel
{
    internal sealed class ParallelOrderedFork<T> : ParallelOrderedFlux<T>
    {
        private readonly IPublisher<T> _source;
        private readonly int _parallelism;
        private readonly int _prefetch;

        internal ParallelOrderedFork(IPublisher<T> source, int parallelism, int prefetch)
        {
            _source = source;
            _parallelism = parallelism;
            _prefetch = prefetch;
        }

        public override int Parallelism => _parallelism;

        public override void SubscribeMany(ISubscriber<IOrderedItem<T>>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }

            _source.Subscribe(new OrderedDispatcher(subscribers, _prefetch));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class OrderedDispatcher : ISubscriber<T>
        {
            readonly ISubscriber<IOrderedItem<T>>[] subscribers;

            readonly long[] requests;

            readonly long[] emissions;

            readonly int prefetch;

            readonly int limit;

            ISubscription s;

            IFlow<T> _flow;

            bool done;
            Exception error;

            bool cancelled;

            int produced;

            private FusionMode _sourceMode;

            int index;

            int subscriberCount;

            long id;

            Pad128 p0;

            int wip;

            Pad120 p1;

            internal OrderedDispatcher(ISubscriber<IOrderedItem<T>>[] subscribers, int prefetch)
            {
                this.subscribers = subscribers;
                int n = subscribers.Length;
                this.requests = new long[n];
                this.emissions = new long[n];
                this.prefetch = prefetch;
                this.limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription subscription)
            {
                if (SubscriptionHelper.Validate(ref this.s, subscription))
                {
                    if (subscription is IFlowSubscription<T> flow)
                    {
                        var mode = flow.RequestFusion(FusionMode.Any);

                        if (mode == FusionMode.Sync)
                        {
                            _sourceMode = mode;
                            _flow = flow;
                            Volatile.Write(ref done, true);
                            SetupSubscribers();
                            Drain();
                            return;
                        }
                        if (mode == FusionMode.Sync)
                        {
                            _sourceMode = mode;
                            _flow = flow;
                            SetupSubscribers();
                            subscription.Request(prefetch < 0 ? long.MaxValue : prefetch);
                            return;
                        }
                    }

                    _flow = QueueDrainHelper.CreateQueue<T>(prefetch);

                    SetupSubscribers();

                    subscription.Request(prefetch < 0 ? long.MaxValue : prefetch);
                }
            }

            void SetupSubscribers()
            {
                var array = subscribers;
                int n = array.Length;

                for (int i = 0; i < n; i++)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        return;
                    }
                    Volatile.Write(ref subscriberCount, i + 1);

                    array[i].OnSubscribe(new RailSubscription(this, i));
                }
            }

            internal void Cancel()
            {
                Volatile.Write(ref cancelled, true);
                s.Cancel();
                if (QueueDrainHelper.Enter(ref wip))
                {
                    _flow.Clear();
                }
            }

            internal void Request(int index, long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    var rs = requests;
                    BackpressureHelper.GetAndAddCap(ref rs[index], n);
                    if (Volatile.Read(ref subscriberCount) == rs.Length)
                    {
                        Drain();
                    }
                }
            }


            public void OnNext(T t)
            {
                if (_sourceMode == FusionMode.None)
                {
                    if (!_flow.Offer(t))
                    {
                        s.Cancel();
                        OnError(BackpressureHelper.MissingBackpressureException());
                        return;
                    }
                }
                Drain();
            }

            public void OnError(Exception e)
            {
                error = e;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnComplete()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                if (_sourceMode == FusionMode.Sync)
                {
                    DrainSync();
                }
                else
                {
                    DrainAsync();
                }
            }

            void DrainSync()
            {
                int missed = 1;
                var q = _flow;
                var a = subscribers;
                int n = a.Length;
                var r = requests;
                var e = emissions;
                int i = index;
                long u = id;

                for (;;)
                {

                    int notReady = 0;

                    for (;;)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            _flow.Clear();
                            return;
                        }

                        bool empty = q.IsEmpty();

                        if (empty)
                        {
                            foreach (var s in a)
                            {
                                s.OnComplete();
                            }
                            return;
                        }

                        long ei = e[i];
                        if (Volatile.Read(ref r[i]) != ei)
                        {
                            Option<T> elem;
                            try
                            {
                                elem = q.Poll();
                                empty = elem.IsNone;
                            }
                            catch (Exception ex)
                            {
                                ExceptionHelper.ThrowIfFatal(ex);
                                s.Cancel();
                                foreach (var s in a)
                                {
                                    s.OnError(ex);
                                }
                                return;
                            }

                            if (empty)
                            {
                                foreach (var s in a)
                                {
                                    s.OnComplete();
                                }
                                return;
                            }

                            a[i].OnNext(new OrderedItem<T>(u++, elem.GetValue()));

                            e[i] = ei + 1;

                            notReady = 0;
                        } else
                        {
                            notReady++;
                        }

                        if (++i == n)
                        {
                            i = 0;
                        }

                        if (notReady == n)
                        {
                            break;
                        }
                    }

                    id = u;
                    index = i;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void DrainAsync()
            {
                int missed = 1;
                var q = _flow;
                var a = subscribers;
                int n = a.Length;
                var r = requests;
                var e = emissions;
                int i = index;
                int c = produced;
                long u = id;

                for (;;)
                {
                    int notReady = 0;

                    for (;;)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            _flow.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);
                        if (d)
                        {
                            var ex = error;
                            if (ex != null)
                            {
                                q.Clear();
                                foreach (var s in a)
                                {
                                    s.OnError(ex);
                                }
                                return;
                            }
                        }

                        bool empty = q.IsEmpty();

                        if (d && empty)
                        {
                            foreach (var s in a)
                            {
                                s.OnComplete();
                            }
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        long ei = e[i];
                        if (Volatile.Read(ref r[i]) != ei)
                        {
                            Option<T> elem;
                            try
                            {
                                elem = q.Poll();
                                empty = elem.IsNone;
                            }
                            catch (Exception ex)
                            {
                                ExceptionHelper.ThrowIfFatal(ex);
                                s.Cancel();
                                foreach (var s in a)
                                {
                                    s.OnError(ex);
                                }
                                return;
                            }

                            if (empty)
                            {
                                break;
                            }

                            a[i].OnNext(new OrderedItem<T>(u++, elem.GetValue()));

                            e[i] = ei + 1;

                            int ci = ++c;
                            if (ci == limit)
                            {
                                c = 0;
                                s.Request(ci);
                            }

                            notReady = 0;
                        }
                        else
                        {
                            notReady++;
                        }

                        if (++i == n)
                        {
                            i = 0;
                        }

                        if (notReady == n)
                        {
                            break;
                        }
                    }

                    id = u;
                    index = i;
                    produced = c;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            sealed class RailSubscription : ISubscription
            {
                readonly OrderedDispatcher parent;

                readonly int index;

                internal RailSubscription(OrderedDispatcher parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void Request(long n)
                {
                    parent.Request(index, n);
                }

                public void Cancel()
                {
                    parent.Cancel();
                }
            }
        }
    }
}
