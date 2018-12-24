using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Subscription
{
    /// <summary>
    /// Can hold onto a single value that may appear later
    /// and emits it on request.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    public class DeferredScalarSubscription<T> : IFlowSubscription<T>
    {
        /// <summary>
        /// The actual downstream subscriber.
        /// </summary>
        protected readonly ISubscriber<T> _actual;

        /// <summary>
        /// The current state.
        /// </summary>
        protected int _state;

        static readonly int NO_REQUEST_NO_VALUE = 0;
        static readonly int NO_REQUEST_HAS_VALUE = 1;
        static readonly int HAS_REQUEST_NO_VALUE = 2;
        static readonly int HAS_REQUEST_HAS_VALUE = 3;
        /// <summary>
        /// Indicates a cancelled state.
        /// </summary>
        protected static readonly int CANCELLED = 4;

        int _fusionState;

        static readonly int EMPTY = 1;
        static readonly int HAS_VALUE = 2;
        static readonly int COMPLETE = 3;

        /// <summary>
        /// The value storage.
        /// </summary>
        protected T _value;

        /// <summary>
        /// Constructs a DeferredScalarSubscription with the target ISubscriber.
        /// </summary>
        /// <param name="actual">The ISubscriber to send signals to.</param>
        protected DeferredScalarSubscription(ISubscriber<T> actual)
        {
            _actual = actual;
        }

        /// <summary>
        /// Signal an exception to the downstream ISubscriber.
        /// </summary>
        /// <param name="ex">The exception to signal</param>
        public virtual void Error(Exception ex)
        {
            _actual.OnError(ex);
        }

        /// <summary>
        /// Signal a valueless completion to the downsream ISubscriber.
        /// </summary>
        public virtual void Complete()
        {
            _actual.OnComplete();
        }

        /// <summary>
        /// Complete with the single value and emit it if
        /// there is request for it.
        /// This should be called at most once.
        /// </summary>
        /// <param name="v">The value to signal</param>
        public void Complete(T v)
        {
            for (;;)
            {
                int s = Volatile.Read(ref _state);
                if (s == CANCELLED || s == NO_REQUEST_HAS_VALUE || s == HAS_REQUEST_HAS_VALUE)
                {
                    return;
                }
                if (s == HAS_REQUEST_NO_VALUE)
                {
                    if (_fusionState == EMPTY)
                    {
                        _value = v;
                        _fusionState = HAS_VALUE;
                    }

                    _actual.OnNext(v);

                    if (Volatile.Read(ref _state) != CANCELLED)
                    {
                        _actual.OnComplete();
                    }

                    return;
                }
                _value = v;
                if (Interlocked.CompareExchange(ref _state, NO_REQUEST_HAS_VALUE, NO_REQUEST_NO_VALUE) == HAS_REQUEST_HAS_VALUE)
                {
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public void Request(long n)
        {
            if (!SubscriptionHelper.Validate(n))
            {
                return;
            }
            for (;;)
            {
                int s = Volatile.Read(ref _state);
                if (s == CANCELLED || s == HAS_REQUEST_NO_VALUE || s == HAS_REQUEST_HAS_VALUE)
                {
                    return;
                }
                if (s == NO_REQUEST_HAS_VALUE)
                {
                    if (Interlocked.CompareExchange(ref _state, HAS_REQUEST_HAS_VALUE, NO_REQUEST_HAS_VALUE) == NO_REQUEST_HAS_VALUE)
                    {
                        T v = _value;

                        if (_fusionState == EMPTY)
                        {
                            _fusionState = HAS_VALUE;
                        }

                        _actual.OnNext(v);

                        if (Volatile.Read(ref _state) != CANCELLED)
                        {
                            _actual.OnComplete();
                        }

                        return;
                    }
                    return;
                }
                if (Interlocked.CompareExchange(ref _state, HAS_REQUEST_NO_VALUE, NO_REQUEST_NO_VALUE) == NO_REQUEST_NO_VALUE)
                {
                    break;
                }
            }
        }


        /// <inheritdoc/>
        public FusionMode RequestFusion(FusionMode mode)
        {
            var m = mode & FusionMode.Async;
            if (m != FusionMode.None)
            {
                _fusionState = EMPTY;
            }
            return m;
        }

        /// <inheritdoc/>
        public bool Offer(T _)
        {
            return FuseableHelper.DontCallOffer();
        }

        /// <inheritdoc/>
        public Option<T> Poll()
        {    
            if (_fusionState == HAS_VALUE)
            {
                _fusionState = COMPLETE;
                return Just(_value);
            }
            return None<T>();
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return _fusionState != HAS_VALUE;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _value = default(T);
            _fusionState = COMPLETE;
        }

        /// <inheritdoc/>
        public virtual void Cancel()
        {
            Volatile.Write(ref _state, CANCELLED);
        }
    }
}
