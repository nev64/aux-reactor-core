using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherEmpty<T> : IFlux<T>, IMono<T>
    {
        internal static readonly PublisherEmpty<T> Instance = new PublisherEmpty<T>();

        private PublisherEmpty()
        {

        }

        public void Subscribe(ISubscriber<T> s)
        {
            EmptySubscription<T>.Complete(s);
        }

        
    }
}
