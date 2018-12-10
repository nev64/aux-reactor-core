﻿using System;
using System.Collections.Generic;
using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherEnumerable<T> : IFlux<T>
    {
        readonly IEnumerable<T> enumerable;

        internal PublisherEnumerable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public void Subscribe(ISubscriber<T> s)
        {

            IEnumerator<T> enumerator;

            bool hasValue;

            try
            {
                enumerator = enumerable.GetEnumerator();

                hasValue = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<T>.Error(s, ex);
                return;
            }

            if (!hasValue)
            {
                EmptySubscription<T>.Complete(s);
                return;
            }

            if (s is IConditionalSubscriber<T>)
            {
                s.OnSubscribe(new EnumerableConditionalSubscription((IConditionalSubscriber<T>)s, enumerator));
            }
            else
            {
                s.OnSubscribe(new EnumerableSubscription(s, enumerator));
            }
        }

        abstract class EnumerableBaseSubscription : IQueueSubscription<T>
        {
            protected readonly IEnumerator<T> enumerator;

            protected long requested;

            protected bool cancelled;

            protected bool empty;

            internal EnumerableBaseSubscription(IEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public void Cancel()
            {
                Volatile.Write(ref cancelled, true);
                if (Interlocked.Increment(ref requested) == 1)
                {
                    Dispose(enumerator);
                }
            }

            protected void Dispose(IEnumerator<T> e)
            {
                try
                {
                    e.Dispose();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowOrDrop(ex);
                }

            }

            public void Clear()
            {
                Dispose(enumerator);
            }

            public bool IsEmpty()
            {
                return empty;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public bool Poll(out T value)
            {
                if (empty)
                {
                    value = default(T);
                    return false;
                }

                if (!enumerator.MoveNext())
                {
                    empty = true;
                    value = default(T);
                    Dispose(enumerator);
                    return false;
                }
                value = enumerator.Current;
                return true;
            }

            public void Request(long n)
            {
                if (BackpressureHelper.ValidateAndAddCap(ref requested, n) == 0L)
                {
                    if (n == long.MaxValue)
                    {
                        FastPath();
                    }
                    else
                    {
                        SlowPath(n);
                    }
                }
            }

            protected abstract void FastPath();

            protected abstract void SlowPath(long r);

            public int RequestFusion(int mode)
            {
                return mode & FuseableHelper.SYNC;
            }

        }

        sealed class EnumerableSubscription : EnumerableBaseSubscription
        {
            readonly ISubscriber<T> actual;

            internal EnumerableSubscription(ISubscriber<T> actual, IEnumerator<T> enumerator) : base(enumerator)
            {
                this.actual = actual;
            }

            protected override void FastPath()
            {
                var a = actual;
                var e = enumerator;
                for (;;)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        Dispose(e);
                        return;
                    }

                    a.OnNext(e.Current);

                    bool b;

                    try
                    {
                        b = e.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);

                        a.OnError(ex);

                        Dispose(e);
                        return;
                    }

                    if (!b)
                    {
                        if (!Volatile.Read(ref cancelled))
                        {
                            a.OnComplete();
                        }

                        Dispose(e);

                        return;
                    }
                }                
            }

            protected override void SlowPath(long r)
            {
                var et = enumerator;
                var e = 0L;
                var a = actual;

                for (;;)
                {

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Dispose(et);
                            return;
                        }

                        a.OnNext(et.Current);

                        bool b;

                        try
                        {
                            b = et.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);
                            a.OnError(ex);
                            return;
                        }

                        if (!b)
                        {
                            if (!Volatile.Read(ref cancelled))
                            {
                                a.OnComplete();
                            }

                            Dispose(et);

                            return;
                        }

                        e++;
                    }

                    r = Volatile.Read(ref requested);
                    if (r == e)
                    {
                        r = Interlocked.Add(ref requested, -e);
                        if (r == 0L)
                        {
                            break;
                        }
                        e = 0L;
                    }
                }
            }
        }

        sealed class EnumerableConditionalSubscription : EnumerableBaseSubscription
        {
            readonly IConditionalSubscriber<T> actual;

            internal EnumerableConditionalSubscription(IConditionalSubscriber<T> actual, IEnumerator<T> enumerator) : base(enumerator)
            {
                this.actual = actual;
            }

            protected override void FastPath()
            {
                var a = actual;
                var e = enumerator;
                for (;;)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        Dispose(e);
                        return;
                    }

                    a.TryOnNext(e.Current);

                    bool b;

                    try
                    {
                        b = e.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);

                        a.OnError(ex);

                        Dispose(e);
                        return;
                    }

                    if (!b)
                    {
                        if (!Volatile.Read(ref cancelled))
                        {
                            a.OnComplete();
                        }

                        Dispose(e);

                        return;
                    }
                }
            }

            protected override void SlowPath(long r)
            {
                var et = enumerator;
                var e = 0L;
                var a = actual;

                for (;;)
                {

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Dispose(et);
                            return;
                        }

                        bool consumed = a.TryOnNext(et.Current);

                        bool b;

                        try
                        {
                            b = et.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            ExceptionHelper.ThrowIfFatal(ex);
                            a.OnError(ex);

                            Dispose(et);
                            return;
                        }

                        if (!b)
                        {
                            if (!Volatile.Read(ref cancelled))
                            {
                                a.OnComplete();
                            }

                            Dispose(et);

                            return;
                        }

                        if (consumed)
                        {
                            e++;
                        }
                    }

                    r = Volatile.Read(ref requested);
                    if (r == e)
                    {
                        r = Interlocked.Add(ref requested, -e);
                        if (r == 0L)
                        {
                            break;
                        }
                        e = 0L;
                    }
                }
            }
        }
    }
}
