using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Util;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    internal sealed class PublisherRange : IFlux<int>
    {
        private readonly int _start;
        private readonly int _end;

        internal PublisherRange(int start, int count)
        {
            _start = start;
            _end = start + count;
        }

        public void Subscribe(ISubscriber<int> subscriber)
        {
            if (subscriber is IConditionalSubscriber<int> conditionalSubscriber)
            {
                subscriber.OnSubscribe(new RangeConditionalSubscription(conditionalSubscriber, _start, _end));
            }
            else
            {
                subscriber.OnSubscribe(new RangeSubscription(subscriber, _start, _end));
            }
        }

        abstract class RangeBaseSubscription : IFlowSubscription<int>
        {
            protected readonly int _end;
            protected int _index;
            protected long _requested;
            protected bool _isCancelled;

            protected RangeBaseSubscription(int start, int end)
            {
                _index = start;
                _end = end;
            }

            public void Cancel() => Volatile.Write(ref _isCancelled, true);

            public void Clear() => _index = _end;

            public bool IsEmpty() => _index == _end;

            public bool Offer(int value) => FuseableHelper.DontCallOffer();

            public Option<int> Poll()
            {
                var i = _index;
                if (i != _end)
                {
                    _index = i + 1;
                    return Just(i);
                }
                return None<int>();
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

            protected abstract void SlowPath(long requested);

            public FusionMode RequestFusion(FusionMode mode) => mode & FusionMode.Sync;
        }

        sealed class RangeSubscription: RangeBaseSubscription
        {
            private readonly ISubscriber<int> _actual;

            internal RangeSubscription(ISubscriber<int> actual, int start, int end) : base(start, end) => 
                _actual = actual;

            protected override void FastPath()
            {
                var end = _end;
                var actual = _actual;

                for (var i = _index; i != end; i++)
                {
                    if (Volatile.Read(ref _isCancelled))
                    {
                        return;
                    }

                    actual.OnNext(i);
                }

                if (Volatile.Read(ref _isCancelled))
                {
                    return;
                }
                actual.OnComplete();
            }

            protected override void SlowPath(long requested)
            {
                var end = 0L;
                var index = _index;
                var rangeEnd = _end;
                var actual = _actual;

                for (;;)
                {

                    while (end != requested && index != rangeEnd)
                    {
                        if (Volatile.Read(ref _isCancelled))
                        {
                            return;
                        }

                        actual.OnNext(index);

                        index++;
                        end++;
                    }

                    if (index == rangeEnd)
                    {
                        if (!Volatile.Read(ref _isCancelled))
                        {
                            actual.OnComplete();
                        }
                        return;
                    }

                    requested = Volatile.Read(ref _requested);
                    if (end == requested)
                    {
                        _index = index;
                        requested = Interlocked.Add(ref _requested, -end);
                        if (requested == 0L)
                        {
                            break;
                        }
                        end = 0L;
                    }
                }
            }
        }

        sealed class RangeConditionalSubscription : RangeBaseSubscription
        {
            private readonly IConditionalSubscriber<int> _actual;

            internal RangeConditionalSubscription(IConditionalSubscriber<int> actual, int start, int end) 
                : base(start, end) => 
                _actual = actual;

            protected override void FastPath()
            {
                var end = _end;
                var actual = _actual;

                for (int i = _index; i != end; i++)
                {
                    if (Volatile.Read(ref _isCancelled))
                    {
                        return;
                    }

                    actual.TryOnNext(i);
                }

                if (Volatile.Read(ref _isCancelled))
                {
                    return;
                }
                actual.OnComplete();
            }

            protected override void SlowPath(long requested)
            {
                var end = 0L;
                var index = _index;
                var rangeEnd = _end;
                var actual = _actual;

                for (;;)
                {

                    while (end != requested && index != rangeEnd)
                    {
                        if (Volatile.Read(ref _isCancelled))
                        {
                            return;
                        }

                        if (actual.TryOnNext(index))
                        {
                            end++;
                        }

                        index++;
                    }

                    if (index == rangeEnd)
                    {
                        if (!Volatile.Read(ref _isCancelled))
                        {
                            actual.OnComplete();
                        }
                        return;
                    }

                    requested = Volatile.Read(ref _requested);
                    if (end == requested)
                    {
                        _index = index;
                        requested = Interlocked.Add(ref _requested, -end);
                        if (requested == 0L)
                        {
                            break;
                        }
                        end = 0L;
                    }
                }
            }
        }
    }
}
