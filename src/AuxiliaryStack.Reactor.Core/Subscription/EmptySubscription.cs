using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Subscription
{
    /// <summary>
    /// Represents an empty subscription that ignores requests and cancellation.
    /// </summary>
    /// <typeparam name="T">The value type (no value is emitted)</typeparam>
    public sealed class EmptySubscription<T> : IFlowSubscription<T>
    {
        private EmptySubscription()
        {
        }

        /// <summary>
        /// Returns the singleton instance of the EmptySubscription class.
        /// </summary>
        public static EmptySubscription<T> Instance { get; } = new EmptySubscription<T>();

        /// <summary>
        /// Sets the empty instance on the ISubscriber and calls OnError with the Exception.
        /// </summary>
        /// <param name="s">The target ISubscriber</param>
        /// <param name="ex">The exception to send</param>
        public static void Error(ISubscriber<T> s, Exception ex)
        {
            s.OnSubscribe(Instance);
            s.OnError(ex);
        }

        /// <summary>
        /// Sets the empty instance on the ISubscriber and calls OnComplete.
        /// </summary>
        /// <param name="s">The target ISubscriber</param>
        public static void Complete(ISubscriber<T> s)
        {
            s.OnSubscribe(Instance);
            s.OnComplete();
        }

        /// <inheritdoc />
        public void Cancel()
        {
            // deliberately ignored
        }

        /// <inheritdoc />
        public void Clear()
        {
            // deliberately ignored
        }

        /// <inheritdoc />
        public bool IsEmpty() => true;

        /// <inheritdoc />
        public bool Offer(T value) => FuseableHelper.DontCallOffer();

        /// <inheritdoc />
        public Option<T> Poll() => None<T>();

        /// <inheritdoc />
        public void Request(long n)
        {
            // deliberately ignored
        }

        /// <inheritdoc />
        public int RequestFusion(int mode) => mode & FuseableHelper.ASYNC;
    }
}
