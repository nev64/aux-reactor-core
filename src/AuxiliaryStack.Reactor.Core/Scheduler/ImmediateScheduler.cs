using System;
using AuxiliaryStack.Reactor.Core.Util;

namespace AuxiliaryStack.Reactor.Core.Scheduler
{
    /// <summary>
    /// A basic scheduler that executes actions immediately on the caller thread.
    /// Useful in conjunction with <see cref="Flux.PublishOn{T}(IFlux{T}, IScheduler, int, bool)"/>
    /// to rebatch downstream requests.
    /// </summary>
    public sealed class ImmediateScheduler : IScheduler, IWorker
    {
        static readonly ImmediateScheduler INSTANCE = new ImmediateScheduler();

        /// <summary>
        /// Returns the singleton instance of this scheduler.
        /// </summary>
        public static ImmediateScheduler Instance { get { return INSTANCE; } }

        /// <inheritdoc/>
        public IWorker CreateWorker()
        {
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // ignored
        }

        /// <inheritdoc/>
        public IDisposable Schedule(Action task)
        {
            task();
            return DisposableHelper.Disposed;
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            // ignored
        }

        /// <inheritdoc/>
        public void Start()
        {
            // ignored
        }
    }
}
