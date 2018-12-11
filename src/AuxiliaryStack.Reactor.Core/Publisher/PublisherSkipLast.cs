using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSkipLast<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly long n;

        internal PublisherSkipLast(IPublisher<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new SkipLastConditionalSubscriber((IConditionalSubscriber<T>) s, n));
            }
            else
            {
                source.Subscribe(new SkipLastSubscriber(s, n));
            }
        }

        sealed class SkipLastSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly long n;

            readonly IFlow<T> _flow;

            ISubscription s;

            long size;

            internal SkipLastSubscriber(ISubscriber<T> actual, long n)
            {
                this.actual = actual;
                this.n = n;
                this._flow = new ArrayFlow<T>();
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                actual.OnComplete();
            }

            public void OnError(Exception e)
            {
                _flow.Clear();
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                long z = size;
                if (z != n)
                {
                    _flow.Offer(t);
                    size = z + 1;
                }
                else
                {
                    //todo: check
                    var u = _flow.Poll();
                    _flow.Offer(t);
                    actual.OnNext(u.GetValue(default(T)));
                }
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    actual.OnSubscribe(this);

                    s.Request(n);
                }
            }

            public void Request(long n)
            {
                s.Request(n);
            }
        }

        sealed class SkipLastConditionalSubscriber : IConditionalSubscriber<T>, ISubscription
        {
            readonly IConditionalSubscriber<T> actual;

            readonly long n;

            readonly IFlow<T> _flow;

            ISubscription s;

            long size;

            internal SkipLastConditionalSubscriber(IConditionalSubscriber<T> actual, long n)
            {
                this.actual = actual;
                this.n = n;
                this._flow = new ArrayFlow<T>();
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                actual.OnComplete();
            }

            public void OnError(Exception e)
            {
                _flow.Clear();
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                long z = size;
                if (z != n)
                {
                    _flow.Offer(t);
                    size = z + 1;
                }
                else
                {
                    var u= 
                    _flow.Poll();
                    _flow.Offer(t);
                    actual.OnNext(u.GetValue(default(T)));
                }
            }

            public bool TryOnNext(T t)
            {
                long z = size;
                if (z != n)
                {
                    _flow.Offer(t);
                    size = z + 1;
                    return true;
                }

                //todo:check
                Option<T> u =_flow.Poll();
                _flow.Offer(t);
                return actual.TryOnNext(u.GetValue(default(T)));
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    actual.OnSubscribe(this);

                    s.Request(n);
                }
            }

            public void Request(long n)
            {
                s.Request(n);
            }
        }
    }
}