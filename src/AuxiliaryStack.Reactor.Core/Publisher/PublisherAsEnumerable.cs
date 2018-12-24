using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherAsEnumerable<T> : IEnumerable<T>
    {
        readonly IPublisher<T> source;

        readonly int prefetch;

        public PublisherAsEnumerable(IPublisher<T> source, int prefetch)
        {
            this.source = source;
            this.prefetch = prefetch;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var a = new AsEnumerator(prefetch);

            source.Subscribe(a);

            return a;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var a = new AsEnumerator(prefetch);

            source.Subscribe(a);

            return a;
        }

        sealed class AsEnumerator : ISubscriber<T>, IEnumerator<T>, IEnumerator
        {
            readonly int prefetch;

            readonly int limit;

            ISubscription s;

            IFlow<T> _flow;

            FusionMode _sourceMode;

            Exception error;

            bool done;

            T current;

            int consumed;

            public T Current
            {
                get
                {
                    return current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return current;
                }
            }

            public AsEnumerator(int prefetch)
            {
                this.prefetch = prefetch;
                this.limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription subscription)
            {
                if (SubscriptionHelper.SetOnce(ref this.s, subscription))
                {
                    if (subscription is IFlowSubscription<T> flow)
                    {
                        var mode = flow.RequestFusion(FusionMode.Any | FusionMode.Boundary);

                        if (mode == FusionMode.Sync)
                        {
                            _sourceMode = mode;
                            _flow = flow;
                            Volatile.Write(ref done, true);
                            return;
                        }

                        if (mode == FusionMode.Async)
                        {
                            _sourceMode = mode;
                            _flow = flow;

                            subscription.Request(prefetch < 0 ? long.MaxValue : prefetch);

                            return;

                        }
                    }

                    _flow = QueueDrainHelper.CreateQueue<T>(prefetch);

                    subscription.Request(prefetch < 0 ? long.MaxValue : prefetch);
                }
            }

            public void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                if (_sourceMode != FusionMode.Async)
                {
                    if (!_flow.Offer(t))
                    {
                        s.Cancel();
                        OnError(BackpressureHelper.MissingBackpressureException());
                        return;
                    }
                }

                Signal();
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                error = e;
                Volatile.Write(ref done, true);
                Signal();
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                Volatile.Write(ref done, true);
                Signal();
            }

            void Signal()
            {
                Monitor.Enter(this);
                try
                {
                    Monitor.Pulse(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            public bool MoveNext()
            {
                for (;;)
                {
                    bool d = Volatile.Read(ref done);

                    var elem = _flow.Poll();
                    
                    bool empty = elem.IsNone;

                    if (d && empty)
                    {
                        Exception ex = error;
                        if (ex != null)
                        {
                            throw ex;
                        }
                        return false;
                    }

                    if (empty)
                    {
                        Monitor.Enter(this);
                        try
                        {
                            Monitor.Wait(this);
                        }
                        finally
                        {
                            Monitor.Exit(this);
                        }
                    }
                    else
                    {
                        current = elem.GetValue();
                        if (_sourceMode != FusionMode.Sync)
                        {
                            int c = consumed + 1;
                            if (c == limit)
                            {
                                consumed = 0;
                                s.Request(c);
                            }
                            else
                            {
                                consumed = c;
                            }
                        }
                        return true;
                    }
                }
            }

            public void Reset() => throw new InvalidOperationException("Reset is not supported");

            public void Dispose() => SubscriptionHelper.Cancel(ref s);
        }
    }
}
