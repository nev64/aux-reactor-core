using System;

namespace AuxiliaryStack.Reactor.Core.Flow
{

    [Flags]
    public enum FusionMode
    {
        None = 0,
        Sync = 1,
        Async = 2,
        Any = Sync | Async,
        Boundary = 4
    }
    /// <summary>
    /// Constants for <see cref="IFlowSubscription{T}.RequestFusion"/> parameter
    /// and return types.
    /// </summary>
    public static class FuseableHelper
    {
        /// <summary>
        /// Handle the case when the <see cref="IFlow{T}.Offer(T)"/> is called on a
        /// <see cref="IFlowSubscription{T}"/>.
        /// </summary>
        /// <returns>Never completes normally.</returns>
        public static bool DontCallOffer()
        {
            throw new InvalidOperationException("IQueueSubscription.Offer mustn't be called.");
        }
    }
}
