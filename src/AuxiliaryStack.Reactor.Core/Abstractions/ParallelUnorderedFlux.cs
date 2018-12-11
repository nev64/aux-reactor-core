

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Base class for unordered parallel stream processing.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public abstract class ParallelUnorderedFlux<T> : IParallelFlux<T>
    {
        /// <inheritdoc/>
        public bool IsOrdered => false;

        /// <inheritdoc/>
        public abstract int Parallelism { get; }

        /// <inheritdoc/>
        public abstract void Subscribe(ISubscriber<T>[] subscribers);

    }
}