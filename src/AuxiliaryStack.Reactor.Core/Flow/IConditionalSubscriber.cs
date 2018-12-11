

namespace AuxiliaryStack.Reactor.Core.Flow
{
    /// <summary>
    /// Represents a conditional ISubscriber that has a TryOnNext() method
    /// to avoid requesting replenishment one-by-one
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    public interface IConditionalSubscriber<T> : ISubscriber<T>
    {
        /// <summary>
        /// Try signalling a value and return true if successful,
        /// false to indicate a new value can be immediately sent out.
        /// </summary>
        /// <param name="t">The value signalled</param>
        /// <returns>True if the value has been consumed, false if a new value can be
        /// sent immediately</returns>
        bool TryOnNext(T t);
    }
}
