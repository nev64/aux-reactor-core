﻿using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromAction : IFlux<Unit>, IMono<Unit>
    {
        private readonly Action _action;

        public FromAction(Action action)
        {
            _action = action;
        }

        public void Subscribe(ISubscriber<Unit> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<Unit>());

            try
            {
                _action();
                try
                {
                    subscriber.OnNext(Unit.Instance);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            catch (SystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                subscriber.OnError(ex);
            }
            finally
            {
                subscriber.OnComplete();
            }
        }
    }
}