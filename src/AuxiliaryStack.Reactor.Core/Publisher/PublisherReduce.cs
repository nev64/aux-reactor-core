using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherReduce<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, T, T> reducer;

        internal PublisherReduce(IPublisher<T> source, Func<T, T, T> reducer)
        {
            this.source = source;
            this.reducer = reducer;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new ReduceSubscriber(s, reducer));
        }

        sealed class ReduceSubscriber : DeferredScalarSubscriber<T, T>
        {
            readonly Func<T, T, T> reducer;

            bool hasValue;

            public ReduceSubscriber(ISubscriber<T> actual, Func<T, T, T> reducer) : base(actual)
            {
                this.reducer = reducer;
            }

            protected override void OnStart()
            {
                s.Request(long.MaxValue);
            }

            public override void OnComplete()
            {
                if (hasValue)
                {
                    Complete(value);
                }
                else
                {
                    actual.OnComplete();
                }
            }

            public override void OnError(Exception e)
            {
                value = default(T);
                actual.OnError(e);
            }

            public override void OnNext(T t)
            {
                if (!hasValue)
                {
                    value = t;
                    hasValue = true;
                }
                else
                {
                    try
                    {
                        value = reducer(value, t);
                    }
                    catch (Exception ex)
                    {
                        Fail(ex);
                    }
                }
            }
        }
    }
}
