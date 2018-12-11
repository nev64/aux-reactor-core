namespace AuxiliaryStack.Reactor.Core
{
    public interface ISubscription 
    {
        void Request(long n);
        void Cancel();
    }
}