﻿using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;


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

        sealed class ErrorSubscription : IFlowSubscription<T>
        {
            readonly ISubscriber<T> actual;

            readonly Exception error;

            int once;

            FusionMode fusionMode;

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

            public Option<T> Poll()
            {
                throw error;
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        if (fusionMode == FusionMode.Async)
                        {
                            actual.OnNext(default);
                        }
                        else
                        {
                            actual.OnError(error);
                        }
                    }
                }
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                fusionMode = mode;
                return mode;
            }
        }
    }
}
