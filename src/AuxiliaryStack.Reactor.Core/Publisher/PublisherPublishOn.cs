﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherPublishOn<T> : IFlux<T>
    {
        private readonly IPublisher<T> _source;
        private readonly IScheduler _scheduler;
        private readonly bool _delayError;
        private readonly int _prefetch;

        internal PublisherPublishOn(IPublisher<T> source, IScheduler scheduler, bool delayError, int prefetch)
        {
            _source = source;
            _scheduler = scheduler;
            _delayError = delayError;
            _prefetch = prefetch;
        }

        public void Subscribe(ISubscriber<T> s)
        {

            if (PublisherSubscribeOn<T>.TrySingleSchedule(_source, s, _scheduler))
            {
                return;
            }

            IWorker worker = _scheduler.CreateWorker();

            if (s is IConditionalSubscriber<T>)
            {
                _source.Subscribe(new PublishOnConditionalSubscriber((IConditionalSubscriber<T>)s, worker, _delayError, _prefetch));
            }
            else
            {
                _source.Subscribe(new PublishOnSubscriber(s, worker, _delayError, _prefetch));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        abstract class BasePublishOnSubscriber : ISubscriber<T>, IQueueSubscription<T>
        {
            protected readonly IWorker worker;

            protected readonly bool delayError;

            protected readonly int prefetch;

            protected readonly int limit;

            protected int sourceMode;

            protected int outputMode;

            protected ISubscription s;

            protected IQueue<T> queue;

            protected bool done;

            protected Exception error;

            protected bool cancelled;

            // Hot fields

            Pad128 p1;

            protected int wip;

            Pad120 p2;

            protected long requested;

            Pad120 p3;

            /// <summary>
            /// Number of items successfully emitted to downstream -
            /// paired with the requested amount.
            /// </summary>
            protected long emitted;
            /// <summary>
            /// Number of items requested from upstream - reset to zero
            /// when the <see cref="limit"/> is reached. 
            /// </summary>
            protected long polled;

            Pad112 p4;

            internal BasePublishOnSubscriber(IWorker worker, bool delayError, int prefetch)
            {
                this.worker = worker;
                this.delayError = delayError;
                this.prefetch = prefetch;
                this.limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    var qs = s as IQueueSubscription<T>;
                    if (qs != null)
                    {
                        int mode = qs.RequestFusion(FuseableHelper.ANY | FuseableHelper.BOUNDARY);

                        if (mode == FuseableHelper.SYNC)
                        {
                            this.sourceMode = mode;
                            this.queue = qs;
                            Volatile.Write(ref done, true);

                            SubscribeActual();

                            Schedule();
                            return;
                        }
                        else
                        if (mode == FuseableHelper.ASYNC)
                        {
                            this.sourceMode = mode;
                            this.queue = qs;

                            SubscribeActual();

                            s.Request(prefetch < 0 ? long.MaxValue : prefetch);

                            return;
                        }
                    }

                    queue = QueueDrainHelper.CreateQueue<T>(prefetch);

                    SubscribeActual();

                    s.Request(prefetch < 0 ? long.MaxValue : prefetch);
                }
            }

            protected abstract void SubscribeActual();

            public void OnNext(T t)
            {
                if (sourceMode == FuseableHelper.NONE)
                {
                    if (!queue.Offer(t))
                    {
                        s.Cancel();
                        OnError(BackpressureHelper.MissingBackpressureException("Queue is full?!"));
                        return;
                    }
                }
                Schedule();
            }

            public void OnError(Exception e)
            {
                if (ExceptionHelper.AddError(ref error, e))
                {
                    Volatile.Write(ref done, true);
                    Schedule();
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(e);
                }
            }

            public void OnComplete()
            {
                Volatile.Write(ref done, true);
                Schedule();
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Schedule();
                }
            }

            public void Cancel()
            {
                if (Volatile.Read(ref cancelled))
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                worker.Dispose();
                s.Cancel();
                if (QueueDrainHelper.Enter(ref wip))
                {
                    queue.Clear();
                }
            }

            void Schedule()
            {
                if (QueueDrainHelper.Enter(ref wip))
                {
                    worker.Schedule(Drain);
                }
            }

            void Drain()
            {
                if (outputMode == FuseableHelper.ASYNC)
                {
                    DrainOutput();
                }
                else
                if (sourceMode == FuseableHelper.SYNC)
                {
                    DrainSync();
                }
                else
                if (delayError)
                {
                    DrainAsyncDelay();
                }
                else
                {
                    DrainAsyncNoDelay();
                }
            }

            public int RequestFusion(int mode)
            {
                int m = mode & FuseableHelper.ASYNC;
                outputMode = m;
                return m;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public bool Poll(out T value)
            {
                if (queue.Poll(out value))
                {
                    if (sourceMode != FuseableHelper.SYNC)
                    {
                        long p = polled + 1;
                        if (p == limit)
                        {
                            polled = 0;
                            s.Request(p);
                        }
                        else
                        {
                            polled = p;
                        }
                    }
                    return true;
                }
                return false;
            }

            public bool IsEmpty()
            {
                return queue.IsEmpty();
            }

            public void Clear()
            {
                queue.Clear();
            }

            protected abstract void DrainSync();

            protected abstract void DrainOutput();

            protected abstract void DrainAsyncDelay();

            protected abstract void DrainAsyncNoDelay();

        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class PublishOnSubscriber : BasePublishOnSubscriber
        {
            // Cold fields
            readonly ISubscriber<T> actual;

            internal PublishOnSubscriber(ISubscriber<T> actual, 
                IWorker worker, bool delayError, int prefetch)
                : base(worker, delayError, prefetch)
            {
                this.actual = actual;
            }

            protected override void SubscribeActual()
            {
                actual.OnSubscribe(this);
            }

            protected override void DrainSync()
            {
                var a = actual;
                var q = queue;

                int missed = 1;
                long e = emitted;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        T v;
                        bool empty;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            q.Clear();
                            s.Cancel();

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }

                        a.OnNext(v);

                        e++;
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool empty;

                        try
                        {
                            empty = q.IsEmpty();
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            q.Clear();
                            s.Cancel();

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }
                    }

                    emitted = e;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            protected override void DrainAsyncDelay()
            {
                var a = actual;
                var q = queue;

                long e = emitted;
                long p = polled;

                int lim = limit;

                int missed = 1;

                for (;;)
                {

                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        bool empty;

                        T v;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            s.Cancel();
                            q.Clear();

                            ExceptionHelper.AddError(ref error, ex);
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                       

                        if (d && empty)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                a.OnError(ex);
                            }
                            else
                            {
                                a.OnComplete();
                            }

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        a.OnNext(v);

                        e++;

                        if (++p == lim)
                        {
                            p = 0L;
                            s.Request(lim);
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        bool empty = q.IsEmpty();

                        if (d && empty)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                a.OnError(ex);
                            }
                            else
                            {
                                a.OnComplete();
                            }

                            worker.Dispose();
                            return;
                        }
                    }

                    emitted = e;
                    polled = p;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            protected override void DrainAsyncNoDelay()
            {
                var a = actual;
                var q = queue;

                long e = emitted;
                long p = polled;
                int lim = limit;

                int missed = 1;

                for (;;)
                {

                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        if (d)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                q.Clear();
                                a.OnError(ex);

                                worker.Dispose();
                                return;
                            }
                        }

                        T v;

                        bool empty;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            s.Cancel();
                            q.Clear();

                            ExceptionHelper.AddError(ref error, ex);
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                        if (d && empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        a.OnNext(v);

                        e++;

                        if (++p == lim)
                        {
                            p = 0L;
                            s.Request(lim);
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        if (d)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                q.Clear();
                                a.OnError(ex);

                                worker.Dispose();
                                return;
                            }
                            if (q.IsEmpty())
                            {
                                a.OnComplete();

                                worker.Dispose();
                                return;
                            }
                        }
                    }

                    emitted = e;
                    polled = p;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }

            }

            protected override void DrainOutput()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        return;
                    }

                    if (!delayError)
                    {
                        Exception ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);
                            return;
                        }
                    }

                    bool d = Volatile.Read(ref done);

                    a.OnNext(default(T));

                    if (d)
                    {
                        Exception ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            a.OnError(ex);
                        }
                        else
                        {
                            a.OnComplete();
                        }

                        worker.Dispose();
                        return;
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class PublishOnConditionalSubscriber : BasePublishOnSubscriber
        {
            // Cold fields
            readonly IConditionalSubscriber<T> actual;

            internal PublishOnConditionalSubscriber(
                IConditionalSubscriber<T> actual, IWorker worker, bool delayError, int prefetch)
                : base(worker, delayError, prefetch)
            {
                this.actual = actual;
            }

            protected override void SubscribeActual()
            {
                actual.OnSubscribe(this);
            }

            protected override void DrainSync()
            {
                var a = actual;
                var q = queue;

                int missed = 1;
                long e = emitted;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        T v;
                        bool empty;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            q.Clear();
                            s.Cancel();

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }

                        if (a.TryOnNext(v))
                        {
                            e++;
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool empty = q.IsEmpty();

                        if (empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }
                    }

                    emitted = e;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            protected override void DrainAsyncDelay()
            {
                var a = actual;
                var q = queue;

                long e = emitted;
                long p = polled;

                int lim = limit;

                int missed = 1;

                for (;;)
                {

                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        bool empty;

                        T v;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            s.Cancel();
                            q.Clear();

                            ExceptionHelper.AddError(ref error, ex);
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }



                        if (d && empty)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                a.OnError(ex);
                            }
                            else
                            {
                                a.OnComplete();
                            }

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        if (a.TryOnNext(v))
                        {
                            e++;
                        }

                        if (++p == lim)
                        {
                            p = 0;
                            s.Request(lim);
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        bool empty = q.IsEmpty();

                        if (d && empty)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                a.OnError(ex);
                            }
                            else
                            {
                                a.OnComplete();
                            }

                            worker.Dispose();
                            return;
                        }
                    }

                    emitted = e;
                    polled = p;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            protected override void DrainAsyncNoDelay()
            {
                var a = actual;
                var q = queue;

                long e = emitted;
                long p = polled;
                int lim = limit;

                int missed = 1;

                for (;;)
                {

                    long r = Volatile.Read(ref requested);

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        if (d)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                q.Clear();
                                a.OnError(ex);

                                worker.Dispose();
                                return;
                            }
                        }

                        T v;

                        bool empty;

                        try
                        {
                            empty = !q.Poll(out v);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);

                            s.Cancel();
                            q.Clear();

                            ExceptionHelper.AddError(ref error, ex);
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);

                            worker.Dispose();
                            return;
                        }

                        if (d && empty)
                        {
                            a.OnComplete();

                            worker.Dispose();
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        if (a.TryOnNext(v))
                        {
                            e++;
                        }

                        if (++p == lim)
                        {
                            p = 0L;
                            s.Request(lim);
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        if (d)
                        {
                            Exception ex = error;
                            if (ex != null)
                            {
                                q.Clear();
                                a.OnError(ex);

                                worker.Dispose();
                                return;
                            }
                            if (q.IsEmpty())
                            {
                                a.OnComplete();

                                worker.Dispose();
                                return;
                            }
                        }
                    }

                    emitted = e;
                    polled = p;
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }

            }

            protected override void DrainOutput()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    bool d = Volatile.Read(ref done);

                    bool empty;

                    try
                    {
                        empty = queue.IsEmpty();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);

                        s.Cancel();

                        ExceptionHelper.AddError(ref error, ex);
                        ex = ExceptionHelper.Terminate(ref error);

                        a.OnError(ex);

                        worker.Dispose();
                        return;
                    }

                    if (!empty)
                    {
                        a.TryOnNext(default(T));
                    }

                    if (d)
                    {
                        Exception ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            a.OnError(ex);
                        }
                        else
                        {
                            a.OnComplete();
                        }

                        worker.Dispose();
                        return;
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
