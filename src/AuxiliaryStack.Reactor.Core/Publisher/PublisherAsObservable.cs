using System;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    /// <summary>
    /// Wraps an IPublisher and exposes it as an IObservable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    sealed class PublisherAsObservable<T> : IObservable<T>
    {
        readonly IPublisher<T> source;

        internal PublisherAsObservable(IPublisher<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var s = new AsObserver(observer);

            source.Subscribe(s);

            return s;
        }

        sealed class AsObserver : ISubscriber<T>, IDisposable
        {
            readonly IObserver<T> observer;

            ISubscription s;

            bool done;

            internal AsObserver(IObserver<T> observer)
            {
                this.observer = observer;
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.SetOnce(ref this.s, s))
                {
                    s.Request(long.MaxValue);
                }
            }

            public void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                try
                {
                    observer.OnNext(t);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    s.Cancel();
                    OnError(ex);
                }
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                done = true;

                try
                {
                    observer.OnError(e);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    ExceptionHelper.OnErrorDropped(new AggregateException(e, ex));
                }
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;

                try
                {
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowOrDrop(ex);
                }
            }

            public void Dispose()
            {
                SubscriptionHelper.Cancel(ref s);
            }
        }
    }
}
