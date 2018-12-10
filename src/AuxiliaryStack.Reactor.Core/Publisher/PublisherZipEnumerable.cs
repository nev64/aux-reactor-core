using System;
using System.Collections.Generic;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherZipEnumerable<T, U, R> : IFlux<R>
    {
        readonly IPublisher<T> source;

        readonly IEnumerable<U> other;

        readonly Func<T, U, R> zipper;

        internal PublisherZipEnumerable(IPublisher<T> source, IEnumerable<U> other, Func<T, U, R> zipper)
        {
            this.source = source;
            this.other = other;
            this.zipper = zipper;
        }

        public void Subscribe(ISubscriber<R> s)
        {
            IEnumerator<U> enumerator;

            bool hasValue;

            try
            {
                enumerator = other.GetEnumerator();

                hasValue = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<R>.Error(s, ex);
                return;
            }

            if (!hasValue)
            {
                EmptySubscription<R>.Complete(s);
                return;
            }

            var parent = new ZipEnumerableSubscriber(s, enumerator, zipper);
            source.Subscribe(parent);
        }

        sealed class ZipEnumerableSubscriber : BasicSubscriber<T, R>
        {
            readonly IEnumerator<U> enumerator;

            readonly Func<T, U, R> zipper;

            bool once;

            public ZipEnumerableSubscriber(ISubscriber<R> actual, IEnumerator<U> enumerator, Func<T, U, R> zipper) : base(actual)
            {
                this.enumerator = enumerator;
                this.zipper = zipper;
            }

            public override void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                enumerator.Dispose();
                actual.OnComplete();
            }

            public override void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                done = true;
                enumerator.Dispose();
                actual.OnComplete();
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                if (once)
                {
                    bool b;

                    try
                    {
                        b = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        Fail(ex);
                        return;
                    }

                    if (!b)
                    {
                        s.Cancel();
                        Complete();
                        return;
                    }
                } else
                {
                    once = true;
                }

                R r;

                try
                {
                    r = zipper(t, enumerator.Current);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    enumerator.Dispose();
                    Fail(ex);
                    return;
                }

                actual.OnNext(r);
            }
        }
    }
}
