﻿using System;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDelay<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly TimeSpan delay;

        readonly ITimedScheduler scheduler;

        internal PublisherDelay(IPublisher<T> source, TimeSpan delay, ITimedScheduler scheduler)
        {
            this.source = source;
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new DelaySubscriber(s, delay, scheduler.CreateTimedWorker()));
        }

        sealed class DelaySubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly TimeSpan delay;

            readonly ITimedWorker worker;

            ISubscription s;

            internal DelaySubscriber(ISubscriber<T> actual, TimeSpan delay, ITimedWorker worker)
            {
                this.actual = actual;
                this.delay = delay;
                this.worker = worker;
            }

            public void Cancel()
            {
                s.Cancel();
                worker.Dispose();
            }

            public void OnComplete()
            {
                worker.Schedule(() =>
                {
                    actual.OnComplete();

                    worker.Dispose();
                }, delay);
            }

            public void OnError(Exception e)
            {
                worker.Schedule(() => {
                    actual.OnError(e);

                    worker.Dispose();
                }, delay);
            }

            public void OnNext(T t)
            {
                worker.Schedule(() => actual.OnNext(t), delay);
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    actual.OnSubscribe(this);
                }
            }

            public void Request(long n)
            {
                s.Request(n);
            }
        }
    }
}
