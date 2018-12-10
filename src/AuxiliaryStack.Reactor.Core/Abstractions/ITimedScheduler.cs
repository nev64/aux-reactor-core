namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Provides an abstract, timed asynchronous boundary to operators.
    /// </summary>
    public interface ITimedScheduler : IScheduler, ITimedScheduling
    {
        /// <summary>
        /// Creates a timed worker of this Scheduler that executed task in a strict
        /// FIFO order, guaranteed non-concurrently with each other.
        /// </summary>
        /// <returns></returns>
        ITimedWorker CreateTimedWorker();

    }
}