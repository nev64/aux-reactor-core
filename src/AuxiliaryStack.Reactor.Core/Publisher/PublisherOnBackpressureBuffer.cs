using System;
using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherOnBackpressureBuffer<T> : IFlux<T>
    {
        readonly IFlux<T> source;

        readonly int capacityHint;

        internal PublisherOnBackpressureBuffer(IFlux<T> source, int capacityHint)
        {
            this.source = source;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new OnBackpressureBufferConditionalSubscriber((IConditionalSubscriber<T>)s, capacityHint));
            }
            else
            {
                source.Subscribe(new OnBackpressureBufferSubscriber(s, capacityHint));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal abstract class BaseOnBackpressureBufferSubscriber : ISubscriber<T>, IFlowSubscription<T>
        {
            protected readonly IFlow<T> _flow;

            ISubscription s;

            bool outputFused;

            protected long requested;

            protected bool cancelled;

            protected bool done;

            protected Exception error;

            Pad128 p0;

            protected int wip;

            Pad120 p1;

            internal BaseOnBackpressureBufferSubscriber(int capacityHint)
            {
                this._flow = new SpscLinkedArrayFlow<T>(capacityHint);
            }

            public void OnSubscribe(ISubscription s)
            {
                if (SubscriptionHelper.Validate(ref this.s, s))
                {
                    SubscribeActual();

                    s.Request(long.MaxValue);
                }
            }

            protected abstract void SubscribeActual();

            public void OnNext(T t)
            {
                _flow.Offer(t);
                Drain();
            }

            public void OnError(Exception e)
            {
                error = e;
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnComplete()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public FusionMode RequestFusion(FusionMode mode)
            {
                if ((mode & FusionMode.Async) != 0)
                {
                    outputFused = true;
                    return FusionMode.Async;
                }
                return FusionMode.None;
            }

            public bool Offer(T value)
            {
                return FuseableHelper.DontCallOffer();
            }

            public Option<T> Poll() => _flow.Poll();

            public bool IsEmpty()
            {
                return _flow.IsEmpty();
            }

            public void Clear()
            {
                _flow.Clear();
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
                Volatile.Write(ref cancelled, true);
                if (QueueDrainHelper.Enter(ref wip))
                {
                    _flow.Clear();
                }
            }

            void Drain()
            {
                if (QueueDrainHelper.Enter(ref wip))
                {
                    if (outputFused)
                    {
                        DrainFused();
                    }
                    else
                    {
                        DrainNormal();
                    }
                }
            }

            protected abstract void DrainNormal();

            protected abstract void DrainFused();
        }

        sealed class OnBackpressureBufferSubscriber : BaseOnBackpressureBufferSubscriber
        {
            readonly ISubscriber<T> actual;

            public OnBackpressureBufferSubscriber(ISubscriber<T> actual, int capacityHint) : base(capacityHint)
            {
                this.actual = actual;
            }

            protected override void DrainFused()
            {
                int missed = 1;
                var a = actual;
                var q = _flow;

                for (;;)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        q.Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    a.OnNext(default(T));

                    if (d)
                    {
                        var ex = error;
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
                        return;
                    }
                }
            }

            protected override void DrainNormal()
            {
                int missed = 1;
                var a = actual;
                var q = _flow;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0L;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T t;

                        var elem = q.Poll();
                        bool empty = elem.IsNone;

                        if (d && empty)
                        {
                            var ex = error;
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

                        t = elem.GetValue();
                        a.OnNext(t);

                        e++;
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
                            var ex = error;
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
                        return;
                    }
                }
            }

            protected override void SubscribeActual()
            {
                actual.OnSubscribe(this);
            }
        }

        sealed class OnBackpressureBufferConditionalSubscriber : BaseOnBackpressureBufferSubscriber
        {
            readonly IConditionalSubscriber<T> actual;

            public OnBackpressureBufferConditionalSubscriber(IConditionalSubscriber<T> actual, int capacityHint) : base(capacityHint)
            {
                this.actual = actual;
            }

            protected override void DrainFused()
            {
                int missed = 1;
                var a = actual;
                var q = _flow;

                for (;;)
                {
                    if (Volatile.Read(ref cancelled))
                    {
                        q.Clear();
                        return;
                    }

                    bool d = Volatile.Read(ref done);

                    a.TryOnNext(default(T));

                    if (d)
                    {
                        var ex = error;
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
                        return;
                    }
                }
            }

            protected override void DrainNormal()
            {
                int missed = 1;
                var a = actual;
                var q = _flow;

                for (;;)
                {
                    long r = Volatile.Read(ref requested);
                    long e = 0L;

                    while (e != r)
                    {
                        if (Volatile.Read(ref cancelled))
                        {
                            q.Clear();
                            return;
                        }

                        bool d = Volatile.Read(ref done);

                        T t;
                        var elem = q.Poll();
                        bool empty = elem.IsNone;

                        if (d && empty)
                        {
                            var ex = error;
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

                        t = elem.GetValue();
                        if (a.TryOnNext(t))
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

                        bool d = Volatile.Read(ref done);

                        bool empty = q.IsEmpty();

                        if (d && empty)
                        {
                            var ex = error;
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
                        return;
                    }
                }
            }

            protected override void SubscribeActual()
            {
                actual.OnSubscribe(this);
            }
        }
    }
}
