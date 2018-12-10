namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Represents an IFlux with a key.
    /// </summary>
    /// <typeparam name="K">The key type</typeparam>
    /// <typeparam name="V">The value type</typeparam>
    public interface IGroupedFlux<K, V> : IFlux<V>
    {
        /// <summary>
        /// The key associated with this group.
        /// </summary>
        K Key { get; }
    }
}
