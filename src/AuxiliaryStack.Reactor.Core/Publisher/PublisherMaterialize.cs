using System;
using AuxiliaryStack.Reactor.Core.Subscriber;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherMaterialize<T> : IFlux<Signal<T>>
    {
        private readonly IPublisher<T> _source;

        internal PublisherMaterialize(IPublisher<T> source) => 
            _source = source;

        public void Subscribe(ISubscriber<Signal<T>> subscriber)
        {
            _source.Subscribe(new MaterializeSubscriber(subscriber));
        }

        sealed class MaterializeSubscriber : BasicSinglePostCompleteSubscriber<T, Signal<T>>
        {
            public MaterializeSubscriber(ISubscriber<Signal<T>> actual) : base(actual)
            {
            }

            public override void OnComplete() => Complete(Signal<T>.OfComplete());

            public override void OnError(Exception e) => Complete(Signal<T>.OfError(e));

            public override void OnNext(T value)
            {
                _produced++;
                _actual.OnNext(Signal<T>.OfNext(value));
            }
        }
    }
}
