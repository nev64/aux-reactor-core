namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Indicates when an error should be delivered in a Concat/ConcatMap operator.
    /// </summary>
    public enum ConcatErrorMode
    {
        /// <summary>
        /// If any of the participating IPublisher signals an OnError, that error is delivered immediately.
        /// </summary>
        Immediate,
        /// <summary>
        /// If any of the participating IPublisher signals an OnError, that error is delivered when the current
        /// inner IPublisher terminates. If multiple OnError signals happened,
        /// the downstream will receive all of them in an AggregateException.
        /// </summary>
        Boundary,
        /// <summary>
        /// If any of the participating IPublisher signals an OnError, that error is delivered only when
        /// the outer and all the inner IPublishers have terminated. If multiple OnError signals happened,
        /// the downstream will receive all of them in an AggregateException.
        /// </summary>
        End
    }
}
