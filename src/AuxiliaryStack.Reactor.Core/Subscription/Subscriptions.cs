namespace AuxiliaryStack.Reactor.Core.Subscription
{
    public static class Subscriptions
    {
        public static ISubscription Empty<T>() => EmptySubscription<T>.Instance;
    }
}