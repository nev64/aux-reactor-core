using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherArray<T> : IFlux<T>
    {
        private readonly T[] _array;

        internal PublisherArray(T[] array)
        {
            _array = array;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T> conditionalSubscriber)
            {
                s.OnSubscribe(new ArrayConditionalSubscription(conditionalSubscriber, _array));
            }
            else
            {
                s.OnSubscribe(new ArraySubscription(s, _array));
            }
        }

        internal abstract class ArrayBaseSubscription : IFlowSubscription<T>
        {
            protected readonly T[] _array;
            protected int _index;
            protected long _requested;
            protected bool _isCancelled;

            internal ArrayBaseSubscription(T[] array)
            {
                _array = array;
            }

            public void Cancel() => Volatile.Write(ref _isCancelled, true);

            public void Clear() => _index = _array.Length;

            public bool IsEmpty() => _index == _array.Length;

            public bool Offer(T value) => FuseableHelper.DontCallOffer();

            public Option<T> Poll()
            {
                var i = _index;
                var a = _array;
                if (i != a.Length)
                {
                    _index = i + 1;
                    return Just(a[i]);
                }
                
                return None<T>();
            }

            public void Request(long n)
            {
                if (BackpressureHelper.ValidateAndAddCap(ref _requested, n) == 0L)
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

            public FusionMode RequestFusion(FusionMode mode)
            {
                return mode & FusionMode.Sync;
            }
        }

        internal sealed class ArraySubscription : ArrayBaseSubscription
        {
            private readonly ISubscriber<T> _actual;

            internal ArraySubscription(ISubscriber<T> actual, T[] array) : base(array)
            {
                _actual = actual;
            }

            protected override void FastPath()
            {
                var b = _array;
                int e = b.Length;
                var a = _actual;

                for (int i = _index; i != e; i++)
                {
                    if (Volatile.Read(ref _isCancelled))
                    {
                        return;
                    }

                    a.OnNext(b[i]);
                }

                if (Volatile.Read(ref _isCancelled))
                {
                    return;
                }
                a.OnComplete();
            }

            protected override void SlowPath(long r)
            {
                var b = _array;
                var f = b.Length;

                var e = 0L;
                var i = _index;
                var a = _actual;

                for (;;)
                {

                    while (e != r && i != f)
                    {
                        if (Volatile.Read(ref _isCancelled))
                        {
                            return;
                        }

                        a.OnNext(b[i]);

                        i++;
                        e++;
                    }

                    if (i == f)
                    {
                        if (!Volatile.Read(ref _isCancelled))
                        {
                            a.OnComplete();
                        }
                        return;
                    }

                    r = Volatile.Read(ref _requested);
                    if (e == r)
                    {
                        _index = i;
                        r = Interlocked.Add(ref _requested, -e);
                        if (r == 0L)
                        {
                            break;
                        }
                        e = 0L;
                    }
                }
            }
        }

        sealed class ArrayConditionalSubscription : ArrayBaseSubscription
        {
            private readonly IConditionalSubscriber<T> _actual;

            internal ArrayConditionalSubscription(IConditionalSubscriber<T> actual, T[] array) : base(array)
            {
                _actual = actual;
            }

            protected override void FastPath()
            {
                var b = _array;
                var e = b.Length;
                var a = _actual;

                for (var i = _index; i != e; i++)
                {
                    if (Volatile.Read(ref _isCancelled))
                    {
                        return;
                    }

                    a.TryOnNext(b[i]);
                }

                if (Volatile.Read(ref _isCancelled))
                {
                    return;
                }
                a.OnComplete();
            }

            protected override void SlowPath(long r)
            {
                var b = _array;
                var f = b.Length;

                var e = 0L;
                var i = _index;
                var a = _actual;

                for (;;)
                {

                    while (e != r && i != f)
                    {
                        if (Volatile.Read(ref _isCancelled))
                        {
                            return;
                        }

                        if (a.TryOnNext(b[i]))
                        {
                            e++;
                        }

                        i++;
                    }

                    if (i == f)
                    {
                        if (!Volatile.Read(ref _isCancelled))
                        {
                            a.OnComplete();
                        }
                        return;
                    }

                    r = Volatile.Read(ref _requested);
                    if (e == r)
                    {
                        _index = i;
                        r = Interlocked.Add(ref _requested, -e);
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
