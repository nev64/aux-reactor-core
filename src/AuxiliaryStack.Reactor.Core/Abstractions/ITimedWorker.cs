namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// A timed worker representing an asynchronous boundary that executes tasks in
    /// a FIFO order, possibly delayed and guaranteed non-concurrently with respect
    /// to each other (delayed or non-delayed alike).
    /// </summary>
    public interface ITimedWorker : IWorker, ITimedScheduling
    {
    }
}