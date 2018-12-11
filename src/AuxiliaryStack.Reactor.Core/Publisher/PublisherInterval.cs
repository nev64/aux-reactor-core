using System;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherInterval : IFlux<long>
    {
        readonly TimeSpan initialDelay;

        readonly TimeSpan period;

        readonly ITimedScheduler scheduler;

        internal PublisherInterval(TimeSpan initialDelay, TimeSpan period, ITimedScheduler scheduler)
        {
            this.initialDelay = initialDelay;
            this.period = period;
            this.scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<long> s)
        {
            var parent = new IntervalSubscription(s);

            s.OnSubscribe(parent);

            parent.SetFuture(scheduler.Schedule(parent.Run, initialDelay, period));
        }
    }

    sealed class IntervalSubscription : ISubscription
    {

        readonly ISubscriber<long> actual;

        long requested;

        IDisposable d;

        long counter;

        internal IntervalSubscription(ISubscriber<long> actual)
        {
            this.actual = actual;
        }

        public void Cancel()
        {
            DisposableHelper.Dispose(ref d);
        }

        public void Request(long n)
        {
            BackpressureHelper.ValidateAndAddCap(ref requested, n);
        }

        internal void Run()
        {
            if (Volatile.Read(ref requested) != 0L)
            {
                actual.OnNext(counter++);

                BackpressureHelper.Produced(ref requested, 1);
            }
            else
            {
                actual.OnError(BackpressureHelper.MissingBackpressureException());
            }
        }

        internal void SetFuture(IDisposable d)
        {
            DisposableHelper.Set(ref this.d, d);
        }
    }
}
