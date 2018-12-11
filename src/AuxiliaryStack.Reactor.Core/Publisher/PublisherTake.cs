using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherTake<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly long n;

        public PublisherTake(IPublisher<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new TakeConditionalSubscriber((IConditionalSubscriber<T>)s, n));
            }
            else
            {
                source.Subscribe(new TakeSubscriber(s, n));
            }
        }

        sealed class TakeSubscriber : BasicFuseableSubscriber<T, T>
        {
            readonly long n;

            long remaining;

            int once;

            public TakeSubscriber(ISubscriber<T> actual, long n) : base(actual)
            {
                this.n = n;
                this.remaining = n;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                if (fusionMode == FuseableHelper.ASYNC)
                {
                    actual.OnNext(t);
                    return;
                }
                long r = remaining;
                if (r == 0L)
                {
                    s.Cancel();
                    Complete();
                    return;
                }
                actual.OnNext(t);
                if (--r == 0)
                {
                    s.Cancel();
                    Complete();
                    return;
                }
                remaining = r;
            }

            public override int RequestFusion(int mode)
            {
                return TransitiveAnyFusion(mode);
            }

            public override void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (Volatile.Read(ref once) == 0 && Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        if (n >= this.n)
                        {
                            s.Request(long.MaxValue);
                            return;
                        }
                    }
                    s.Request(n);
                }
            }

            public override Option<T> Poll()
            {
                long r = remaining;
                if (r == 0)
                {
                    if (fusionMode == FuseableHelper.ASYNC && !done)
                    {
                        done = true;
                        qs.Cancel();
                        actual.OnComplete();
                    }
                    return None<T>();
                }

                var result = qs.Poll();
                if (result.IsJust)
                {
                    remaining = --r;
                    if (r == 0)
                    {
                        if (fusionMode == FuseableHelper.ASYNC && !done)
                        {
                            done = true;
                            qs.Cancel();
                            actual.OnComplete();
                        }
                    }
                }
                return result;
            }
        }

        sealed class TakeConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly long n;

            long remaining;

            int once;

            public TakeConditionalSubscriber(IConditionalSubscriber<T> actual, long n) : base(actual)
            {
                this.n = n;
                this.remaining = n;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                if (fusionMode == FuseableHelper.ASYNC)
                {
                    actual.OnNext(t);
                    return;
                }
                long r = remaining;
                if (r == 0L)
                {
                    s.Cancel();
                    Complete();
                    return;
                }
                actual.OnNext(t);
                if (--r == 0)
                {
                    s.Cancel();
                    Complete();
                    return;
                }
                remaining = r;
            }

            public override bool TryOnNext(T t)
            {
                if (done)
                {
                    return true;
                }
                if (fusionMode == FuseableHelper.ASYNC)
                {
                    return actual.TryOnNext(t);
                }
                long r = remaining;
                if (r == 0L)
                {
                    s.Cancel();
                    Complete();
                    return true;
                }
                bool b = actual.TryOnNext(t);
                if (--r == 0)
                {
                    s.Cancel();
                    Complete();
                    return true;
                }
                return b;
            }

            public override int RequestFusion(int mode)
            {
                return TransitiveAnyFusion(mode);
            }

            public override void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (Volatile.Read(ref once) == 0 && Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        if (n >= this.n)
                        {
                            s.Request(long.MaxValue);
                            return;
                        }
                    }
                    s.Request(n);
                }
            }

            public override Option<T> Poll()
            {
                long r = remaining;
                if (r == 0)
                {
                    if (fusionMode == FuseableHelper.ASYNC && !done)
                    {
                        done = true;
                        qs.Cancel();
                        actual.OnComplete();
                    }

                    return None<T>();
                }

                var result = qs.Poll();
                if (result.IsJust) 
                {
                    remaining = --r;
                    if (r == 0)
                    {
                        if (fusionMode == FuseableHelper.ASYNC && !done)
                        {
                            done = true;
                            qs.Cancel();
                            actual.OnComplete();
                        }
                    }
                }
                return result;
            }

            public override bool IsEmpty()
            {
                return remaining == 0 || qs.IsEmpty();
            }
        }
    }
}
