using System;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class FromNullFactoryFunc<T> : IFlux<T>, IMono<T>
        where T : class
    {
        private readonly Func<T> _supplier;

        public FromNullFactoryFunc(Func<T> supplier)
        {
            _supplier = supplier;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            subscriber.OnSubscribe(Subscriptions.Empty<T>());

            try
            {
                var result = _supplier();
                if (result == null)
                {
                    return;
                }

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