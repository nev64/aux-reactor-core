using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    /// <summary>
    /// A constant scalar source.
    /// </summary>
    /// <remarks>
    /// It is based on IMono to facilitate easy reuse for Flux and Mono extension methods.
    /// </remarks>
    /// <typeparam name="T">The value type</typeparam>
    internal sealed class PublisherJust<T> : IFlux<T>, IMono<T>, IScalarCallable<T>
    {
        readonly T value;

        internal PublisherJust(T value)
        {
            this.value = value;
        }

        public T Value
        {
            get
            {
                return value;
            }
        }

        public void Subscribe(ISubscriber<T> s)
        {
            s.OnSubscribe(new ScalarSubscription<T>(s, value));
        }
    }
}
