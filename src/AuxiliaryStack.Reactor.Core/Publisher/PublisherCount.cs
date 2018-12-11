using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherCount<T> : IFlux<long>, IMono<long>
    {
        readonly IPublisher<T> source;

        public PublisherCount(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<long> s)
        {
            source.Subscribe(new CountSubscriber(s));
        }

        sealed class CountSubscriber : DeferredScalarSubscriber<T, long>
        {
            public CountSubscriber(ISubscriber<long> actual) : base(actual)
            {
            }

            public override void OnComplete()
            {
                Complete(value);
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                value++;
            }
        }
    }
}
