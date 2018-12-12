﻿using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    /// <summary>
    /// Abstract base class for ISubscribers that may receive an upstream
    /// IQueueSubscription and themselves want to offer IQueueSubscription.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="U">The output value type.</typeparam>
    internal abstract class BasicFuseableSubscriber<T, U> : ISubscriber<T>, IFlowSubscription<U>
    {
        /// <summary>
        /// The actual child ISubscriber.
        /// </summary>
        protected readonly ISubscriber<U> actual;

        protected bool done;

        protected ISubscription s;

        /// <summary>
        /// If not null, the upstream is fuseable.
        /// </summary>
        protected IFlowSubscription<T> qs;

        /// <summary>
        /// The established fusion mode. See <see cref="FuseableHelper"/> constants.
        /// </summary>
        protected int fusionMode;

        internal BasicFuseableSubscriber(ISubscriber<U> actual)
        {
            this.actual = actual;
        }

        public virtual void Cancel()
        {
            s.Cancel();
        }

        public abstract void OnComplete();

        public abstract void OnError(Exception e);

        public abstract void OnNext(T t);

        public virtual void OnSubscribe(ISubscription s)
        {
            if (SubscriptionHelper.Validate(ref this.s, s))
            {
                qs = s as IFlowSubscription<T>;

                if (BeforeSubscribe())
                {
                    actual.OnSubscribe(this);

                    AfterSubscribe();
                }
            }
        }

        public virtual void Request(long n)
        {
            s.Request(n);
        }

        /// <summary>
        /// Called after a successful OnSubscribe call but
        /// before the downstream's OnSubscribe is called with this.
        /// </summary>
        /// <returns>True if calling the downstream's OnSubscribe can happen.</returns>
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
            if (done)
            {
                return;
            }
            done = true;
            actual.OnComplete();
        }

        /// <summary>
        /// Signal an error to the actual ISubscriber if the sequence is not already done.
        /// </summary>
        protected void Error(Exception ex)
        {
            if (done)
            {
                ExceptionHelper.OnErrorDropped(ex);
                return;
            }
            done = true;
            actual.OnError(ex);
        }

        /// <summary>
        /// Rethrows a fatal exception, cancels the ISubscription and
        /// calls <see cref="Error(Exception)"/>
        /// </summary>
        /// <param name="ex">The exception to rethrow or signal</param>
        protected void Fail(Exception ex)
        {
            ExceptionHelper.ThrowIfFatal(ex);
            s.Cancel();
            OnError(ex);
        }

        public abstract int RequestFusion(int mode);

        public bool Offer(U value)
        {
            return FuseableHelper.DontCallOffer();
        }

        public abstract Option<U> Poll();

        /// <inheritdoc/>
        public virtual bool IsEmpty()
        {
            return qs.IsEmpty();
        }

        /// <inheritdoc/>
        public virtual void Clear()
        {
            qs.Clear();
        }

        /// <summary>
        /// Forward the mode request to the upstream IQueueSubscription and
        /// set the mode it returns.
        /// If the upstream is not an IQueueSubscription, <see cref="FuseableHelper.NONE"/>
        /// is returned.
        /// </summary>
        /// <param name="mode">The incoming fusion mode.</param>
        /// <returns>The established fusion mode</returns>
        protected int TransitiveAnyFusion(int mode)
        {
            var qs = this.qs;
            if (qs != null)
            {
                int m = qs.RequestFusion(mode);
                fusionMode = m;
                return m;
            }
            return FuseableHelper.NONE;
        }

        /// <summary>
        /// Unless the mode contains the <see cref="FuseableHelper.BOUNDARY"/> flag,
        /// forward the mode request to the upstream IQueueSubscription and
        /// set the mode it returns.
        /// If the upstream is not an IQueueSubscription, <see cref="FuseableHelper.NONE"/>
        /// is returned.
        /// </summary>
        /// <param name="mode">The incoming fusion mode.</param>
        /// <returns>The established fusion mode</returns>
        protected int TransitiveBoundaryFusion(int mode)
        {
            var qs = this.qs;
            if (qs != null)
            {
                if ((mode & FuseableHelper.BOUNDARY) != 0)
                {
                    return FuseableHelper.NONE;
                }
                int m = qs.RequestFusion(mode);
                fusionMode = m;
                return m;
            }
            return FuseableHelper.NONE;
        }
    }
}
