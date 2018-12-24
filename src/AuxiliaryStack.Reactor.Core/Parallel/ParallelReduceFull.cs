using System;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelReduceFull<T> : IFlux<T>, IMono<T>
    {
        private readonly IParallelFlux<T> _source;
        private readonly Func<T, T, T> _reducer;

        internal ParallelReduceFull(IParallelFlux<T> source, Func<T, T, T> reducer)
        {
            _source = source;
            _reducer = reducer;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            var parent = new ReduceFullCoordinator(subscriber, _source.Parallelism, _reducer);
            subscriber.OnSubscribe(parent);

            _source.Subscribe(parent._subscribers);
        }

        sealed class ReduceFullCoordinator : DeferredScalarSubscription<T>
        {
            internal readonly InnerSubscriber[] _subscribers;
            private readonly Func<T, T, T> _reducer;
            private Exception _error;
            private int _remaining;
            private SlotPair _current;

            public ReduceFullCoordinator(ISubscriber<T> actual, int n, Func<T, T, T> reducer) 
                : base(actual)
            {
                var a = new InnerSubscriber[n];
                for (var i = 0; i < n; i++)
                {
                    a[i] = new InnerSubscriber(this, reducer);
                }
                _subscribers = a;
                _reducer = reducer;
                Volatile.Write(ref _remaining, n);
            }

            SlotPair AddValue(T value)
            {
                for (;;)
                {
                    var c = Volatile.Read(ref _current);

                    if (c == null)
                    {
                        c = new SlotPair();
                        if (Interlocked.CompareExchange(ref _current, c, null) != null)
                        {
                            continue;
                        }
                    }

                    int slot = c.TryAcquireSlot();
                    if (slot < 0)
                    {
                        Interlocked.CompareExchange(ref _current, null, c);
                        continue;
                    }
                    if (slot == 0)
                    {
                        c.first = value;
                    }
                    else
                    {
                        c.second = value;
                    }

                    if (c.ReleaseSlot())
                    {
                        Interlocked.CompareExchange(ref _current, null, c);
                        return c;
                    }
                    return null;
                }
            }

            public override void Cancel()
            {
                base.Cancel();
                foreach (var inner in _subscribers)
                {
                    inner.Cancel();
                }
            }

            internal void InnerComplete(T value, bool hasValue)
            {
                if (hasValue)
                {
                    for (;;)
                    {
                        var sp = AddValue(value);

                        if (sp == null)
                        {
                            break;
                        }

                        try
                        {
                            value = _reducer(sp.first, sp.second);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);
                            InnerError(ex);
                            return;
                        }
                    }
                }

                if (Interlocked.Decrement(ref _remaining) == 0)
                {
                    var sp = Volatile.Read(ref _current);
                    _current = null;
                    if (sp != null)
                    {
                        Complete(sp.first);
                    }
                    else
                    {
                        Complete();
                    }
                }
            }

            internal void InnerError(Exception ex)
            {
                if (ExceptionHelper.AddError(ref _error, ex))
                {
                    Cancel();
                    ex = ExceptionHelper.Terminate(ref _error);
                    _actual.OnError(ex);
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(ex);
                }
            }
        }

        sealed class InnerSubscriber : ISubscriber<T>
        {
            readonly ReduceFullCoordinator parent;

            readonly Func<T, T, T> reducer;

            ISubscription s;

            T value;

            bool hasValue;

            bool done;

            internal InnerSubscriber(ReduceFullCoordinator parent, Func<T, T, T> reducer)
            {
                this.parent = parent;
                this.reducer = reducer;
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.SetOnce(ref this.s, s))
                {
                    s.Request(long.MaxValue);
                }
            }

            public void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                if (!hasValue)
                {
                    value = t;
                    hasValue = true;
                }
                else
                {
                    try
                    {
                        value = reducer(value, t);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        done = true;
                        parent.InnerError(ex);
                    }
                }
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                done = true;
                parent.InnerError(e);
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                parent.InnerComplete(value, hasValue);
            }

            internal void Cancel()
            {
                SubscriptionHelper.Cancel(ref s);
            }
        }

        sealed class SlotPair
        {
            internal T first;
            internal T second;

            int acquireIndex;

            int releaseIndex;

            internal int TryAcquireSlot()
            {
                int i = Volatile.Read(ref acquireIndex);
                for (;;)
                {
                    if (i >= 2)
                    {
                        return -1;
                    }
                    int j = Interlocked.CompareExchange(ref acquireIndex, i + 1, i);
                    if (j == i)
                    {
                        return i;
                    }
                    i = j;
                }
            }

            internal bool ReleaseSlot()
            {
                return Interlocked.Increment(ref releaseIndex) == 2;
            }
        }
    }
}
