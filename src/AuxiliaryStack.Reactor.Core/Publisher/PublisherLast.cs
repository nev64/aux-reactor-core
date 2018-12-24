using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherLast<T> : IMono<T>
    {
        readonly IPublisher<T> source;
    
        internal PublisherLast(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new LastSubscriber(s));
        }

        sealed class LastSubscriber : DeferredScalarSubscriber<T, T>
        {

            bool hasValue;

            public LastSubscriber(ISubscriber<T> actual) : base(actual)
            {
            }

            protected override void OnStart()
            {
                _subscription.Request(long.MaxValue);
            }

            public override void OnComplete()
            {
                if (hasValue)
                {
                    Complete(_value);
                }
                else
                {
                    Error(new IndexOutOfRangeException("The source sequence is empty."));
                }
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                hasValue = true;
                _value = t;
            }
        }
    }

}
