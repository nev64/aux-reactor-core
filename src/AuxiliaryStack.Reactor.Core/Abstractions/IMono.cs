using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// A Reactive Streams <see cref="Reactive.Streams.IPublisher{T}">Publisher</see>
    /// with basic rx operators that completes successfully by emitting an element, or
    /// with an error.
    /// </summary>
    public interface IMono<out T> : IPublisher<T>
    {
    }
}
