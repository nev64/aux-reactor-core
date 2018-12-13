using System;
using System.Threading;
using System.Threading.Tasks;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Subscription;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    internal sealed class FromTask : FromTask<Unit> 
    {
        private readonly Task _actionTask;

        internal FromTask(Task task) : base(task.ContinueWith(_ => Unit.Instance))
        {
            _actionTask = task;
        }

        protected override void Start()
        {
            if (_actionTask.Status == TaskStatus.Created)
            {
                _actionTask.Start();
            }
        }
    }

    internal class FromTask<T> : IFlux<T>, IMono<T>
    {
        private readonly Task<T> _task;

        internal FromTask(Task<T> task)
        {
            _task = task;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            var subscription = new TaskSubscription(subscriber, _task);
            subscriber.OnSubscribe(subscription);
            Start();
        }
        
        protected virtual void Start()
        {
            if (_task.Status == TaskStatus.Created)
            {
                _task.Start();
            }
        }

        private sealed class TaskSubscription : DeferredScalarSubscription<T>
        {
            private readonly CancellationTokenSource _cancellation;

            public TaskSubscription(ISubscriber<T> actual, Task<T> task)
                : base(actual)
            {
                _cancellation = new CancellationTokenSource();
                task.ContinueWith(prev =>
                {
                    if (prev.IsFaulted)
                    {
                        _actual.OnError(prev.Exception);
                    }
                    else
                    {
                        _actual.OnNext(prev.Result);
                    }

                    _actual.OnComplete();
                }, _cancellation.Token);
            }

            public override void Cancel()
            {
                base.Cancel();
                try
                {
                    _cancellation.Cancel();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.OnErrorDropped(ex);
                }
            }
        }
    }
}