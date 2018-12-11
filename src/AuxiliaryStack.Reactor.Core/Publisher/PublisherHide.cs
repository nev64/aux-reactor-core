using System;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherHide<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        internal PublisherHide(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new HideSubscriber(s));
        }

        sealed class HideSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            ISubscription s;

            internal HideSubscriber(ISubscriber<T> actual)
            {
                this.actual = actual;
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
                actual.OnNext(t);
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
        }
    }
}
