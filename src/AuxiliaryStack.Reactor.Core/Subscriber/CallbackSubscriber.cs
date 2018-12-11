using System;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    internal sealed class CallbackSubscriber<T> : ISubscriber<T>, IDisposable
    {
        readonly Action<T> onNext;

        readonly Action<Exception> onError;

        readonly Action onComplete;

        ISubscription s;

        bool done;

        internal CallbackSubscriber(Action<T> onNext, Action<Exception> onError, Action onComplete)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onComplete = onComplete;
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
            try
            {
                onNext(t);
            } catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                s.Cancel();
                OnError(ex);
                return;
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
                onError(e);
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
                onComplete();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                ExceptionHelper.OnErrorDropped(ex);
            }
        }

        public void Dispose()
        {
            SubscriptionHelper.Cancel(ref s);
        }
    }
}
