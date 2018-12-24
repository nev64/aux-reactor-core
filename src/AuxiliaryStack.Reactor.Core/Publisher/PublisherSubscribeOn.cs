using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSubscribeOn<T> : IFlux<T>, IMono<T>
    {
        private readonly IPublisher<T> _source;
        private readonly IScheduler _scheduler;

        internal PublisherSubscribeOn(IPublisher<T> source, IScheduler scheduler)
        {
            _source = source;
            _scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<T> subscriber)
        {
            if (TrySingleSchedule(_source, subscriber, _scheduler))
            {
                return;
            }

            var worker = _scheduler.CreateWorker();

            if (subscriber is IConditionalSubscriber<T> conditionalSubscriber)
            {
                var parent = new SubscribeOnConditionalSubscription(conditionalSubscriber, worker);

                subscriber.OnSubscribe(parent);

                worker.Schedule(() => _source.Subscribe(parent));
            }
            else
            {
                var parent = new SubscribeOnSubscription(subscriber, worker);

                subscriber.OnSubscribe(parent);

                worker.Schedule(() => _source.Subscribe(parent));
            }
        }

        /// <summary>
        /// Checks if the source is ICallable and if so, subscribes with a custom
        /// Subscription that schedules the first request on the scheduler directly
        /// and reads the Callable.Value on that thread.
        /// </summary>
        /// <param name="source">The source IPublisher to check</param>
        /// <param name="subscriber">The target subscriber.</param>
        /// <param name="scheduler">The Scheduler to use.</param>
        /// <returns></returns>
        public static bool TrySingleSchedule(IPublisher<T> source, ISubscriber<T> subscriber, IScheduler scheduler)
        {
            if (source is ICallable<T> callable)
            {
                subscriber.OnSubscribe(new CallableSubscribeOn(subscriber, callable, scheduler));
                return true;
            }
            return false;
        }

        sealed class SubscribeOnSubscription : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;

            readonly IWorker worker;

            ISubscription s;

            long requested;

            internal SubscribeOnSubscription(ISubscriber<T> actual, IWorker worker)
            {
                this.actual = actual;
                this.worker = worker;
            }

            public void Cancel()
            {
                worker.Dispose();
                SubscriptionHelper.Cancel(ref s);
            }

            public void OnComplete()
            {
                try
                {
                    actual.OnComplete();
                }
                finally
                {
                    worker.Dispose();
                }
            }

            public void OnError(Exception e)
            {
                try
                {
                    actual.OnError(e);
                }
                finally
                {
                    worker.Dispose();
                }

            }
    
            public void OnNext(T t)
            {
                actual.OnNext(t);
            }

            public void OnSubscribe(ISubscription s)
            {
                BackpressureHelper.DeferredSetOnce(ref this.s, ref requested, s);
            }

            public void Request(long n)
            {
                worker.Schedule(() =>
                {
                    BackpressureHelper.DeferredRequest(ref s, ref requested, n);
                });
            }
        }

        sealed class SubscribeOnConditionalSubscription : IConditionalSubscriber<T>, ISubscription
        {
            readonly IConditionalSubscriber<T> actual;

            readonly IWorker worker;

            ISubscription s;

            long requested;

            internal SubscribeOnConditionalSubscription(IConditionalSubscriber<T> actual, IWorker worker)
            {
                this.actual = actual;
                this.worker = worker;
            }

            public void Cancel()
            {
                worker.Dispose();
                SubscriptionHelper.Cancel(ref s);
            }

            public void OnComplete()
            {
                try
                {
                    actual.OnComplete();
                }
                finally
                {
                    worker.Dispose();
                }
            }

            public void OnError(Exception e)
            {
                try
                {
                    actual.OnError(e);
                }
                finally
                {
                    worker.Dispose();
                }

            }

            public void OnNext(T t)
            {
                actual.OnNext(t);
            }

            public void OnSubscribe(ISubscription s)
            {
                BackpressureHelper.DeferredSetOnce(ref this.s, ref requested, s);
            }

            public void Request(long n)
            {
                worker.Schedule(() =>
                {
                    BackpressureHelper.DeferredRequest(ref s, ref requested, n);
                });
            }

            public bool TryOnNext(T t)
            {
                return actual.TryOnNext(t);
            }
        }

        sealed class CallableSubscribeOn : IFlowSubscription<T>
        {
            readonly ISubscriber<T> actual;

            readonly ICallable<T> callable;

            readonly IScheduler scheduler;

            IDisposable cancel;

            int once;

            FusionMode fusionMode;

            bool hasValue;

            internal CallableSubscribeOn(ISubscriber<T> actual, ICallable<T> callable, IScheduler scheduler)
            {
                this.actual = actual;
                this.callable = callable;
                this.scheduler = scheduler;
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                    {
                        var d = scheduler.Schedule(Run);
                        DisposableHelper.Replace(ref cancel, d);
                    }
                }
            }

            void Run()
            {
                if (fusionMode != FusionMode.None)
                {
                    hasValue = true;
                    actual.OnNext(default(T));
                    actual.OnComplete();
                    return;
                }

                T t;

                try
                {
                    t = callable.Value;
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    if (!DisposableHelper.IsDisposed(ref cancel))
                    {
                        actual.OnError(ex);

                        cancel = null;
                    }
                    return;
                }

                if (!DisposableHelper.IsDisposed(ref cancel))
                {
                    actual.OnNext(t);

                    if (!DisposableHelper.IsDisposed(ref cancel))
                    {
                        actual.OnComplete();

                        cancel = null;
                    }
                }
            }

            public void Cancel()
            {
                DisposableHelper.Dispose(ref cancel);
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                if ((mode & FusionMode.Boundary) != 0)
                {
                    return FusionMode.None;
                }
                var m = mode & FusionMode.Async;
                fusionMode = m;
                return m;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<T> Poll()
            {
                if (hasValue)
                {
                    hasValue = false;
                    return Just(callable.Value);
                }

                return None<T>();
            }

            public bool IsEmpty()
            {
                return hasValue;
            }

            public void Clear()
            {
                hasValue = false;
            }
        }
    }
}
