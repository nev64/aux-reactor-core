using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromActions : IFlux<Unit>
    {
        private readonly Action[] _actions;

        public FromActions(Action[] actions)
        {
            _actions = actions;
        }

        public void Subscribe(ISubscriber<Unit> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<Unit>());
            try
            {
                for (var i = 0; i < _actions.Length; i++)
                {
                    try
                    {
                        _actions[i]();
                        subscriber.OnNext(Unit.Instance);
                    }
                    catch (SystemException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        subscriber.OnError(ex);
                    }
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