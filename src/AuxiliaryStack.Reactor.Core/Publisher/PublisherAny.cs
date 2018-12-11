using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherAny<T> : IMono<bool>
    {
        readonly IPublisher<T> source;

        readonly Func<T, bool> predicate;

        public PublisherAny(IPublisher<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISubscriber<bool> s)
        {
            source.Subscribe(new AnySubscriber(s, predicate));
        }

        sealed class AnySubscriber : DeferredScalarSubscriber<T, bool>
        {
            readonly Func<T, bool> predicate;

            bool done;

            public AnySubscriber(ISubscriber<bool> actual, Func<T, bool> predicate) : base(actual)
            {
                this.predicate = predicate;
            }

            protected override void OnStart()
            {
                s.Request(long.MaxValue);
            }


            public override void OnComplete()
            {
                if (done)
                {
                    return;
                }
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

                bool b;

                try
                {
                    b = predicate(t);
                }
                catch (Exception ex)
                {
                    done = true;
                    Fail(ex);
                    return;
                }
                if (b)
                {
                    s.Cancel();
                    done = true;
                    Complete(true);
                }
            }
        }
    }
}
