using System;
using AuxiliaryStack.Reactor.Core.Flow;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class MonoPublishOn<T> : IMono<T>
    {
        readonly IMono<T> source;

        readonly IScheduler scheduler;

        internal MonoPublishOn(IMono<T> source, IScheduler scheduler)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new PublishOnSubscriber(s, scheduler));
        }

        sealed class PublishOnSubscriber : ISubscriber<T>, IQueueSubscription<T>
        {
            readonly ISubscriber<T> actual;

            readonly IScheduler scheduler;

            ISubscription s;

            bool hasValue;
            bool valueTaken;

            T value;

            internal PublishOnSubscriber(ISubscriber<T> actual, IScheduler scheduler)
            {
                this.actual = actual;
                this.scheduler = scheduler;
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                if (!hasValue)
                {
                    scheduler.Schedule(() => actual.OnComplete());
                }
            }

            public void OnError(Exception e)
            {
                scheduler.Schedule(() => actual.OnError(e));
            }

            public void OnNext(T t)
            {
                hasValue = true;
                value = t;
                scheduler.Schedule(() =>
                {
                    actual.OnNext(value);
                    actual.OnComplete();
                });
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(this);
            }

            public void Request(long n)
            {
                s.Request(n);
            }

            public int RequestFusion(int mode)
            {
                return mode & FuseableHelper.ASYNC;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public bool Poll(out T value)
            {
                if (!valueTaken)
                {
                    valueTaken = true;
                    value = this.value;
                    return true;
                }
                value = default(T);
                return false;
            }

            public bool IsEmpty()
            {
                return !hasValue || valueTaken;
            }

            public void Clear()
            {
                hasValue = false;
                value = default(T);
            }
        }
    }
}
