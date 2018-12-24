using System;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    /// <summary>
    /// A subscriber that takes values and produces a single output in a fuseable,
    /// backpressure aware manner.
    /// </summary>
    /// <typeparam name="T">The input value type</typeparam>
    /// <typeparam name="R">The single output value type</typeparam>
    public abstract class DeferredScalarSubscriber<T, R> : DeferredScalarSubscription<R>, ISubscriber<T>
    {
        /// <summary>
        /// The ISubscription to the upstream.
        /// </summary>
        protected ISubscription _subscription;

        /// <summary>
        /// Constructs an instance with the given actual downstream subscriber.
        /// </summary>
        /// <param name="actual">The downstream subscriber</param>
        protected DeferredScalarSubscriber(ISubscriber<R> actual) : base(actual)
        {
        }

        /// <inheritdoc/>
        public abstract void OnComplete();

        /// <inheritdoc/>
        public abstract void OnError(Exception e);

        /// <inheritdoc/>
        public abstract void OnNext(T value);

        /// <inheritdoc/>
        public void OnSubscribe(ISubscription subscription)
        {
            if (SubscriptionHelper.Validate(ref _subscription, subscription))
            {
                _actual.OnSubscribe(this);

                OnStart();
            }
        }

        /// <summary>
        /// Called after this instance has been sent to downstream via OnSubscribe.
        /// </summary>
        protected virtual void OnStart()
        {

        }

        /// <inheritdoc/>
        public override void Cancel()
        {
            base.Cancel();
            _subscription.Cancel();
        }

        /// <summary>
        /// Set the done flag and signal the exception.
        /// </summary>
        /// <param name="ex">The exception to signal</param>
        public override void Error(Exception ex)
        {
            if (Volatile.Read(ref _state) == CANCELLED)
            {
                ExceptionHelper.OnErrorDropped(ex);
                return;
            }
            base.Cancel();
            base.Error(ex);
        }

        /// <summary>
        /// Rethrow the exception if fatal, otherwise cancel the subscription
        /// and signal the exception via <see cref="Error(Exception)"/>.
        /// </summary>
        /// <param name="ex">The exception to check and signal</param>
        protected void Fail(Exception ex)
        {
            ExceptionHelper.ThrowIfFatal(ex);
            _subscription.Cancel();
            OnError(ex);
        }
    }
}
