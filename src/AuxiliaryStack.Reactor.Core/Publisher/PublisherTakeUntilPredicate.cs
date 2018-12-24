using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherTakeUntilPredicate<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, bool> predicate;

        internal PublisherTakeUntilPredicate(IPublisher<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new TakeUntilPredicateConditionalSubscriber((IConditionalSubscriber<T>) s, predicate));
            }
            else
            {
                source.Subscribe(new TakeUntilPredicateSubscriber(s, predicate));
            }
        }

        sealed class TakeUntilPredicateSubscriber : BasicFuseableSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            public TakeUntilPredicateSubscriber(ISubscriber<T> actual, Func<T, bool> predicate) : base(actual)
            {
                this.predicate = predicate;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                actual.OnNext(t);

                if (fusionMode != FusionMode.None)
                {
                    return;
                }

                bool b;

                try
                {
                    b = predicate(t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }

                if (b)
                {
                    s.Cancel();
                    Complete();
                }
            }

            public override Option<T> Poll()
            {
                if (done)
                {
                    if (fusionMode == FusionMode.Async)
                    {
                        actual.OnComplete();
                    }

                    return None<T>();
                }

                var result = qs.Poll();

                if (result.IsJust)
                {
                    bool d = predicate(result.GetValue());
                    done = d;
                }

                return result;
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }

        sealed class TakeUntilPredicateConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            public TakeUntilPredicateConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, bool> predicate) :
                base(actual)
            {
                this.predicate = predicate;
            }

            public override void OnComplete()
            {
                Complete();
            }

            public override void OnError(Exception e)
            {
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                actual.OnNext(t);

                if (fusionMode != FusionMode.None)
                {
                    return;
                }

                bool b;

                try
                {
                    b = predicate(t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }

                if (b)
                {
                    s.Cancel();
                    Complete();
                }
            }

            public override bool TryOnNext(T t)
            {
                if (done)
                {
                    return false;
                }

                bool c = actual.TryOnNext(t);

                if (fusionMode != FusionMode.None)
                {
                    return c;
                }

                bool b;

                try
                {
                    b = predicate(t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return false;
                }

                if (b)
                {
                    s.Cancel();
                    Complete();
                    return false;
                }

                return c;
            }

            public override Option<T> Poll()
            {
                if (done)
                {
                    if (fusionMode == FusionMode.Async)
                    {
                        actual.OnComplete();
                    }

                    return None<T>();
                }


                var result = qs.Poll();

                if (result.IsJust)
                {
                    bool d = predicate(result.GetValue());
                    done = d;
                }

                return result;
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }
    }
}