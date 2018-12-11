using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelOrderedFilter<T> : ParallelOrderedFlux<T>
    {
        private readonly ParallelOrderedFlux<T> _source;
        private readonly Func<T, bool> _predicate;

        public override int Parallelism => _source.Parallelism;

        internal ParallelOrderedFilter(ParallelOrderedFlux<T> source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public override void SubscribeMany(ISubscriber<IOrderedItem<T>>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }
            int n = subscribers.Length;

            var parents = new ISubscriber<IOrderedItem<T>>[n];

            for (int i = 0; i < n; i++)
            {
                var s = subscribers[i];
                if (s is IConditionalSubscriber<T>)
                {
                    parents[i] = new ParallelFilterConditionalSubscriber(
                        (IConditionalSubscriber<IOrderedItem<T>>)s, _predicate);
                }
                else
                {
                    parents[i] = new ParallelFilterSubscriber(s, _predicate);
                }
            }
            _source.SubscribeMany(parents);
        }

        sealed class ParallelFilterSubscriber : BasicSubscriber<IOrderedItem<T>, IOrderedItem<T>>, IConditionalSubscriber<IOrderedItem<T>>
        {
            private readonly Func<T, bool> _predicate;

            public ParallelFilterSubscriber(ISubscriber<IOrderedItem<T>> actual, Func<T, bool> predicate) : base(actual)
            {
                _predicate = predicate;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(IOrderedItem<T> t)
            {
                if (!TryOnNext(t))
                {
                    _subscription.Request(1);
                }
            }

            public bool TryOnNext(IOrderedItem<T> t)
            {
                if (_isCompleted)
                {
                    return false;
                }

                bool b;

                try
                {
                    b = _predicate(t.Value);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return false;
                }

                if (b)
                {
                    _actual.OnNext(t);
                    return true;
                }
                return false;
            }
        }

        sealed class ParallelFilterConditionalSubscriber : BasicConditionalSubscriber<IOrderedItem<T>, IOrderedItem<T>>
        {
            private readonly Func<T, bool> _predicate;

            public ParallelFilterConditionalSubscriber(IConditionalSubscriber<IOrderedItem<T>> actual, Func<T, bool> predicate) : base(actual)
            {
                _predicate = predicate;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(IOrderedItem<T> t)
            {
                if (!TryOnNext(t))
                {
                    s.Request(1);
                }
            }

            public override bool TryOnNext(IOrderedItem<T> t)
            {
                if (done)
                {
                    return false;
                }

                bool b;

                try
                {
                    b = _predicate(t.Value);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return false;
                }

                if (b)
                {
                    return actual.TryOnNext(t);
                }
                return false;
            }
        }
    }
}
