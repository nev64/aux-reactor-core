using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromFuncs<T> : IFlux<T>, IMono<T>
    {
        private readonly Func<T>[] _suppliers;

        public FromFuncs(params Func<T>[] suppliers)
        {
            _suppliers = suppliers;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<T>());

            try
            {
                for (var i = 0; i < _suppliers.Length; i++)
                {
                    try
                    {
                        var result = _suppliers[i]();
                        try
                        {
                            subscriber.OnNext(result);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    catch (Exception ex)
                    {
                        subscriber.OnError(new DelegateInvocationException(ex, i));
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
