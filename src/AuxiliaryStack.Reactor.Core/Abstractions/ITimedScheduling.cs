using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Interface to indicate support for delayed and periodic scheduling of tasks.
    /// </summary>
    public interface ITimedScheduling : ITimeSource, IScheduling
    {
        /// <summary>
        /// Schedules the execution of the given task with the given delay amount.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="delay">The delay amount</param>
        /// <returns>The IDisposable to cancel the task</returns>
        IDisposable Schedule(Action task, TimeSpan delay);

        /// <summary>
        /// Schedules a periodic execution of the given task with the given initial delay and period.
        /// </summary>
        /// <param name="task">The tast to execute periodically</param>
        /// <param name="initialDelay">The initial delay</param>
        /// <param name="period">The period amount</param>
        /// <returns>The IDisposable to cancel the periodic task</returns>
        IDisposable Schedule(Action task, TimeSpan initialDelay, TimeSpan period);
    }
}