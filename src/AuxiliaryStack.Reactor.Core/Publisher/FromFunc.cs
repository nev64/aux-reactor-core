using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromFunc<T> : IFlux<T>, IMono<T>
    {
        private readonly Func<T> _supplier;

        public FromFunc(Func<T> supplier)
        {
            _supplier = supplier;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<T>());

            try
            {
                var result = _supplier();
                try
                {
                    subscriber.OnNext(result);
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
            finally
            {
                subscriber.OnComplete();
            }
        }
    }
}
