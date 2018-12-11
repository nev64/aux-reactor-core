using System;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    /// <summary>
    /// Base class for subscribers with an actual ISubscriber, a done flag and
    /// a sequentially set ISubscription.
    /// </summary>
    /// <typeparam name="TSub">The input value type</typeparam>
    /// <typeparam name="TPub">The output value type</typeparam>
    internal abstract class BasicSubscriber<TSub, TPub> : ISubscriber<TSub>, ISubscription
    {
        protected readonly ISubscriber<TPub> _actual;
        protected bool _isCompleted;
        protected ISubscription _subscription;

        internal BasicSubscriber(ISubscriber<TPub> actual) => _actual = actual;

        public virtual void Cancel() => _subscription.Cancel();

        public abstract void OnComplete();

        public abstract void OnError(Exception e);

        public abstract void OnNext(TSub t);

        public void OnSubscribe(ISubscription subscription)
        {
            if (SubscriptionHelper.Validate(ref _subscription, subscription))
            {
                if (BeforeSubscribe())
                {
                    _actual.OnSubscribe(this);

                    AfterSubscribe();
                }
            }
        }

        public virtual void Request(long n)
        {
            _subscription.Request(n);
        }

        /// <summary>
        /// Called after a successful OnSubscribe call but
        /// before the downstream's OnSubscribe is called with this.
        /// </summary>
        protected virtual bool BeforeSubscribe()
        {
            return true;
        }

        /// <summary>
        /// Called once the OnSubscribe has been called the first time
        /// and this has been set on the child ISubscriber.
        /// </summary>
        protected virtual void AfterSubscribe()
        {

        }

        /// <summary>
        /// Complete the actual ISubscriber if the sequence is not already done.
        /// </summary>
        protected void Complete()
        {
            if (_isCompleted)
            {
                return;
            }
            _isCompleted = true;
            _actual.OnComplete();
        }

        /// <summary>
        /// Signal an error to the actual ISubscriber if the sequence is not already done.
        /// </summary>
        protected void Error(Exception ex)
        {
            if (_isCompleted)
            {
                ExceptionHelper.OnErrorDropped(ex);
                return;
            }
            _isCompleted = true;
            _actual.OnError(ex);
        }

        /// <summary>
        /// Rethrows a fatal exception, cancels the ISubscription and
        /// calls <see cref="Error(Exception)"/>
        /// </summary>
        /// <param name="ex">The exception to rethrow or signal</param>
        protected void Fail(Exception ex)
        {
            ExceptionHelper.ThrowIfFatal(ex);
            _subscription.Cancel();
            OnError(ex);
        }
    }
}
