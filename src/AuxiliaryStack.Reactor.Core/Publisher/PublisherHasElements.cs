using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherHasElements<T> : IMono<bool>
    {
        readonly IPublisher<T> source;

        internal PublisherHasElements(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<bool> s)
        {
            source.Subscribe(new HasElementsSubscriber(s));
        }

        sealed class HasElementsSubscriber : DeferredScalarSubscriber<T, bool>
        {
            bool done;

            public HasElementsSubscriber(ISubscriber<bool> actual) : base(actual)
            {

            }

            protected override void OnStart()
            {
                _subscription.Request(long.MaxValue);
            }

            public override void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                Complete(false);
            }

            public override void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                done = true;
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                done = true;
                _subscription.Cancel();
                Complete(true);
            }
        }
    }
}
