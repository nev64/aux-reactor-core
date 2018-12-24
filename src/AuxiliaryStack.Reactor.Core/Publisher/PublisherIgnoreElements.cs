using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherIgnoreElements<T, R> : IFlux<R>, IMono<R>
    {
        readonly IPublisher<T> source;

        internal PublisherIgnoreElements(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<R> s)
        {
            source.Subscribe(new IgnoreElementsSubscriber(s));
        }

        sealed class IgnoreElementsSubscriber : ISubscriber<T>, IFlowSubscription<R>
        {
            readonly ISubscriber<R> actual;

            ISubscription s;

            public IgnoreElementsSubscriber(ISubscriber<R> actual)
            {
                this.actual = actual;
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void Clear()
            {
                // always empty
            }

            public bool IsEmpty()
            {
                return true;
            }

            public bool Offer(R value)
            {
                return FuseableHelper.DontCallOffer();
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
                // ignored
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    s.Request(long.MaxValue);
                }
            }

            public Option<R> Poll()
            {
                return None<R>();
            }

            public void Request(long n)
            {
                // ignored, always empty
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                return mode & FusionMode.Async;
            }
        }
    }
}
