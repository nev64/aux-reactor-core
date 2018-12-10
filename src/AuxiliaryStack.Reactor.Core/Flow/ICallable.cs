namespace AuxiliaryStack.Reactor.Core.Flow
{
    /// <summary>
    /// Indicates an IPublisher holds a single value that can be computed or
    /// retrieved at subscription time.
    /// </summary>
    /// <typeparam name="T">The returned value type.</typeparam>
    public interface ICallable<out T>
    {
        /// <summary>
        /// Returns the value.
        /// </summary>
        T Value { get; }
    }
}
