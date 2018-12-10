using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherMaterialize<T> : IFlux<ISignal<T>>
    {
        readonly IPublisher<T> source;

        internal PublisherMaterialize(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<ISignal<T>> s)
        {
            source.Subscribe(new MaterializeSubscriber(s));
        }

        sealed class MaterializeSubscriber : BasicSinglePostCompleteSubscriber<T, ISignal<T>>
        {
            public MaterializeSubscriber(ISubscriber<ISignal<T>> actual) : base(actual)
            {
            }

            public override void OnComplete()
            {
                Complete(SignalHelper.Complete<T>());
            }

            public override void OnError(Exception e)
            {
                Complete(SignalHelper.Error<T>(e));
            }

            public override void OnNext(T t)
            {
                produced++;
                actual.OnNext(SignalHelper.Next(t));
            }
        }
    }
}
