using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromActions : IFlux<int>
    {
        private readonly Action[] _actions;

        public FromActions(Action[] actions)
        {
            _actions = actions;
        }

        public void Subscribe(ISubscriber<int> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<int>());
            try
            {
                for (var i = 0; i < _actions.Length; i++)
                {
                    try
                    {
                        _actions[i]();
                        try
                        {
                            subscriber.OnNext(i);
                        }
                        catch
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