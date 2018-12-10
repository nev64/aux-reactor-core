using AuxiliaryStack.Reactor.Core.Flow;

namespace AuxiliaryStack.Reactor.Core.Subscription
{
    /// <summary>
    /// A IQueueuSubscription that reject all fusion.
    /// </summary>
    /// <typeparam name="T">The output value type</typeparam>
    internal abstract class BasicRejectingSubscription<T> : IQueueSubscription<T>
    {
        public abstract void Cancel();

        public void Clear()
        {
            // ignored
        }

        public bool IsEmpty()
        {
            return false;
        }

        public bool Offer(T value)
        {
            return FuseableHelper.DontCallOffer();
        }

        public bool Poll(out T value)
        {
            value = default(T);
            return false;
        }

        public abstract void Request(long n);

        public int RequestFusion(int mode)
        {
            return FuseableHelper.NONE;
        }
    }
}
