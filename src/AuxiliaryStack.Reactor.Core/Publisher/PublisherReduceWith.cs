using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherReduceWith<T, A> : IFlux<A>, IMono<A>
    {
        readonly IPublisher<T> source;

        readonly Func<A> initialSupplier;

        readonly Func<A, T, A> reducer;

        internal PublisherReduceWith(IPublisher<T> source, Func<A> initialSupplier, Func<A, T, A> reducer)
        {
            this.source = source;
            this.reducer = reducer;
            this.initialSupplier = initialSupplier;
        }

        public void Subscribe(ISubscriber<A> s)
        {
            A accumulator;

            try
            {
                accumulator = initialSupplier();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<A>.Error(s, ex);
                return;
            }

            source.Subscribe(new ReduceWithSubscriber(s, accumulator, reducer));
        }

        internal sealed class ReduceWithSubscriber : DeferredScalarSubscriber<T, A>
        {
            readonly Func<A, T, A> reducer;

            public ReduceWithSubscriber(ISubscriber<A> actual, A accumulator, Func<A, T, A> reducer) : base(actual)
            {
                this._value = accumulator;
                this.reducer = reducer;
            }

            protected override void OnStart()
            {
                _subscription.Request(long.MaxValue);
            }

            public override void OnComplete()
            {
                Complete(_value);
            }

            public override void OnError(Exception e)
            {
                _value = default(A);
                _actual.OnError(e);
            }

            public override void OnNext(T t)
            {
                try
                {
                    _value = reducer(_value, t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }
        }
    }
}
