﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFlatMap<T, R> : IFlux<R>
    {
        readonly IPublisher<T> source;

        readonly Func<T, IPublisher<R>> mapper;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        readonly int prefetch;

        internal PublisherFlatMap(IPublisher<T> source, Func<T, IPublisher<R>> mapper,
            bool delayErrors, int maxConcurrency, int prefetch)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
            this.prefetch = prefetch;
        }

        public void Subscribe(ISubscriber<R> s)
        {
            if (PublisherCallableXMap<T, R>.CallableXMap(source, s, mapper))
            {
                return;
            }
            source.Subscribe(new FlatMapSubscriber(s, mapper, delayErrors, maxConcurrency, prefetch));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal sealed class FlatMapSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<R> actual;

            readonly Func<T, IPublisher<R>> mapper;

            readonly bool delayErrors;

            readonly int maxConcurrency;

            readonly int prefetch;

            readonly int limit;

            bool done;

            Exception error;

            bool cancelled;

            ISubscription s;

            SpscFreelistTracker<FlatMapInnerSubscriber> tracker;

            IFlow<R> _scalarFlow;

            int scalarConsumed;

            Pad128 p0;

            int wip;

            Pad120 p1;

            long requested;

            Pad120 p2;

            internal FlatMapSubscriber(
                ISubscriber<R> actual, Func<T, IPublisher<R>> mapper,
                bool delayErrors, int maxConcurrency, int prefetch
            )
            {
                this.actual = actual;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.maxConcurrency = maxConcurrency;
                this.prefetch = prefetch;
                this.limit = prefetch - (prefetch >> 2);
                tracker.Init();
            }

            bool lvCancelled()
            {
                return Volatile.Read(ref cancelled);
            }

            bool lvDone()
            {
                return Volatile.Read(ref done);
            }

            Exception lvError()
            {
                return Volatile.Read(ref error);
            }

            public void Cancel()
            {
                if (lvCancelled())
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                s.Cancel();
                CancelAll();

                if (QueueDrainHelper.Enter(ref wip))
                {
                    var sq = Volatile.Read(ref _scalarFlow);
                    sq?.Clear();
                }
            }

            void CancelAll()
            {
                var a = tracker.Cancel();
                int n = a.Length;

                for (int i = 0; i < n; i++)
                {
                    var inner = Volatile.Read(ref a[i]);
                    inner?.Cancel();
                }
            }

            public void OnComplete()
            {
                if (lvDone())
                {
                    return;
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception e)
            {
                if (lvDone())
                {
                    ExceptionHelper.OnErrorDropped(e);
                }
                else
                if (ExceptionHelper.AddError(ref error, e))
                {
                    if (!delayErrors)
                    {
                        CancelAll();
                    }
                    Volatile.Write(ref done, true);
                    Drain();
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(e);
                }
            }

            void ScalarConsumed()
            {
                if (maxConcurrency != int.MaxValue)
                {
                    int p = scalarConsumed + 1;
                    if (p == limit)
                    {
                        scalarConsumed = 0;
                        s.Request(p);
                    } else
                    {
                        scalarConsumed = p;
                    }
                }
            }

            public void OnNext(T t)
            {
                if (lvDone())
                {
                    return;
                }

                IPublisher<R> p;

                try
                {
                    p = mapper(t);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    s.Cancel();
                    OnError(ex);
                    return;
                }

                if (p == null)
                {
                    s.Cancel();
                    OnError(new NullReferenceException("The mapper produced a null IPublisher"));
                }
                else
                if (p == PublisherEmpty<R>.Instance)
                {
                    ScalarConsumed();
                }
                else
                if (p is ICallable<R>)
                {
                    R scalar;

                    try
                    {
                        scalar = (p as ICallable<R>).Value;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        s.Cancel();
                        OnError(ex);
                        return;
                    }
                    TryEmitScalar(scalar);
                }
                else
                {
                    var inner = new FlatMapInnerSubscriber(this, prefetch);
                    if (tracker.Add(inner))
                    {
                        p.Subscribe(inner);
                    }
                }
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {

                    actual.OnSubscribe(this);

                    if (maxConcurrency == int.MaxValue)
                    {
                        s.Request(long.MaxValue);
                    }
                    else
                    {
                        s.Request(maxConcurrency);
                    }
                }
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Drain();
                }
            }

            IFlow<R> GetOrCreateScalarQueue()
            {
                var q = Volatile.Read(ref _scalarFlow);
                if (q == null)
                {
                    if (maxConcurrency == int.MaxValue)
                    {
                        q = new SpscLinkedArrayFlow<R>(prefetch);
                    }
                    else
                    {
                        q = new SpscArrayFlow<R>(maxConcurrency);
                    }
                    Volatile.Write(ref _scalarFlow, q);
                }
                return q;
            }

            void TryEmitScalar(R scalar)
            {
                if (QueueDrainHelper.TryEnter(ref wip))
                {
                    long r = Volatile.Read(ref requested);
                    if (r != 0)
                    {
                        actual.OnNext(scalar);
                        if (r != long.MaxValue)
                        {
                            Interlocked.Decrement(ref requested);
                        }

                        ScalarConsumed();
                    } else
                    {
                        var q = GetOrCreateScalarQueue();

                        q.Offer(scalar);
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var q = GetOrCreateScalarQueue();

                    q.Offer(scalar);

                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            internal void Drain()
            {
                if (QueueDrainHelper.Enter(ref wip))
                {
                    DrainLoop();
                }
            }

            void DrainLoop()
            {
                int missed = 1;

                var a = actual;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long m = 0;
                    long e = 0;
                    bool d;

                    var sq = Volatile.Read(ref _scalarFlow);

                    if (sq != null)
                    {
                        while (e != r)
                        {
                            if (CheckCancelOrError(a))
                            {
                                return;
                            }

                            R v;

                            var elem = sq.Poll();
                            if (elem.IsJust)
                            {
                                a.OnNext(elem.GetValue());

                                e++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    var inners = tracker.Values();

                    int n = inners.Length;

                    for (int i = 0; i < n; i++)
                    {
                        if (CheckCancelOrError(a))
                        {
                            return;
                        }

                        var inner = Volatile.Read(ref inners[i]);
                        if (inner != null)
                        {
                            d = inner.lvDone();
                            IFlow<R> q = inner.GetQueue();

                            if (q == null || q.IsEmpty())
                            {
                                if (d)
                                {
                                    m++;
                                    tracker.Remove(i);
                                }
                            } else
                            {
                                while (e != r)
                                {
                                    if (CheckCancelOrError(a))
                                    {
                                        return;
                                    }

                                    d = inner.lvDone();

                                    R v;

                                    bool hasValue;
                                    Option<R> elem;

                                    try
                                    {
                                        elem = q.Poll();
                                        hasValue = elem.IsJust;
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHelper.ThrowIfFatal(ex);
                                        inner.Cancel();
                                        ExceptionHelper.AddError(ref error, ex);

                                        if (CheckCancelOrError(a))
                                        {
                                            return;
                                        }
                                        d = true;
                                        hasValue = false;
                                        elem = None<R>();
                                    }
                                    if (hasValue)
                                    {
                                        v = elem.GetValue();
                                        a.OnNext(v);

                                        e++;
                                        inner.RequestOne();
                                    }
                                    else
                                    {
                                        if (d)
                                        {
                                            m++;
                                            tracker.Remove(i);
                                        }
                                        break;
                                    }
                                }

                                if (e == r)
                                {
                                    if (CheckCancelOrError(a))
                                    {
                                        return;
                                    }

                                    if (inner.lvDone() && q.IsEmpty())
                                    {
                                        m++;
                                        tracker.Remove(i);
                                    }
                                }
                            }
                        }
                    }

                    if (CheckCancelOrError(a))
                    {
                        return;
                    }

                    d = lvDone();

                    n = tracker.Size();

                    sq = Volatile.Read(ref _scalarFlow);

                    if (d && n == 0 && (sq == null || sq.IsEmpty()))
                    {
                        Exception ex = lvError();
                        if (ex != null)
                        {
                            ex = ExceptionHelper.Terminate(ref error);

                            a.OnError(ex);
                        }
                        else
                        {
                            a.OnComplete();
                        }
                        return;
                    }

                    if (e != 0 && r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -e);
                    }

                    if (m != 0 && !lvDone() && !lvCancelled())
                    {
                        s.Request(m);
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            bool CheckCancelOrError(ISubscriber<R> a)
            {
                if (lvCancelled())
                {
                    var sq = Volatile.Read(ref _scalarFlow);
                    sq?.Clear();
                    return true;
                }

                if (!delayErrors)
                {
                    Exception ex = lvError();
                    if (ex != null)
                    {
                        ex = ExceptionHelper.Terminate(ref error);
                        var sq = Volatile.Read(ref _scalarFlow);
                        sq?.Clear();

                        a.OnError(ex);
                        return true;
                    }
                }

                return false;
            }

            internal void InnerNext(FlatMapInnerSubscriber sender, R value)
            {
                if (sender.IsAsyncFused())
                {
                    Drain();
                    return;
                }
                if (QueueDrainHelper.TryEnter(ref wip))
                {
                    long r = Volatile.Read(ref requested);
                    if (r != 0)
                    {
                        actual.OnNext(value);
                        if (r != long.MaxValue)
                        {
                            Interlocked.Decrement(ref requested);
                        }

                        sender.RequestOne();
                    }
                    else
                    {
                        var q = sender.GetOrCreateQueue();

                        q.Offer(value);
                    }

                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var q = sender.GetOrCreateQueue();

                    q.Offer(value);

                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            internal void InnerError(FlatMapInnerSubscriber sender, Exception ex)
            {
                if (ExceptionHelper.AddError(ref error, ex))
                {
                    if (!delayErrors)
                    {
                        CancelAll();
                    }
                    sender.svDone();
                    Drain();
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(ex);
                }
            }

            internal void InnerComplete(FlatMapInnerSubscriber sender)
            {
                Drain();
            }
        }

        internal sealed class FlatMapInnerSubscriber : ISubscriber<R>
        {
            readonly FlatMapSubscriber parent;

            readonly int prefetch;

            readonly int limit;

            ISubscription s;

            IFlow<R> _flow;

            bool done;

            long produced;

            int fusionMode;

            internal FlatMapInnerSubscriber(FlatMapSubscriber parent, int prefetch)
            {
                this.parent = parent;
                this.prefetch = prefetch;
                this.limit = prefetch - (prefetch >> 2);
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.SetOnce(ref this.s, s))
                {
                    var qs = s as IFlowSubscription<R>;
                    if (qs != null)
                    {
                        int m = qs.RequestFusion(FuseableHelper.ANY);
                        if (m == FuseableHelper.SYNC)
                        {
                            fusionMode = m;
                            _flow = qs;
                            Volatile.Write(ref done, true);

                            parent.Drain();
                            return;
                        }
                        else
                        if (m == FuseableHelper.ASYNC)
                        {
                            fusionMode = m;
                            _flow = qs;

                            s.Request(prefetch < 0 ? long.MaxValue : prefetch);

                            return;
                        }
                    }

                    _flow = QueueDrainHelper.CreateQueue<R>(prefetch);

                    s.Request(prefetch < 0 ? long.MaxValue : prefetch);
                }
            }

            public void OnNext(R t)
            {
                parent.InnerNext(this, t);
            }

            public void OnError(Exception e)
            {
                parent.InnerError(this, e);
            }

            public void OnComplete()
            {
                Volatile.Write(ref done, true);
                parent.InnerComplete(this);
            }

            internal void RequestOne()
            {
                if (fusionMode != FuseableHelper.SYNC)
                {
                    long p = produced + 1;
                    if (p == limit)
                    {
                        produced = 0;
                        s.Request(p);
                    }
                    else
                    {
                        produced = p;
                    }
                }
            }

            internal void Cancel()
            {
                SubscriptionHelper.Cancel(ref s);
            }

            internal void Clear()
            {
                _flow.Clear();
            }

            internal IFlow<R> GetOrCreateQueue()
            {
                var q = Volatile.Read(ref _flow);
                if (q == null)
                {
                    q = QueueDrainHelper.CreateQueue<R>(prefetch);
                    Volatile.Write(ref _flow, q);
                }
                return q;
            }

            internal IFlow<R> GetQueue()
            {
                return Volatile.Read(ref _flow);
            }
            
            internal bool lvDone()
            {
                return Volatile.Read(ref done);
            }

            internal void svDone()
            {
                Volatile.Write(ref done, true);
            }

            internal bool IsAsyncFused()
            {
                return fusionMode == FuseableHelper.ASYNC;
            }
        }
    }
}
