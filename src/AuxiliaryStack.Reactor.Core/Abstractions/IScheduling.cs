using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Interface to indicate support for non-delayed scheduling of tasks.
    /// </summary>
    public interface IScheduling
    {
        /// <summary>
        /// Schedules the given task on this scheduler/worker non-delayed execution.
        /// </summary>
        /// <param name="task">task the task to execute</param>
        /// <returns>The Cancellation instance that let's one cancel this particular task.
        /// If the Scheduler has been shut down, the <see cref="SchedulerHelper.Rejected"/> instance is returned.</returns>
        IDisposable Schedule(Action task);
    }
}