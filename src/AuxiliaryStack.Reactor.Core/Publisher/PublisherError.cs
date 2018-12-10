using System;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherError<T> : IFlux<T>, IMono<T>
    {
        readonly Exception error;

        readonly bool whenRequested;

        internal PublisherError(Exception error, bool whenRequested)
        {
            this.error = error;
            this.whenRequested = whenRequested;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (!whenRequested)
            {
                EmptySubscription<T>.Error(s, error);
            }
            else
            {
                s.OnSubscribe(new ErrorSubscription(s, error));
            }
        }

        sealed class ErrorSubscription : IQueueSubscription<T>
        {
            readonly ISubscriber<T> actual;

            readonly Exception error;

            int once;

            int fusionMode;

            public ErrorSubscription(ISubscriber<T> actual, Exception error)
            {
                this.actual = actual;
                this.error = error;
            }

            public void Cancel()
            {
                Volatile.Write(ref once, 1);
            }

            public void Clear()
            {
                // ignored
            }

            public bool IsEmpty()
            {
                throw error;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public bool Poll(out T value)
            {
                throw error;
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        if (fusionMode == FuseableHelper.ASYNC)
                        {
                            actual.OnNext(default(T));
                        }
                        else
                        {
                            actual.OnError(error);
                        }
                    }
                }
            }

            public int RequestFusion(int mode)
            {
                fusionMode = mode;
                return mode;
            }
        }
    }
}
