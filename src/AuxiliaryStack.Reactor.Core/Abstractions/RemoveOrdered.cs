using System;


namespace AuxiliaryStack.Reactor.Core
{
    internal sealed class RemoveOrdered<T> : ISubscriber<IOrderedItem<T>>, ISubscription
    {
        private readonly ISubscriber<T> _actual;

        private ISubscription _subscription;

        public RemoveOrdered(ISubscriber<T> actual) => _actual = actual;

        public void Cancel() => _subscription.Cancel();

        public void OnComplete() => _actual.OnComplete();

        public void OnError(Exception e) => _actual.OnError(e);

        public void OnNext(IOrderedItem<T> t) => _actual.OnNext(t.Value);

        public void OnSubscribe(ISubscription subscription)
        {
            _subscription = subscription;
            _actual.OnSubscribe(this);
        }

        public void Request(long n) => _subscription.Request(n);
    }
}