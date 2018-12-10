using System;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherAction<T>: IFlux<T>, IMono<T>
    {
        readonly Action action;

        public PublisherAction(Action action)
        {
            this.action = action;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<T>.Error(s, ex);
                return;
            }
            EmptySubscription<T>.Complete(s);
        }
    }
}
