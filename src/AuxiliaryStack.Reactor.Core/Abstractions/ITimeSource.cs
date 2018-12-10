namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Interface indicating a Time source that can tell the current time in UTC
    /// milliseconds
    /// </summary>
    public interface ITimeSource
    {
        /// <summary>
        /// The current UTC time.
        /// </summary>
        long NowUtc { get; }
    }
}