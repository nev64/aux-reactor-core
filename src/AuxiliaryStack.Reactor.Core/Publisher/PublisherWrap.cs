

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherWrap<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        internal PublisherWrap(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(s);
        }
    }
}
