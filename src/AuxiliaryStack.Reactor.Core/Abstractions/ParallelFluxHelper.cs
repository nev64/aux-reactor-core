using System;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core
{
    internal static class ParallelFluxHelper
    {
        /// <summary>
        /// Validate that the parallelism of the IParallelFlux equals to the number of elements in
        /// the ISubscriber array. If not, each ISubscriber is notified with an ArgumentException.
        /// </summary>
        /// <typeparam name="T">The element type of the parallel flux</typeparam>
        /// <typeparam name="U">The element type of the ISubscriber array (could be T or IOrderedItem{T})</typeparam>
        /// <param name="pf">The parent IParallelFlux instance</param>
        /// <param name="subscribers">The ISubscriber array</param>
        /// <returns>True if the subscribers were valid</returns>
        internal static bool Validate<T, U>(this IParallelFlux<T> pf, ISubscriber<U>[] subscribers)
        {
            if (pf.Parallelism != subscribers.Length)
            {
                Exception ex = new ArgumentException("Parallelism(" + pf.Parallelism + ") != Subscriber count (" + subscribers.Length + ")");
                foreach (ISubscriber<U> s in subscribers)
                {
                    EmptySubscription<U>.Error(s, ex);
                }
                return false;
            }
            return true;
        }

    }
}