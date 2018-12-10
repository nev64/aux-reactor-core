using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Represents a connectable IFlux that starts streaming
    /// only when the <see cref="Connect(Action{IDisposable})"/> is called.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    public interface IConnectableFlux<T> : IFlux<T>
    {
        /// <summary>
        /// Connect to the upstream IFlux.
        /// </summary>
        /// <param name="onConnect">If given, it is called with a disposable instance that allows in-sequence connectioncancellation.</param>
        /// <returns>The IDisposable to cancel the connection</returns>
        IDisposable Connect(Action<IDisposable> onConnect = null);
    }
}
