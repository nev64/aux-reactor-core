using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDelaySubscription<T, U> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly IPublisher<U> other;

        internal PublisherDelaySubscription(IPublisher<T> source, IPublisher<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                other.Subscribe(new DelaySubscriptionConditionalSubscriber(s as IConditionalSubscriber<T>, source));
            }
            else
            {
                other.Subscribe(new DelaySubscriptionSubscriber(s, source));
            }
        }

        sealed class DelaySubscriptionSubscriber : ISubscriber<U>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly IPublisher<T> source;

            SubscriptionArbiterStruct arbiter;

            bool done;

            internal DelaySubscriptionSubscriber(ISubscriber<T> actual, IPublisher<T> source)
            {
                this.actual = actual;
                this.source = source;
            }

            public void OnSubscribe(ISubscription s)
            {
                arbiter.Set(s);

                actual.OnSubscribe(this);

                s.Request(long.MaxValue);
            }

            public void OnNext(U t)
            {
                OnComplete();
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                arbiter.Set(EmptySubscription<T>.Instance);
                Subscribe();
            }

            public void Request(long n)
            {
                arbiter.Request(n);
            }

            public void Cancel()
            {
                arbiter.Cancel();
            }

            void Subscribe()
            {
                source.Subscribe(new OtherSubscriber(this));
            }

            sealed class OtherSubscriber : ISubscriber<T>
            {
                readonly DelaySubscriptionSubscriber parent;

                readonly ISubscriber<T> actual;

                internal OtherSubscriber(DelaySubscriptionSubscriber parent)
                {
                    this.parent = parent;
                    this.actual = parent.actual;
                }

                public void OnSubscribe(ISubscription s)
                {
                    parent.arbiter.Set(s);
                }

                public void OnNext(T t)
                {
                    actual.OnNext(t);
                }

                public void OnError(Exception e)
                {
                    actual.OnError(e);
                }

                public void OnComplete()
                {
                    actual.OnComplete();
                }
            }
        }

        sealed class DelaySubscriptionConditionalSubscriber : IConditionalSubscriber<U>, ISubscription
        {
            readonly IConditionalSubscriber<T> actual;

            readonly IPublisher<T> source;

            SubscriptionArbiterStruct arbiter;

            bool done;

            internal DelaySubscriptionConditionalSubscriber(IConditionalSubscriber<T> actual, IPublisher<T> source)
            {
                this.actual = actual;
                this.source = source;
            }

            public void OnSubscribe(ISubscription s)
            {
                arbiter.Set(s);

                actual.OnSubscribe(this);

                s.Request(long.MaxValue);
            }

            public void OnNext(U t)
            {
                OnComplete();
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                arbiter.Set(EmptySubscription<T>.Instance);
                Subscribe();
            }

            public void Request(long n)
            {
                arbiter.Request(n);
            }

            public void Cancel()
            {
                arbiter.Cancel();
            }

            void Subscribe()
            {
                source.Subscribe(new OtherSubscriber(this));
            }

            public bool TryOnNext(U t)
            {
                OnComplete();
                return false;
            }

            sealed class OtherSubscriber : IConditionalSubscriber<T>
            {
                readonly DelaySubscriptionConditionalSubscriber parent;

                readonly IConditionalSubscriber<T> actual;

                internal OtherSubscriber(DelaySubscriptionConditionalSubscriber parent)
                {
                    this.parent = parent;
                    this.actual = parent.actual;
                }

                public void OnSubscribe(ISubscription s)
                {
                    parent.arbiter.Set(s);
                }

                public void OnNext(T t)
                {
                    actual.OnNext(t);
                }

                public void OnError(Exception e)
                {
                    actual.OnError(e);
                }

                public void OnComplete()
                {
                    actual.OnComplete();
                }

                public bool TryOnNext(T t)
                {
                    return actual.TryOnNext(t);
                }
            }
        }
    }
}
