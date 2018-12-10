using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherNever<T> : IFlux<T>, IMono<T>
    {
        internal static readonly PublisherNever<T> Instance = new PublisherNever<T>();

        private PublisherNever()
        {

        }

        public void Subscribe(ISubscriber<T> s)
        {
            s.OnSubscribe(NeverSubscription<T>.Instance);
        }
    }
}
