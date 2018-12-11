using System;

namespace AuxiliaryStack.Reactor.Core
{
    public interface ISubscriber<in T> 
    {
        void OnSubscribe(ISubscription subscription);
        void OnNext(T value);
        void OnError(Exception error);
        void OnComplete();
    }
}