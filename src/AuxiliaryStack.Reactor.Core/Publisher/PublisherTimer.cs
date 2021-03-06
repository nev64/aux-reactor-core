﻿using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherTimer : IFlux<long>, IMono<long>
    {
        readonly TimeSpan delay;

        readonly ITimedScheduler scheduler;

        internal PublisherTimer(TimeSpan delay, ITimedScheduler scheduler)
        {
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<long> s)
        {
            TimerSubscription parent = new TimerSubscription(s);
            s.OnSubscribe(parent);

            parent.SetFuture(scheduler.Schedule(parent.Run, delay));
        }

        sealed class TimerSubscription : IFlowSubscription<long>
        {
            readonly ISubscriber<long> actual;

            IDisposable d;

            bool requested;

            bool available;

            internal TimerSubscription(ISubscriber<long> actual)
            {
                this.actual = actual;
            }

            internal void SetFuture(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }

            internal void Run()
            {
                if (Volatile.Read(ref requested))
                {
                    available = true;
                    actual.OnNext(0);
                    if (!DisposableHelper.IsDisposed(ref d))
                    {
                        actual.OnComplete();
                    }
                }
                else
                {
                    actual.OnError(BackpressureHelper.MissingBackpressureException());
                }
            }

            public void Cancel()
            {
                DisposableHelper.Dispose(ref d);
            }

            public void Clear()
            {
                // nothing to do
            }

            public bool IsEmpty()
            {
                return !available;
            }

            public bool Offer(long value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<long> Poll()
            {
                if (available)
                {
                    available = false;
                    return Just(0L);
                }
                
                return None<long>();
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    Volatile.Write(ref requested, true);
                }
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                return mode & FusionMode.Async;
            }
        }
    }
}
