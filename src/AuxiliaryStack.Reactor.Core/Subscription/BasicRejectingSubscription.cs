using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;

namespace AuxiliaryStack.Reactor.Core.Subscription
{
    /// <summary>
    /// A IQueueuSubscription that reject all fusion.
    /// </summary>
    /// <typeparam name="T">The output value type</typeparam>
    internal abstract class BasicRejectingSubscription<T> : IFlowSubscription<T>
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

        public Option<T> Poll()
        {
            return None<T>();
        }

        public abstract void Request(long n);

        public int RequestFusion(int mode)
        {
            return FuseableHelper.NONE;
        }
    }
}
