using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Subscriber
{
    sealed class TaskCompleteSubscriber<T> : ISubscriber<T>, IDisposable
    {
        readonly TaskCompletionSource<Void> tcs = new TaskCompletionSource<Void>();

        ISubscription s;

        public void OnComplete()
        {
            tcs.TrySetResult(null);
        }

        public void OnError(Exception e)
        {
            tcs.TrySetException(e);
        }

        public void OnNext(T t)
        {
            // values ignored
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


        internal Task Task()
        {
            return tcs.Task;
        }

        internal Task Task(CancellationToken token)
        {
            token.Register(this.Dispose);
            return tcs.Task;
        }

    }
}
