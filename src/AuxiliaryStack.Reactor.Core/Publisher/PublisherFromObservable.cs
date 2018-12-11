using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFromObservable<T> : IFlux<T>
    {
        readonly IObservable<T> source;

        readonly BackpressureHandling backpressure;

        internal PublisherFromObservable(IObservable<T> source, BackpressureHandling backpressure)
        {
            this.source = source;
            this.backpressure = backpressure;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            switch (backpressure)
            {
                case BackpressureHandling.Error:
                    {
                        ErrorObserver o = new ErrorObserver(s);
                        s.OnSubscribe(o);
                        IDisposable d = source.Subscribe(o);
                        o.SetDisposable(d);
                    }
                    break;
                case BackpressureHandling.Drop:
                    {
                        ErrorObserver o = new ErrorObserver(s);
                        s.OnSubscribe(o);
                        IDisposable d = source.Subscribe(o);
                        o.SetDisposable(d);
                    }
                    break;
                case BackpressureHandling.Latest:
                    {
                        if (s is IConditionalSubscriber<T>)
                        {
                            LatestConditionalObserver o = new LatestConditionalObserver((IConditionalSubscriber<T>)s);
                            s.OnSubscribe(o);
                            IDisposable d = source.Subscribe(o);
                            o.SetDisposable(d);
                        }
                        else
                        {
                            LatestObserver o = new LatestObserver(s);
                            s.OnSubscribe(o);
                            IDisposable d = source.Subscribe(o);
                            o.SetDisposable(d);
                        }
                    }
                    break;
                case BackpressureHandling.Buffer:
                    {
                        if (s is IConditionalSubscriber<T>)
                        {
                            BufferConditionalObserver o = new BufferConditionalObserver((IConditionalSubscriber<T>)s, Flux.BufferSize);
                            s.OnSubscribe(o);
                            IDisposable d = source.Subscribe(o);
                            o.SetDisposable(d);
                        }
                        else
                        {
                            BufferObserver o = new BufferObserver(s, Flux.BufferSize);
                            s.OnSubscribe(o);
                            IDisposable d = source.Subscribe(o);
                            o.SetDisposable(d);
                        }
                    }
                    break;
                default:
                    {
                        NoneObserver o = new NoneObserver(s);
                        s.OnSubscribe(o);
                        IDisposable d = source.Subscribe(o);
                        o.SetDisposable(d);
                    }
                    break;
            }
        }

        internal sealed class NoneObserver : BasicRejectingSubscription<T>, IObserver<T>
        {
            readonly ISubscriber<T> actual;

            IDisposable d;

            public NoneObserver(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public override void Cancel()
            {
                DisposableHelper.Dispose(ref d);
            }

            public void OnCompleted()
            {
                actual.OnComplete();
            }

            public void OnError(Exception error)
            {
                actual.OnError(error);
            }

            public void OnNext(T value)
            {
                actual.OnNext(value);
            }

            public override void Request(long n)
            {
                // ignored
            }

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }
        }

        sealed class ErrorObserver : BasicRejectingSubscription<T>, IObserver<T>
        {
            readonly ISubscriber<T> actual;

            IDisposable d;

            long requested;

            long produced;

            bool done;

            public ErrorObserver(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public override void Cancel()
            {
                DisposableHelper.Dispose(ref d);
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                done = true;
                actual.OnComplete();
            }

            public void OnError(Exception error)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(error);
                    return;
                }
                done = true;
                actual.OnError(error);
            }

            public void OnNext(T value)
            {
                if (done)
                {
                    return;
                }

                long r = Volatile.Read(ref requested);
                long p = produced;

                if (r != p)
                {
                    produced = p + 1;
                    actual.OnNext(value);
                }
                else
                {
                    done = true;
                    Cancel();
                    actual.OnError(BackpressureHelper.MissingBackpressureException());
                }
            }

            public override void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                }
            }

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }
        }

        sealed class DropObserver : BasicRejectingSubscription<T>, IObserver<T>
        {
            readonly ISubscriber<T> actual;

            IDisposable d;

            long requested;

            long produced;

            public DropObserver(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public override void Cancel()
            {
                DisposableHelper.Dispose(ref d);
            }

            public void OnCompleted()
            {
                actual.OnComplete();
            }

            public void OnError(Exception error)
            {
                actual.OnError(error);
            }

            public void OnNext(T value)
            {
                long r = Volatile.Read(ref requested);
                long p = produced;

                if (r != p)
                {
                    produced = p + 1;
                    actual.OnNext(value);
                }
            }

            public override void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                }
            }

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class BufferObserver : IObserver<T>, IFlowSubscription<T>
        {
            readonly ISubscriber<T> actual;

            readonly IFlow<T> _flow;

            bool outputFused;

            IDisposable d;

            bool done;

            Exception error;

            bool cancelled;

            Pad128 p0;

            long requested;

            Pad120 p1;

            int wip;

            Pad120 p2;

            public BufferObserver(ISubscriber<T> actual, int bufferSize)
            {
                this.actual = actual;
                this._flow = new SpscLinkedArrayFlow<T>(bufferSize);
            }

            public void Cancel()
            {
                if (Volatile.Read(ref cancelled))
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                DisposableHelper.Dispose(ref d);

                if (QueueDrainHelper.Enter(ref wip))
                {
                    _flow.Clear();
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

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }

            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                if (outputFused)
                {
                    DrainOutput();
                }
                else
                {
                    DrainRegular();
                }
            }

            void DrainOutput()
            {
                var q = _flow;
                var a = actual;

                int missed = 1;

                for (;;)
                {

                    if (Volatile.Read(ref cancelled))
                    {
                        q.Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    T v;
                    var elem = _flow.Poll();
                    bool empty = elem.IsNone;

                    if (!empty)
                    {
                        a.OnNext(default(T));
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
                        return;
                    }
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void DrainRegular()
            {
                var q = _flow;
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T v;
                        var elem = _flow.Poll();
                        bool empty = elem.IsNone;

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
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        v = elem.GetValue();
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

                        if (Volatile.Read(ref done) && q.IsEmpty())
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
                            return;
                        }
                    }

                    if (e != 0L && r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -e);
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T value)
            {
                _flow.Offer(value);
                Drain();
            }

            public int RequestFusion(int mode)
            {
                int m = mode & FuseableHelper.ASYNC;
                outputFused = m != 0;
                return m;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<T> Poll()
            {
                return _flow.Poll();
            }

            public bool IsEmpty()
            {
                return _flow.IsEmpty();
            }

            public void Clear()
            {
                _flow.Clear();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class BufferConditionalObserver : IObserver<T>, IFlowSubscription<T>
        {
            readonly IConditionalSubscriber<T> actual;

            readonly IFlow<T> _flow;

            bool outputFused;

            IDisposable d;

            bool done;

            Exception error;

            bool cancelled;

            Pad128 p0;

            long requested;

            Pad120 p1;

            int wip;

            Pad120 p2;

            public BufferConditionalObserver(IConditionalSubscriber<T> actual, int bufferSize)
            {
                this.actual = actual;
                this._flow = new SpscLinkedArrayFlow<T>(bufferSize);
            }

            public void Cancel()
            {
                if (Volatile.Read(ref cancelled))
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                DisposableHelper.Dispose(ref d);

                if (QueueDrainHelper.Enter(ref wip))
                {
                    _flow.Clear();
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

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }

            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                if (outputFused)
                {
                    DrainOutput();
                }
                else
                {
                    DrainRegular();
                }
            }

            void DrainOutput()
            {
                var q = _flow;
                var a = actual;

                int missed = 1;

                for (;;)
                {

                    if (Volatile.Read(ref cancelled))
                    {
                        q.Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    T v;
                    var elem = _flow.Poll();
                    bool empty = elem.IsNone;

                    if (!empty)
                    {
                        a.TryOnNext(default(T));
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
                        return;
                    }
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void DrainRegular()
            {
                var q = _flow;
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T v;
                        var elem = _flow.Poll();
                        bool empty = elem.IsNone;

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
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        v = elem.GetValue();
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

                        if (Volatile.Read(ref done) && q.IsEmpty())
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
                            return;
                        }
                    }

                    if (e != 0L && r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -e);
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T value)
            {
                _flow.Offer(value);
                Drain();
            }

            public int RequestFusion(int mode)
            {
                int m = mode & FuseableHelper.ASYNC;
                outputFused = m != 0;
                return m;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<T> Poll()
            {
                return _flow.Poll();
            }

            public bool IsEmpty()
            {
                return _flow.IsEmpty();
            }

            public void Clear()
            {
                _flow.Clear();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        sealed class LatestObserver : IObserver<T>, IFlowSubscription<T>
        {
            readonly ISubscriber<T> actual;

            IDisposable d;

            bool outputFused;

            bool done;

            Exception error;

            bool cancelled;

            Pad128 p0;

            Entry entry;

            Pad120 p1;

            long requested;

            Pad120 p2;

            int wip;

            Pad120 p3;

            internal LatestObserver(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T value)
            {
                Volatile.Write(ref entry, new Entry(value));
                Drain();
            }

            public int RequestFusion(int mode)
            {
                int m = mode & FuseableHelper.ASYNC;
                outputFused = m != 0;
                return m;
            }

            public Option<T> Poll()
            {
                var e = Volatile.Read(ref entry);

                if (e != null)
                {
                    e = Interlocked.Exchange(ref entry, null);
                    return Just(e.value);
                }
                return None<T>();
            }

            public bool IsEmpty()
            {
                return Volatile.Read(ref entry) == null;
            }

            public void Clear()
            {
                entry = null;
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Drain();
                }
            }

            public void Cancel()
            {
                if (Volatile.Read(ref cancelled))
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                DisposableHelper.Dispose(ref d);

                if (QueueDrainHelper.Enter(ref wip))
                {
                    entry = null;
                }
            }


            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                if (outputFused)
                {
                    DrainOutput();
                }
                else
                {
                    DrainRegular();
                }
            }

            void DrainOutput()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {

                    if (Volatile.Read(ref cancelled))
                    {
                        Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    bool empty = IsEmpty();

                    if (!empty)
                    {
                        a.OnNext(default(T));
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
                        return;
                    }
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void DrainRegular()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T v;

                        var elem = Poll();
                        bool empty = elem.IsNone;

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
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        v = elem.GetValue();
                        a.OnNext(v);

                        e++;
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Clear();
                            return;
                        }

                        if (Volatile.Read(ref done) && IsEmpty())
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
                            return;
                        }
                    }

                    if (e != 0L && r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -e);
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
        sealed class LatestConditionalObserver : IObserver<T>, IFlowSubscription<T>
        {
            readonly IConditionalSubscriber<T> actual;

            IDisposable d;

            bool outputFused;

            bool done;

            Exception error;

            bool cancelled;

            Pad128 p0;

            Entry entry;

            Pad120 p1;

            long requested;

            Pad120 p2;

            int wip;

            Pad120 p3;

            internal LatestConditionalObserver(IConditionalSubscriber<T> actual)
            {
                this.actual = actual;
            }

            internal void SetDisposable(IDisposable d)
            {
                DisposableHelper.Replace(ref this.d, d);
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                this.error = error;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T value)
            {
                Volatile.Write(ref entry, new Entry(value));
                Drain();
            }

            public int RequestFusion(int mode)
            {
                int m = mode & FuseableHelper.ASYNC;
                outputFused = m != 0;
                return m;
            }

            public Option<T> Poll()
            {
                var e = Volatile.Read(ref entry);

                if (e != null)
                {
                    e = Interlocked.Exchange(ref entry, null);
                    return Just(e.value);
                }
                return None<T>();
            }

            public bool IsEmpty()
            {
                return Volatile.Read(ref entry) == null;
            }

            public void Clear()
            {
                entry = null;
            }

            public void Request(long n)
            {
                if (SubscriptionHelper.Validate(n))
                {
                    BackpressureHelper.GetAndAddCap(ref requested, n);
                    Drain();
                }
            }

            public void Cancel()
            {
                if (Volatile.Read(ref cancelled))
                {
                    return;
                }
                Volatile.Write(ref cancelled, true);
                DisposableHelper.Dispose(ref d);

                if (QueueDrainHelper.Enter(ref wip))
                {
                    entry = null;
                }
            }


            void Drain()
            {
                if (!QueueDrainHelper.Enter(ref wip))
                {
                    return;
                }

                if (outputFused)
                {
                    DrainOutput();
                }
                else
                {
                    DrainRegular();
                }
            }

            void DrainOutput()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {

                    if (Volatile.Read(ref cancelled))
                    {
                        Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    bool empty = IsEmpty();

                    if (!empty)
                    {
                        a.TryOnNext(default(T));
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
                        return;
                    }
                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            void DrainRegular()
            {
                var a = actual;

                int missed = 1;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T v;
                        var elem = Poll();
                        bool empty = elem.IsNone;

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
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        v = elem.GetValue();
                        if (a.TryOnNext(v))
                        {
                            e++;
                        }
                    }

                    if (e == r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            Clear();
                            return;
                        }

                        if (Volatile.Read(ref done) && IsEmpty())
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
                            return;
                        }
                    }

                    if (e != 0L && r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -e);
                    }

                    missed = QueueDrainHelper.Leave(ref wip, missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }

        sealed class Entry
        {
            internal readonly T value;

            internal Entry(T value)
            {
                this.value = value;
            }
        }
    }
}
