using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFromTask<T> : IFlux<T>, IMono<T>
    {
        readonly Task<T> task;

        internal PublisherFromTask(Task<T> task)
        {
            this.task = task;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            var ts = new TaskSubscription(s);
            task.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    ts.Complete(t.Result);
                }
                else
                if (t.IsFaulted)
                {
                    ts.Error(t.Exception);
                }
            }, ts.ct.Token);
        }

        sealed class TaskSubscription : DeferredScalarSubscription<T>
        {
            internal CancellationTokenSource ct;

            public TaskSubscription(ISubscriber<T> actual) : base(actual)
            {
                ct = new CancellationTokenSource();
            }

            public override void Cancel()
            {
                base.Cancel();
                try
                {
                    ct.Cancel();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.OnErrorDropped(ex);
                }
            }
        }
    }

    sealed class PublisherFromTask : IFlux<Void>, IMono<Void>
    {
        readonly Task task;

        internal PublisherFromTask(Task task)
        {
            this.task = task;
        }

        public void Subscribe(ISubscriber<Void> s)
        {
            var ts = new TaskSubscription(s);
            task.ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    ts.Complete();
                }
                else
                if (t.IsFaulted)
                {
                    ts.Error(t.Exception);
                }
            }, ts.ct.Token);
        }

        sealed class TaskSubscription : DeferredScalarSubscription<Void>
        {
            internal CancellationTokenSource ct;

            public TaskSubscription(ISubscriber<Void> actual) : base(actual)
            {
                ct = new CancellationTokenSource();
            }

            public override void Cancel()
            {
                base.Cancel();
                try
                {
                    ct.Cancel();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.OnErrorDropped(ex);
                }
            }
        }
    }
}
