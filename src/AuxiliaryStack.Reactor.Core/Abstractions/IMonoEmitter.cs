using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// API surface to signal completion, 1 element followed by a completion or 1 error only,
    /// hiding an actual ISubscriber.
    /// </summary>
    public interface IMonoEmitter<in T>
    {
        /// <summary>
        /// Signal the single value and complete. Disposes any associated resource.
        /// </summary>
        /// <param name="t">The value.</param>
        void Complete(T t);

        /// <summary>
        /// Signal an error. Disposes any associated resource.
        /// </summary>
        /// <param name="e"></param>
        void Error(Exception e);

        /// <summary>
        /// Signal a completion. Disposes any associated resource.
        /// </summary>
        void Complete();

        /// <summary>
        /// Indicate no more signals will follow. Further calls
        /// to the other methods are ignored.
        /// </summary>
        void Stop();

        /// <summary>
        /// Associate a resource with the emitter that should
        /// be disposed on completion or cancellation
        /// </summary>
        /// <param name="d">The resource to associate.</param>
        void SetDisposable(IDisposable d);
    }
}
