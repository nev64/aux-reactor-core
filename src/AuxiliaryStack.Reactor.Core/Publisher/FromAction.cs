using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromAction<T>: IFlux<T>, IMono<T>
    {
        private readonly Action _action;

        public FromAction(Action action)
        {
            _action = action;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            try
            {
                _action();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<T>.Error(subscriber, ex);
                return;
            }
            EmptySubscription<T>.Complete(subscriber);
        }
    }
}
