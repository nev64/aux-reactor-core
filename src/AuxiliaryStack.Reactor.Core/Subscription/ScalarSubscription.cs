using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Subscription
{
    /// <summary>
    /// A fuseable subscription that emits a single value on request or poll.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    public sealed class ScalarSubscription<T> : IFlowSubscription<T>
    {
        const int READY = 0;//todo: enum
        const int REQUESTED = 1;
        const int CANCELLED = 2;
        
        private readonly ISubscriber<T> _actual;
        private readonly T _value;
        private int _state;

        /// <summary>
        /// Constructs a ScalarSubscription with the target ISubscriber and value to
        /// emit on request.
        /// </summary>
        /// <param name="actual">The target ISubscriber</param>
        /// <param name="value">The value to emit</param>
        public ScalarSubscription(ISubscriber<T> actual, T value)
        {
            _actual = actual;
            _value = value;
        }

        /// <inheritdoc/>
        public int RequestFusion(int mode)
        {
            if ((mode & FuseableHelper.SYNC) != 0)
            {
                return FuseableHelper.SYNC;
            }

            return FuseableHelper.NONE;
        }

        /// <inheritdoc/>
        public bool Offer(T value)
        {
            return FuseableHelper.DontCallOffer();
        }

        /// <inheritdoc/>
        public Option<T> Poll()
        {
            var s = _state;
            if (s == READY)
            {
                _state = REQUESTED;
                return Just(_value);
            }

            return None<T>();
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return _state != READY;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _state = REQUESTED;
        }

        /// <inheritdoc/>
        public void Request(long n)
        {
            if (SubscriptionHelper.Validate(n))
            {
                if (Interlocked.CompareExchange(ref _state, REQUESTED, READY) == READY)
                {
                    _actual.OnNext(_value);
                    if (Volatile.Read(ref _state) != CANCELLED)
                    {
                        _actual.OnComplete();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            Volatile.Write(ref _state, CANCELLED);
        }
    }
}