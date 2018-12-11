namespace AuxiliaryStack.Reactor.Core
{
    public interface IPublisher<out T>
    {
        void Subscribe(ISubscriber<T> subscriber);
    }
}