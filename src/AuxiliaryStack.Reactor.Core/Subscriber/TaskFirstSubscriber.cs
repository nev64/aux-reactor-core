using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    sealed class TaskFirstSubscriber<T> : ISubscriber<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        ISubscription s;

        bool hasValue;

        public void OnComplete()
        {
            if (!hasValue)
            {
                tcs.TrySetException(new IndexOutOfRangeException("The upstream didn't produce any value."));
            }
        }

        public void OnError(Exception e)
        {
            if (hasValue)
            {
                ExceptionHelper.OnErrorDropped(e);
                return;
            }
            tcs.TrySetException(e);
        }

        public void OnNext(T t)
        {
            if (hasValue)
            {
                return;
            }
            hasValue = true;
            s.Cancel();
            tcs.TrySetResult(t);
        }

        public void OnSubscribe(ISubscription s)
        {
            if (SubscriptionHelper.SetOnce(ref this.s, s))
            {
                s.Request(long.MaxValue);
            }
        }

        public void Dispose()
        {
            SubscriptionHelper.Cancel(ref s);
        }


        internal Task<T> Task()
        {
            return tcs.Task;
        }

        internal Task<T> Task(CancellationToken token)
        {
            token.Register(this.Dispose);
            return tcs.Task;
        }

    }
}
