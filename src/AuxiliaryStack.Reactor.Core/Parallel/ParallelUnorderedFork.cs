using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Parallel
{
    internal sealed class ParallelUnorderedFork<T> : ParallelUnorderedFlux<T>
    {
        private readonly IPublisher<T> _source;
        private readonly int _parallelism;
        private readonly int _prefetch;

        internal ParallelUnorderedFork(IPublisher<T> source, int parallelism, int prefetch)
        {
            _source = source;
            _parallelism = parallelism;
            _prefetch = prefetch;
        }

        public override int Parallelism => _parallelism;

        public override void Subscribe(ISubscriber<T>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }

            _source.Subscribe(new UnorderedDispatcher(subscribers, _prefetch));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class UnorderedDispatcher : ISubscriber<T>
        {
            private readonly ISubscriber<T>[] _subscribers;
            private readonly long[] _requests;
            private readonly long[] _emissions;
            private readonly int _prefetch;
            private readonly int _limit;
            private ISubscription _subscription;
            private IFlow<T> _flow;
            private bool _isDone;
            private Exception _error;
            private bool _isCancelled;
            private int _produced;
            private FusionMode _sourceMode;
            private int _index;
            private int _subscriberCount;
            private Pad128 _p0;
            private int _wip;
            private Pad120 _p1;

            internal UnorderedDispatcher(ISubscriber<T>[] subscribers, int prefetch)
            {
                _subscribers = subscribers;
                _requests = new long[subscribers.Length];
                _emissions = new long[subscribers.Length];
                _prefetch = prefetch;
                _limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription subscription)
            {
                if (SubscriptionHelper.Validate(ref _subscription, subscription))
                {
                    if (subscription is IFlowSubscription<T> flow)
                    {
                        var m = flow.RequestFusion(FusionMode.Any);

                        if (m == FusionMode.Sync)
                        {
                            _sourceMode = m;
                            _flow = flow;
                            Volatile.Write(ref _isDone, true);
                            SetupSubscribers();
                            Drain();
                            return;
                        }
                        if (m == FusionMode.Async)
                        {
                            _sourceMode = m;
                            _flow = flow;
                            SetupSubscribers();
                            subscription.Request(_prefetch < 0 ? long.MaxValue : _prefetch);
                            return;
                        }
                    }

                    _flow = QueueDrainHelper.CreateQueue<T>(_prefetch);

                    SetupSubscribers();

                    subscription.Request(_prefetch < 0 ? long.MaxValue : _prefetch);
                }
            }

            void SetupSubscribers()
            {
                var array = _subscribers;

                for (var i = 0; i < array.Length; i++)
                {
                    if (Volatile.Read(ref _isCancelled))
                    {
                        return;
                    }
                    Volatile.Write(ref _subscriberCount, i + 1);

                    array[i].OnSubscribe(new RailSubscription(this, i));
                }
            }

            internal void Cancel()
            {
                Volatile.Write(ref _isCancelled, true);
                _subscription.Cancel();
                if (QueueDrainHelper.Enter(ref _wip))
                {
                    _flow.Clear();
                }
            }

            internal void Request(int index, long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    var rs = _requests;
                    BackpressureHelper.GetAndAddCap(ref rs[index], n);
                    if (Volatile.Read(ref _subscriberCount) == rs.Length)
                    {
                        Drain();
                    }
                }
            }

            public void OnNext(T value)
            {
                if (_sourceMode == FusionMode.None)
                {
                    if (!_flow.Offer(value))
                    {
                        _subscription.Cancel();
                        OnError(BackpressureHelper.MissingBackpressureException());
                        return;
                    }
                }
                Drain();
            }

            public void OnError(Exception e)
            {
                _error = e;
                Volatile.Write(ref _isDone, true);
                Drain();
            }

            public void OnComplete()
            {
                Volatile.Write(ref _isDone, true);
                Drain();
            }

            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref _wip))
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
                var a = _subscribers;
                int n = a.Length;
                var r = _requests;
                var e = _emissions;
                int i = _index;

                for (;;)
                {

                    int notReady = 0;

                    for (;;)
                    {
                        if (Volatile.Read(ref _isCancelled))
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
                                _subscription.Cancel();
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

                            a[i].OnNext(elem.GetValue());

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

                    _index = i;
                    missed = QueueDrainHelper.Leave(ref _wip, missed);
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
                var a = _subscribers;
                int n = a.Length;
                var r = _requests;
                var e = _emissions;
                int i = _index;
                int c = _produced;

                for (;;)
                {
                    int notReady = 0;

                    for (;;)
                    {
                        if (Volatile.Read(ref _isCancelled))
                        {
                            _flow.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref _isDone);
                        if (d)
                        {
                            var ex = _error;
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
                                _subscription.Cancel();
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

                            a[i].OnNext(elem.GetValue());

                            e[i] = ei + 1;

                            int ci = ++c;
                            if (ci == _limit)
                            {
                                c = 0;
                                _subscription.Request(ci);
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

                    _index = i;
                    _produced = c;
                    missed = QueueDrainHelper.Leave(ref _wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            sealed class RailSubscription : ISubscription
            {
                readonly UnorderedDispatcher parent;

                readonly int index;

                internal RailSubscription(UnorderedDispatcher parent, int index)
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
