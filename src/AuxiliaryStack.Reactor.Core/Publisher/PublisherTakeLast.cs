using System;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherTakeLast<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly long n;

        internal PublisherTakeLast(IPublisher<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new TakeLastSubscriber(s, n));
        }

        sealed class TakeLastSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly long n;

            readonly IQueue<T> queue;

            ISubscription s;

            long size;

            long requested;

            bool cancelled;

            internal TakeLastSubscriber(ISubscriber<T> actual, long n)
            {
                this.actual = actual;
                this.n = n;
                this.queue = new ArrayQueue<T>();
            }

            public void Cancel()
            {
                Volatile.Write(ref cancelled, true);
                s.Cancel();
            }

            public void OnComplete()
            {
                BackpressureHelper.PostComplete(ref requested, actual, queue, ref cancelled);
            }

            public void OnError(Exception e)
            {
                queue.Clear();
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                long z = size;
                if (z == n)
                {
                    T u;
                    queue.Poll(out u);
                    queue.Offer(t);
                }
                else
                {
                    queue.Offer(t);
                    size = z + 1;
                }
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    actual.OnSubscribe(this);

                    s.Request(long.MaxValue);
                }
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (!BackpressureHelper.PostCompleteRequest(ref requested, n, actual, queue, ref cancelled))
                    {
                        s.Request(n);
                    }
                }
            }
        }
    }
}
