﻿using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    internal abstract class BasicFuseableConditionalSubscriber<T, U> : IConditionalSubscriber<T>, IFlowSubscription<U>
    {
        /// <summary>
        /// The actual child ISubscriber.
        /// </summary>
        protected readonly IConditionalSubscriber<U> actual;

        protected bool done;

        protected ISubscription s;

        /// <summary>
        /// If not null, the upstream is fuseable.
        /// </summary>
        protected IFlowSubscription<T> qs;

        protected FusionMode fusionMode;

        internal BasicFuseableConditionalSubscriber(IConditionalSubscriber<U> actual)
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

        public abstract bool TryOnNext(T t);

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

        public abstract FusionMode RequestFusion(FusionMode mode);

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
        protected FusionMode TransitiveAnyFusion(FusionMode mode)
        {
            var qs = this.qs;
            if (qs != null)
            {
                var m = qs.RequestFusion(mode);
                fusionMode = m;
                return m;
            }
            return FusionMode.None;
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
        protected FusionMode TransitiveBoundaryFusion(FusionMode mode)
        {
            var qs = this.qs;
            if (qs != null)
            {
                if ((mode & FusionMode.Boundary) != 0)
                {
                    return FusionMode.None;
                }
                var m = qs.RequestFusion(mode);
                fusionMode = m;
                return m;
            }
            return FusionMode.None;
        }
    }
}
