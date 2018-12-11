using System;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    /// <summary>
    /// Wrapper that makes sure the TCK's misbehaviors are handled according to the
    /// TCK's expectations.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    sealed class PublisherTck<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        internal PublisherTck(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber), "§1.9 violated: Subscribe(null) not allowed");
            }
            source.Subscribe(new TckSubscriber(subscriber));
        }

        sealed class TckSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            HalfSerializerStruct serializer;

            ISubscription s;

            internal TckSubscriber(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public void OnSubscribe(ISubscription subscription)
            {
                if (SubscriptionHelper.Validate(ref this.s, subscription))
                {
                    this.s = subscription;

                    actual.OnSubscribe(this);
                }
            }

            public void OnNext(T element)
            {
                serializer.OnNext(actual, element);
            }

            public void OnError(Exception cause)
            {
                serializer.OnError(actual, cause);
            }

            public void OnComplete()
            {
                serializer.OnComplete(actual);
            }

            public void Request(long n)
            {
                if (n <= 0)
                {
                    OnError(new ArgumentException("§3.9 violated: non-positive request amount"));
                } else
                {
                    s.Request(n);
                }
            }

            public void Cancel()
            {
                s.Cancel();
            }
        }
    }
}
