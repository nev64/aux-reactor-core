namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Provides an abstract asychronous boundary to operators.
    /// </summary>
    public interface IScheduler : IScheduling
    {
        /// <summary>
        /// Instructs this Scheduler to prepare itself for running tasks
        /// directly or through its Workers.
        /// </summary>
        void Start();

        /// <summary>
        /// Instructs this Scheduler to release all resources and reject
        /// any new tasks to be executed.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Creates a worker of this Scheduler that executed task in a strict
        /// FIFO order, guaranteed non-concurrently with each other.
        /// </summary>
        /// <returns>The Worker instance.</returns>
        IWorker CreateWorker();
    }
}
