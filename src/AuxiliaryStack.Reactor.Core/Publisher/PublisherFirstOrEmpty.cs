using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFirstOrEmpty<T> : IMono<T>
    {
        readonly IPublisher<T> source;

        internal PublisherFirstOrEmpty(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new FirstOrEmptySubscriber(s));
        }

        sealed class FirstOrEmptySubscriber : DeferredScalarSubscriber<T, T>
        {

            bool done;

            public FirstOrEmptySubscriber(ISubscriber<T> actual) : base(actual)
            {
            }

            public override void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                Complete();
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
                s.Cancel();
                Complete(t);
            }

            protected override void OnStart()
            {
                s.Request(long.MaxValue);
            }
        }
    }
}
