using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherIgnoreBoth<T, U, R> : IFlux<R>, IMono<R>
    {
        readonly IPublisher<T> first;

        readonly IPublisher<U> second;

        internal PublisherIgnoreBoth(IPublisher<T> first, IPublisher<U> second)
        {
            this.first = first;
            this.second = second;
        }

        public void Subscribe(ISubscriber<R> s)
        {
            first.Subscribe(new IgnoreBothFirstSubscriber(s, second));
        }

        sealed class IgnoreBothFirstSubscriber : ISubscriber<T>, IFlowSubscription<R>
        {
            readonly ISubscriber<R> actual;

            readonly IPublisher<U> other;

            ISubscription s;

            ISubscription z;

            internal IgnoreBothFirstSubscriber(ISubscriber<R> actual, IPublisher<U> other)
            {
                this.actual = actual;
                this.other = other;
            }

            public void Cancel()
            {
                s.Cancel();
                SubscriptionHelper.Cancel(ref z);
            }

            public void OnComplete()
            {
                other.Subscribe(new SecondSubscriber(this));
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
                    actual.OnSubscribe(this);

                    s.Request(long.MaxValue);
                }
            }

            public void Request(long n)
            {
                // ignored
            }

            internal void OtherSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.SetOnce(ref z, s))
                {
                    s.Request(long.MaxValue);
                }
            }

            internal void OtherComplete()
            {
                actual.OnComplete();
            }

            internal void OtherError(Exception ex)
            {
                actual.OnError(ex);
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                return mode & FusionMode.Async;
            }

            public bool Offer(R value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<R> Poll()
            {
                return None<R>();
            }

            public bool IsEmpty()
            {
                return true;
            }

            public void Clear()
            {
                // always empty
            }

            sealed class SecondSubscriber : ISubscriber<U>
            {
                readonly IgnoreBothFirstSubscriber parent;

                internal SecondSubscriber(IgnoreBothFirstSubscriber parent)
                {
                    this.parent = parent;
                }

                public void OnComplete()
                {
                    parent.OtherComplete();
                }

                public void OnError(Exception e)
                {
                    parent.OtherError(e);
                }

                public void OnNext(U t)
                {
                    // ignored
                }

                public void OnSubscribe(ISubscription s)
                {
                    parent.OtherSubscribe(s);
                }
            }
        }
    }
}
