using System;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDefer<T> : IFlux<T>, IMono<T>
    {
        readonly Func<IPublisher<T>> supplier;

        internal PublisherDefer(Func<IPublisher<T>> supplier)
        {
            this.supplier = supplier;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            IPublisher<T> p;

            try
            {
                p = supplier();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<T>.Error(s, ex);
                return;
            }

            if (p == null)
            {
                EmptySubscription<T>.Error(s, new NullReferenceException("The supplier returned a null IPublisher"));
                return;
            }

            p.Subscribe(s);
        }
    }
}
