using System;
using System.Collections.Generic;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFlattenEnumerable<T, R> : IFlux<R>
    {
        readonly IPublisher<T> source;

        readonly Func<T, IEnumerable<R>> mapper;

        readonly int prefetch;

        internal PublisherFlattenEnumerable(IPublisher<T> source, Func<T, IEnumerable<R>> mapper,
            int prefetch)
        {
            this.source = source;
            this.mapper = mapper;
            this.prefetch = prefetch;
        }

        public void Subscribe(ISubscriber<R> s)
        {
            source.Subscribe(new FlattenEnumerableSubscriber(s, mapper, prefetch));
        }

        sealed class FlattenEnumerableSubscriber : BasicFuseableSubscriber<T, R>
        {
            readonly Func<T, IEnumerable<R>> mapper;

            readonly int prefetch;

            IFlow<T> _flow;

            int wip;

            long requested;

            int outputMode;

            Exception error;

            bool cancelled;

            IEnumerator<R> enumerator;

            bool hasValue;

            public FlattenEnumerableSubscriber(ISubscriber<R> actual, Func<T, IEnumerable<R>> mapper, int prefetch) : base(actual)
            {
                this.mapper = mapper;
                this.prefetch = prefetch;
            }

            public override void OnComplete()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public override void OnError(Exception e)
            {
                if (ExceptionHelper.AddError(ref error, e))
                {
                    Drain();
                }
                else
                {
                    ExceptionHelper.OnErrorDropped(e);
                }
            }

            public override void OnNext(T t)
            {
                if (fusionMode != FuseableHelper.ASYNC)
                {
                    if (!_flow.Offer(t))
                    {
                        OnError(BackpressureHelper.MissingBackpressureException());
                        return;
                    }
                }
                Drain();
            }

            public override Option<R> Poll()
            {
                var en = enumerator;
                for (;;)
                {
                    if (en == null)
                    {
                        var elem = _flow.Poll();
                        if (elem.IsJust) 
                        {
                            en = mapper(elem.GetValue())
                                .GetEnumerator();
                            enumerator = en;
                        }
                        else
                        {
                            return None<R>();
                        }
                    }

                    if (en.MoveNext())
                    {
                        return Just(en.Current);
                    }
                    en = null;
                }
            }

            public override bool IsEmpty()
            {
                return _flow.IsEmpty();
            }

            public override int RequestFusion(int mode)
            {
                if ((mode & FuseableHelper.SYNC) != 0 && fusionMode == FuseableHelper.SYNC)
                {
                    outputMode = FuseableHelper.SYNC;
                    return FuseableHelper.SYNC;
                }
                return FuseableHelper.NONE;
            }

            public override void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Drain();
                }
            }

            public override void Cancel()
            {
                Volatile.Write(ref cancelled, true);
                s.Cancel();
                if (QueueDrainHelper.Enter(ref wip))
                {
                    _flow.Clear();
                    enumerator?.Dispose();
                    enumerator = null;
                }
            }

            protected override bool BeforeSubscribe()
            {
                var qs = this.qs;
                if (qs != null)
                {
                    int m = qs.RequestFusion(FuseableHelper.ANY);
                    if (m == FuseableHelper.SYNC)
                    {
                        _flow = qs;
                        fusionMode = m;
                        Volatile.Write(ref done, true);

                        actual.OnSubscribe(this);

                        Drain();
                        return false;
                    }
                    else
                    if (m == FuseableHelper.ASYNC)
                    {
                        _flow = qs;
                        fusionMode = m;

                        actual.OnSubscribe(this);

                        s.Request(prefetch < 0 ? long.MaxValue : prefetch);

                        return false;
                    }
                }

                _flow = QueueDrainHelper.CreateQueue<T>(prefetch);
                return true;
            }

            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                int missed = 1;
                var a = actual;
                var q = _flow;
                var en = enumerator;

                for (;;)
                {
                    if (en == null)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            en?.Dispose();
                            enumerator = null;

                            return;
                        }

                        Exception ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            ex = ExceptionHelper.Terminate(ref error);

                            q.Clear();
                            en?.Dispose();
                            enumerator = null;

                            a.OnError(ex);
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T v;
                        var elem = _flow.Poll();
                        if (elem.IsJust)
                        {
                            en = mapper(elem.GetValue())
                                .GetEnumerator();
                            enumerator = en;
                        }
                        else
                        {
                            if (d)
                            {
                                en?.Dispose();
                                enumerator = null;

                                a.OnComplete();
                                return;
                            }
                        }
                    }

                    if (en != null)
                    {
                        long r = Volatile.Read(ref requested);
                        long e = 0L;

                        while (e != r)
                        {
                            if (Volatile.Read(ref cancelled))
                            {
                                q.Clear();
                                en?.Dispose();
                                enumerator = null;

                                return;
                            }

                            Exception ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                ex = ExceptionHelper.Terminate(ref error);

                                q.Clear();
                                en?.Dispose();
                                enumerator = null;

                                a.OnError(ex);
                                return;
                            }

                            bool b = hasValue;

                            if (!b)
                            {
                                try
                                {
                                    b = en.MoveNext();
                                }
                                catch (Exception exc)
                                {
                                    ExceptionHelper.ThrowIfFatal(exc);

                                    s.Cancel();

                                    q.Clear();

                                    ExceptionHelper.AddError(ref error, exc);
                                    exc = ExceptionHelper.Terminate(ref error);

                                    a.OnError(exc);

                                    return;
                                }
                                hasValue = b;
                            }

                            if (b)
                            {

                                a.OnNext(en.Current);

                                e++;
                                hasValue = false;
                            }
                            else
                            {
                                en?.Dispose();
                                en = null;
                                enumerator = en;
                                break;
                            }
                        }

                        if (e == r)
                        {
                            if (Volatile.Read(ref cancelled))
                            {
                                q.Clear();
                                en?.Dispose();
                                enumerator = null;

                                return;
                            }

                            Exception ex = Volatile.Read(ref error);
                            if (ex != null)
                            {
                                ex = ExceptionHelper.Terminate(ref error);

                                q.Clear();
                                en?.Dispose();
                                enumerator = null;

                                a.OnError(ex);
                                return;
                            }

                            bool b = hasValue;
                            if (!b)
                            {
                                try
                                {
                                    b = en.MoveNext();
                                }
                                catch (Exception exc)
                                {
                                    ExceptionHelper.ThrowIfFatal(exc);

                                    s.Cancel();

                                    q.Clear();

                                    ExceptionHelper.AddError(ref error, exc);
                                    exc = ExceptionHelper.Terminate(ref error);

                                    a.OnError(exc);

                                    return;
                                }
                                hasValue = b;
                            }

                            if (!b)
                            {
                                en?.Dispose();
                                en = null;
                                enumerator = en;
                            }
                        }

                        if (e != 0 && r != long.MaxValue)
                        {
                            Interlocked.Add(ref requested, -e);
                        }

                        if (en == null)
                        {
                            continue;
                        }
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
