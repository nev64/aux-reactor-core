using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSwitchIfEmpty<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly IPublisher<T> other;

        internal PublisherSwitchIfEmpty(IPublisher<T> source, IPublisher<T> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                var parent = new SwitchIfEmptyConditionalSubscriber((IConditionalSubscriber<T>)s, other);
                s.OnSubscribe(parent);
                source.Subscribe(parent);
            }
            else
            {
                var parent = new SwitchIfEmptySubscriber(s, other);
                s.OnSubscribe(parent);
                source.Subscribe(parent);
            }
        }

        sealed class SwitchIfEmptySubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly IPublisher<T> other;

            SubscriptionArbiterStruct arbiter;

            bool hasValue;

            internal SwitchIfEmptySubscriber(ISubscriber<T> actual, IPublisher<T> other)
            {
                this.actual = actual;
                this.other = other;
            }

            public void OnSubscribe(ISubscription s)
            {
                arbiter.Set(s);
            }

            public void OnNext(T t)
            {
                if (!hasValue)
                {
                    hasValue = true;
                }

                actual.OnNext(t);
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnComplete()
            {
                if (hasValue)
                {
                    actual.OnComplete();
                }
                else
                {
                    hasValue = true;
                    other.Subscribe(this);
                }
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    arbiter.Request(n);
                }
            }

            public void Cancel()
            {
                arbiter.Cancel();
            }
        }

        sealed class SwitchIfEmptyConditionalSubscriber : IConditionalSubscriber<T>, ISubscription
        {
            readonly IConditionalSubscriber<T> actual;

            readonly IPublisher<T> other;

            SubscriptionArbiterStruct arbiter;

            bool hasValue;

            internal SwitchIfEmptyConditionalSubscriber(IConditionalSubscriber<T> actual, IPublisher<T> other)
            {
                this.actual = actual;
                this.other = other;
            }

            public void OnSubscribe(ISubscription s)
            {
                arbiter.Set(s);
            }

            public void OnNext(T t)
            {
                if (!hasValue)
                {
                    hasValue = true;
                }

                actual.OnNext(t);
            }

            public bool TryOnNext(T t)
            {
                if (!hasValue)
                {
                    hasValue = true;
                }

                return actual.TryOnNext(t);
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnComplete()
            {
                if (hasValue)
                {
                    actual.OnComplete();
                }
                else
                {
                    hasValue = true;
                    other.Subscribe(this);
                }
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    arbiter.Request(n);
                }
            }

            public void Cancel()
            {
                arbiter.Cancel();
            }
        }
    }
}
