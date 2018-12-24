using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads.Extensions;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelOrderedJoin<T> : IFlux<T>
    {
        readonly ParallelOrderedFlux<T> source;

        readonly int prefetch;

        internal ParallelOrderedJoin(ParallelOrderedFlux<T> source, int prefetch)
        {
            this.source = source;
            this.prefetch = prefetch;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            var parent = new OrderedJoin(s, source.Parallelism, prefetch);
            s.OnSubscribe(parent);
            source.SubscribeMany(parent.subscribers);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class OrderedJoin : ISubscription
        {
            static readonly IOrderedItem<T> FINISHED = new OrderedItem<T>(long.MaxValue, default(T));

            readonly ISubscriber<T> actual;

            internal readonly InnerSubscriber[] subscribers;

            readonly IOrderedItem<T>[] peek;

            Exception error;

            long requested;

            bool cancelled;

            Pad128 p0;

            int wip;

            Pad120 p1;

            long emitted;

            Pad120 p2;

            internal OrderedJoin(ISubscriber<T> actual, int n, int prefetch)
            {
                this.actual = actual;
                var a = new InnerSubscriber[n];
                for (int i = 0; i < n; i++)
                {
                    a[i] = new InnerSubscriber(this, prefetch);
                }

                this.subscribers = a;
                this.peek = new IOrderedItem<T>[n];
            }

            public void Cancel()
            {
                Volatile.Write(ref cancelled, true);
                CancelAll();

                if (QueueDrainHelper.Enter(ref wip))
                {
                    ClearAll();
                }
            }

            void CancelAll()
            {
                foreach (var inner in subscribers)
                {
                    inner.Cancel();
                }
            }

            void ClearAll()
            {
                var a = subscribers;
                int n = a.Length;
                for (int i = 0; i < n; i++)
                {
                    peek[i] = null;
                    a[i].Clear();
                }
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Drain();
                }
            }

            internal void InnerError(Exception e)
            {
                if (ExceptionHelper.AddError(ref error, e))
                {
                    CancelAll();
                    Drain();
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(e);
                }
            }

            internal void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                var b = actual;
                var vs = peek;
                var a = subscribers;
                int n = a.Length;
                int missed = 1;

                long e = emitted;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);

                    for (;;)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            ClearAll();
                            return;
                        }

                        var ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            ex = ExceptionHelper.Terminate(ref error);
                            CancelAll();
                            ClearAll();

                            b.OnError(ex);
                            return;
                        }

                        bool fullRow = true;
                        int finished = 0;

                        IOrderedItem<T> min = null;
                        int minIndex = -1;

                        for (int i = 0; i < n; i++)
                        {
                            var inner = a[i];

                            bool d = Volatile.Read(ref inner._isDone);

                            var q = Volatile.Read(ref inner._flow);
                            if (q != null)
                            {
                                var vsi = vs[i].ToOption();
                                if (vsi.IsNone)
                                {
                                    bool hasValue;

                                    try
                                    {
                                        vsi = q.Poll();
                                        hasValue = vsi.IsJust;
                                    }
                                    catch (Exception exc)
                                    {
                                        ExceptionHelper.AddError(ref error, exc);
                                        ex = ExceptionHelper.Terminate(ref error);
                                        CancelAll();
                                        ClearAll();

                                        b.OnError(ex);
                                        return;
                                    }

                                    if (hasValue)
                                    {
                                        vs[i] = vsi.GetValue();
                                        if (min == null || min.CompareTo(vsi.GetValue()) > 0)
                                        {
                                            min = vsi.GetValue();
                                            minIndex = i;
                                        }
                                    }
                                    else
                                    {
                                        if (d)
                                        {
                                            vs[i] = FINISHED;
                                            finished++;
                                        }
                                        else
                                        {
                                            fullRow = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (vsi.GetValue() == FINISHED)
                                    {
                                        finished++;
                                    }
                                    else if (min == null || min.CompareTo(vsi.GetValue()) > 0)
                                    {
                                        min = vsi.GetValue();
                                        minIndex = i;
                                    }
                                }
                            }
                            else
                            {
                                fullRow = false;
                            }
                        }

                        if (finished == n)
                        {
                            b.OnComplete();
                            return;
                        }

                        if (!fullRow || e == r || min == null)
                        {
                            break;
                        }

                        vs[minIndex] = null;

                        b.OnNext(min.Value);

                        e++;
                        a[minIndex].RequestOne();
                    }


                    emitted = e;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }

        sealed class InnerSubscriber : ISubscriber<IOrderedItem<T>>
        {
            private readonly OrderedJoin _parent;
            private readonly int _prefetch;
            private readonly int _limit;
            private ISubscription _subscription;
            private FusionMode _fusionMode;
            private int _produced;

            internal IFlow<IOrderedItem<T>> _flow;
            internal bool _isDone;

            internal InnerSubscriber(OrderedJoin parent, int prefetch)
            {
                _parent = parent;
                _prefetch = prefetch;
                _limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription subscription)
            {
                if (SubscriptionHelper.SetOnce(ref _subscription, subscription))
                {
                    if (subscription is IFlowSubscription<IOrderedItem<T>> flow)
                    {
                        var mode = flow.RequestFusion(FusionMode.Any);
                        if (mode == FusionMode.Sync)
                        {
                            _fusionMode = mode;
                            Volatile.Write(ref _flow, flow);
                            Volatile.Write(ref _isDone, true);

                            _parent.Drain();
                            return;
                        }

                        if (mode == FusionMode.Async)
                        {
                            _fusionMode = mode;
                            Volatile.Write(ref _flow, flow);
                            subscription.Request(_prefetch < 0 ? long.MaxValue : _prefetch);
                            return;
                        }
                    }

                    Volatile.Write(ref _flow, QueueDrainHelper.CreateQueue<IOrderedItem<T>>(_prefetch));

                    subscription.Request(_prefetch < 0 ? long.MaxValue : _prefetch);
                }
            }

            public void OnNext(IOrderedItem<T> t)
            {
                if (_fusionMode == FusionMode.None)
                {
                    if (!_flow.Offer(t))
                    {
                        OnError(BackpressureHelper.MissingBackpressureException("Queue full?!"));
                        return;
                    }
                }

                _parent.Drain();
            }

            public void OnError(Exception e)
            {
                _parent.InnerError(e);
            }

            public void OnComplete()
            {
                Volatile.Write(ref _isDone, true);
                _parent.Drain();
            }

            internal void Cancel()
            {
                SubscriptionHelper.Cancel(ref _subscription);
            }

            internal void Clear() => _flow?.Clear();

            internal void RequestOne()
            {
                if (_fusionMode != FusionMode.Sync)
                {
                    var produced = _produced + 1;
                    if (produced == _limit)
                    {
                        _produced = 0;
                        _subscription.Request(produced);
                    }
                    else
                    {
                        _produced = produced;
                    }
                }
            }
        }
    }
}