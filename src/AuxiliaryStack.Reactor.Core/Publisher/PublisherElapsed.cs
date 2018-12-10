using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherElapsed<T> : IFlux<Timed<T>>, IMono<Timed<T>>
    {
        readonly IPublisher<T> source;

        readonly ITimedScheduler scheduler;

        internal PublisherElapsed(IPublisher<T> source, ITimedScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<Timed<T>> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new ElapsedConditionalSubscriber(s as IConditionalSubscriber<Timed<T>>, scheduler));
            }
            else
            {
                source.Subscribe(new ElapsedSubscriber(s, scheduler));
            }
        }

        sealed class ElapsedSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<Timed<T>> actual;

            readonly ITimedScheduler scheduler;

            ISubscription s;

            long last;

            internal ElapsedSubscriber(ISubscriber<Timed<T>> actual, ITimedScheduler scheduler)
            {
                this.actual = actual;
                this.scheduler = scheduler;
                this.last = scheduler.NowUtc;
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                actual.OnComplete();
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                long prev = last;
                long curr = scheduler.NowUtc;
                last = curr;

                actual.OnNext(new Timed<T>(t, curr - prev));
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

        sealed class ElapsedConditionalSubscriber : IConditionalSubscriber<T>, ISubscription
        {
            readonly IConditionalSubscriber<Timed<T>> actual;

            readonly ITimedScheduler scheduler;

            ISubscription s;

            long last;

            internal ElapsedConditionalSubscriber(IConditionalSubscriber<Timed<T>> actual, ITimedScheduler scheduler)
            {
                this.actual = actual;
                this.scheduler = scheduler;
                this.last = scheduler.NowUtc;
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                actual.OnComplete();
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                long prev = last;
                long curr = scheduler.NowUtc;
                last = curr;

                actual.OnNext(new Timed<T>(t, curr - prev));
            }

            public bool TryOnNext(T t)
            {
                long prev = last;
                long curr = scheduler.NowUtc;
                last = curr;

                return actual.TryOnNext(new Timed<T>(t, curr - prev));
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
