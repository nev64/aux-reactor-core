using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    sealed class TaskLastSubscriber<T> : ISubscriber<T>, IDisposable
    {
        readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        ISubscription s;

        bool hasValue;

        T value;

        public void OnComplete()
        {
            if (!hasValue)
            {
                tcs.TrySetException(new IndexOutOfRangeException("The upstream didn't produce any value."));
            }
            else
            {
                tcs.TrySetResult(value);
            }
        }

        public void OnError(Exception e)
        {
            tcs.TrySetException(e);
        }

        public void OnNext(T t)
        {
            hasValue = true;
            value = t;
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
