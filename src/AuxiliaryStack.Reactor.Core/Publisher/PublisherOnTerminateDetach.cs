﻿using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Monads.Extensions;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherOnTerminateDetach<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        internal PublisherOnTerminateDetach(IPublisher<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new OnTerminateDetachConditionalSubscriber((IConditionalSubscriber<T>)s));
            }
            else
            {
                source.Subscribe(new OnTerminateDetachSubscriber(s));
            }
        }

        abstract class BaseOnTerminateDetachSubscriber : IFlowSubscription<T>
        {
            ISubscription s;

            IFlowSubscription<T> qs;

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    qs = s as IFlowSubscription<T>;

                    subscribeActual();
                }
            }

            protected abstract void subscribeActual();

            public FusionMode RequestFusion(FusionMode mode)
            {
                var qs = this.qs;
                if (qs != null)
                {
                    return qs.RequestFusion(mode);
                }
                return FusionMode.None;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<T> Poll() =>
                this.qs.ToOption()
                    .Bind(_ => _.Poll());

            public bool IsEmpty()
            {
                var qs = this.qs;
                if (qs != null)
                {
                    return qs.IsEmpty();
                }
                return true;
            }

            public void Clear()
            {
                var qs = this.qs;
                Cleanup();
                qs?.Clear();
            }

            public void Request(long n)
            {
                s?.Request(n);
            }

            public void Cancel()
            {
                var s = this.s;
                Cleanup();
                s.Cancel();
            }

            protected void Cleanup()
            {
                nullActual();
                this.s = SubscriptionHelper.Cancelled;
                qs = null;
            }

            protected abstract void nullActual();
        }

        sealed class OnTerminateDetachSubscriber : BaseOnTerminateDetachSubscriber, ISubscriber<T>
        {
            ISubscriber<T> actual;

            internal OnTerminateDetachSubscriber(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public void OnNext(T t)
            {
                actual?.OnNext(t);
            }

            public void OnError(Exception e)
            {
                var a = actual;
                Cleanup();
                a?.OnError(e);
            }

            public void OnComplete()
            {
                var a = actual;
                Cleanup();
                a?.OnComplete();
            }

            protected override void subscribeActual()
            {
                actual.OnSubscribe(this);
            }

            protected override void nullActual()
            {
                actual = null;
            }
        }

        sealed class OnTerminateDetachConditionalSubscriber : BaseOnTerminateDetachSubscriber, IConditionalSubscriber<T>
        {
            IConditionalSubscriber<T> actual;

            internal OnTerminateDetachConditionalSubscriber(IConditionalSubscriber<T> actual)
            {
                this.actual = actual;
            }

            public void OnNext(T t)
            {
                actual?.OnNext(t);
            }

            public bool TryOnNext(T t)
            {
                var a = actual;
                if (a != null)
                {
                    return a.TryOnNext(t);
                }
                return false;
            }

            public void OnError(Exception e)
            {
                var a = actual;
                Cleanup();
                a?.OnError(e);
            }

            public void OnComplete()
            {
                var a = actual;
                Cleanup();
                a?.OnComplete();
            }

            protected override void subscribeActual()
            {
                actual.OnSubscribe(this);
            }

            protected override void nullActual()
            {
                actual = null;
            }
        }
    }
}
