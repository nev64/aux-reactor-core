namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Handling of backpressure for FluxEmitter and IObservable conversions.
    /// </summary>
    public enum BackpressureHandling
    {
        /// <summary>
        /// Completely ignore backpressure.
        /// </summary>
        None,
        /// <summary>
        /// Signal an error if the downstream can't keep up.
        /// </summary>
        Error,
        /// <summary>
        /// Drop the overflown item.
        /// </summary>
        Drop,
        /// <summary>
        /// Keep only the latest item.
        /// </summary>
        Latest,
        /// <summary>
        /// Buffer all items.
        /// </summary>
        Buffer
    }
}
