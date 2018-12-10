using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// API for emitting signals based on requests.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public interface ISignalEmitter<T>
    {
        /// <summary>
        /// Signal the next value. Should be called at most once per generator invocation.
        /// </summary>
        /// <param name="t">The value to signal</param>
        void Next(T t);

        /// <summary>
        /// Signal an error. Can be called directly after calling <see cref="Next(T)"/>.
        /// </summary>
        /// <param name="e"></param>
        void Error(Exception e);

        /// <summary>
        /// Signal a completion. Can be called directly after calling <see cref="Next(T)"/>.
        /// </summary>
        void Complete();
    }
}
