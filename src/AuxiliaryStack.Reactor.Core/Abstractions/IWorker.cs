using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// A worker representing an asynchronous boundary that executes tasks in
    /// a FIFO order, guaranteed non-concurrently with respect to each other.
    /// </summary>
    public interface IWorker : IDisposable, IScheduling
    {
    }
}