﻿using System;
using System.Net.WebSockets;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSkip<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly long n;

        public PublisherSkip(IPublisher<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new SkipConditionalSubscriber((IConditionalSubscriber<T>) s, n));
            }
            else
            {
                source.Subscribe(new SkipSubscriber(s, n));
            }
        }

        sealed class SkipSubscriber : BasicFuseableSubscriber<T, T>
        {
            readonly long n;

            long remaining;

            public SkipSubscriber(ISubscriber<T> actual, long n) : base(actual)
            {
                this.n = n;
                this.remaining = n;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                long r = remaining;
                if (r == 0L)
                {
                    actual.OnNext(t);
                    return;
                }

                remaining = r - 1;
            }

            public override Option<T> Poll()
            {
                var qs = this.qs;
                long r = remaining;

                if (r == 0L)
                {
                    return qs.Poll();
                }

                T local;
                for (;;)
                {
                    var elem = qs.Poll();
                    if (elem.IsNone)
                    {
                        remaining = r;
                        return None<T>();
                    }

                    if (--r == 0)
                    {
                        remaining = 0;
                        return qs.Poll();
                    }
                }
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveAnyFusion(mode);
            }

            protected override void AfterSubscribe()
            {
                s.Request(n);
            }
        }

        sealed class SkipConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly long n;

            long remaining;

            public SkipConditionalSubscriber(IConditionalSubscriber<T> actual, long n) : base(actual)
            {
                this.n = n;
                this.remaining = n;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                long r = remaining;
                if (r == 0L)
                {
                    actual.OnNext(t);
                    return;
                }

                remaining = r - 1;
            }

            public override bool TryOnNext(T t)
            {
                long r = remaining;
                if (r == 0L)
                {
                    return actual.TryOnNext(t);
                }

                remaining = r - 1;
                return true;
            }

            public override Option<T> Poll()
            {
                var qs = this.qs;
                long r = remaining;

                if (r == 0L)
                {
                    return qs.Poll();
                }

                for (;;)
                {
                    var elem = qs.Poll();
                    if (elem.IsNone)
                    {
                        remaining = r;
                        return None<T>();
                    }

                    if (--r == 0)
                    {
                        remaining = 0;
                        return qs.Poll();
                    }
                }
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveAnyFusion(mode);
            }

            protected override void AfterSubscribe()
            {
                s.Request(n);
            }
        }
    }
}