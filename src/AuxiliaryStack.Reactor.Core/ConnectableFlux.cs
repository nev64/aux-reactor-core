using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Extension methods to work with <seealso cref="IConnectableFlux{T}"/> instances.
    /// </summary>
    public static class ConnectableFlux
    {
        /// <summary>
        /// Automatically connect to the source if the number of arriving
        /// ISubscribers reaches the specified number.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="source">The source IConnectableFlux to connect to.</param>
        /// <param name="n">The minimum number of Subscribers to connect to the source.
        /// Zero connects immediately.</param>
        /// <param name="onConnect">The callback to receive a disposable that let's disconnect
        /// the established connection.</param>
        /// <returns>The new IFlux instance.</returns>
        public static IFlux<T> AutoConnect<T>(this IConnectableFlux<T> source, int n = 1, Action<IDisposable> onConnect = null)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
