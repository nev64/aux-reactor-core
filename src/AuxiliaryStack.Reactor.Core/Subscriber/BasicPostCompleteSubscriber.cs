using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    /// <summary>
    /// Base class for operators that want to signal one extra value after the 
    /// upstream has completed.
    /// Call <seealso cref="Complete(TPub)"/> to signal the post-complete value.
    /// </summary>
    /// <typeparam name="TSub">The input value type.</typeparam>
    /// <typeparam name="TPub">The output value type</typeparam>
    internal abstract class BasicSinglePostCompleteSubscriber<TSub, TPub> : BasicSubscriber<TSub, TPub>, IQueue<TPub>
    {
        private TPub _last;
        private bool _hasValue;
        private long _requested;
        private bool _isCancelled;

        /// <summary>
        /// Tracks the amount produced before completion.
        /// </summary>
        protected long _produced;

        protected BasicSinglePostCompleteSubscriber(ISubscriber<TPub> actual) : base(actual)
        {
        }

        /// <summary>
        /// Prepares the value to be the post-complete value.
        /// </summary>
        /// <param name="value">The value to signal post-complete.</param>
        protected void Complete(TPub value)
        {
            var p = _produced;
            if (p != 0L)
            {
                Produced(p);
            }
            _last = value;
            _hasValue = true;
            BackpressureHelper.PostComplete(ref _requested, _actual, this, ref _isCancelled);
        }

        public sealed override void Request(long n)
        {
            if (SubscriptionHelper.Validate(n))
            {
                if (!BackpressureHelper.PostCompleteRequest(ref _requested, n, _actual, this, ref _isCancelled))
                {
                    _subscription.Request(n);
                }
            }
        }

        /// <summary>
        /// Atomically subtract the given amount from the requested amount.
        /// </summary>
        /// <param name="n">The value to subtract, positive (not verified)</param>
        private void Produced(long n) => Interlocked.Add(ref _requested, -n);

        public bool Offer(TPub value) => FuseableHelper.DontCallOffer();

        public bool Poll(out TPub value)
        {
            if (_hasValue)
            {
                _hasValue = false;
                value = _last;
                _last = default;
                return true;
            }
            value = default;
            return false;
        }

        public bool IsEmpty() => !_hasValue;

        public void Clear()
        {
            _last = default;
            _hasValue = false;
        }

        public sealed override void Cancel()
        {
            Volatile.Write(ref _isCancelled, true);
            base.Cancel();
        }
    }
}
