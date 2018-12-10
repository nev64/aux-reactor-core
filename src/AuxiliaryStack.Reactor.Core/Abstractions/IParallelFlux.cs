using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Base interface for parallel stream processing.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public interface IParallelFlux<out T>
    {
        /// <summary>
        /// True if the underlying implementation is ordered.
        /// </summary>
        bool IsOrdered { get; }

        /// <summary>
        /// The parallelism level of this IParallelFlux.
        /// </summary>
        int Parallelism { get; }

        /// <summary>
        /// Subscribes an array of ISubscribers, one for each rail.
        /// </summary>
        /// <param name="subscribers">The array of subscribers, its length must be equal
        /// to the <see cref="Parallelism"/> value.</param>
        void Subscribe(ISubscriber<T>[] subscribers);

    }
}
