using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherCollect<T, C> : IMono<C>, IFlux<C>
    {
        readonly IPublisher<T> source;

        readonly Func<C> collectionSupplier;

        readonly Action<C, T> collector;

        internal PublisherCollect(IPublisher<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            this.source = source;
            this.collectionSupplier = collectionSupplier;
            this.collector = collector;
        }

        public void Subscribe(ISubscriber<C> s)
        {
            C c;

            try
            {
                c = collectionSupplier();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<C>.Error(s, ex);
                return;
            }

            CollectSubscriber parent = new CollectSubscriber(s, c, collector);

            source.Subscribe(parent);
        }

        sealed class CollectSubscriber : DeferredScalarSubscriber<T, C>
        {
            readonly Action<C, T> collector;

            internal CollectSubscriber(ISubscriber<C> actual, C collection, Action<C, T> collector) : base(actual)
            {
                this._value = collection;
                this.collector = collector;
            }

            public override void OnComplete()
            {
                Complete(_value);
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                try
                {
                    collector(_value, t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }
            }

            protected override void OnStart()
            {
                _subscription.Request(long.MaxValue);
            }
        }
    }
}
