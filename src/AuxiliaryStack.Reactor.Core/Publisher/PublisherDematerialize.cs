using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDematerialize<T> : IFlux<T>
    {
        private readonly IPublisher<Signal<T>> _source;

        internal PublisherDematerialize(IPublisher<Signal<T>> source) => _source = source;

        public void Subscribe(ISubscriber<T> subscriber) =>
            _source.Subscribe(new DematerializeSubscriber(subscriber));

        sealed class DematerializeSubscriber : BasicSubscriber<Signal<T>, T>
        {
            private Option<Signal<T>> _latestSignal;

            public DematerializeSubscriber(ISubscriber<T> actual) : base(actual) => _latestSignal = None<Signal<T>>();

            protected override void AfterSubscribe() => _subscription.Request(1);

            public override void OnComplete() => Complete();

            public override void OnError(Exception e) => Error(e);

            public override void OnNext(Signal<T> signal)
            {
                if (_isCompleted)
                {
                    return;
                }

                var latestSignal = _latestSignal;

                if (latestSignal.IsJust)
                {
                    var value = latestSignal.GetValue();
                    if (value.IsNext)
                    {
                        _actual.OnNext(value.Next.GetValue());
                    }
                    else if (value.IsError)
                    {
                        _latestSignal = None<Signal<T>>();
                        _subscription.Cancel();
                        Error(value.Error.GetValue());
                        return;
                    }
                    else
                    {
                        _subscription.Cancel();
                        Complete();
                        return;
                    }
                }

                _latestSignal = Just(signal);
            }
        }
    }
}