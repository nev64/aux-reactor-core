using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDematerialize<T> : IFlux<T>
    {
        readonly IPublisher<ISignal<T>> source;

        internal PublisherDematerialize(IPublisher<ISignal<T>> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new DematerializeSubscriber(s));
        }

        sealed class DematerializeSubscriber : BasicSubscriber<ISignal<T>, T>, ISubscription
        {
            ISignal<T> value;

            public DematerializeSubscriber(ISubscriber<T> actual) : base(actual)
            {
            }

            protected override void AfterSubscribe()
            {
                s.Request(1);
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(ISignal<T> t)
            {
                if (done)
                {
                    return;
                }

                var s = value;

                if (s != null)
                {
                    if (s.IsNext)
                    {
                        actual.OnNext(s.Next);
                    }
                    else
                    if (s.IsError)
                    {
                        value = null;
                        this.s.Cancel();
                        Error(s.Error);
                        return;
                    }
                    else
                    {
                        this.s.Cancel();
                        Complete();
                        return;
                    }
                }

                value = t;
            }
        }
    }
}
