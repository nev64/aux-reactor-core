using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherTakeWhile<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, bool> predicate;

        internal PublisherTakeWhile(IPublisher<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new TakeWhileConditionalSubscriber((IConditionalSubscriber<T>)s, predicate));
            }
            else
            {
                source.Subscribe(new TakeWhileSubscriber(s, predicate));
            }
        }

        sealed class TakeWhileSubscriber : BasicFuseableSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            public TakeWhileSubscriber(ISubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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

                if (fusionMode != FusionMode.None)
                {
                    actual.OnNext(t);
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
                    actual.OnNext(t);
                }
                else
                {
                    s.Cancel();
                    Complete();
                }
            }

            public override Option<T> Poll()
            {
               return  qs.Poll()
                    .Filter(val =>
                    {
                        
                        if (fusionMode == FusionMode.Async)
                        {
                            actual.OnComplete();
                        }

                        return predicate(val);
                    });
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }

        sealed class TakeWhileConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            public TakeWhileConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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

                if (fusionMode != FusionMode.None)
                {
                    actual.OnNext(t);
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
                    actual.OnNext(t);
                }
                else
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

                if (fusionMode != FusionMode.None)
                {
                    return actual.TryOnNext(t);
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
                    return actual.TryOnNext(t);
                }
                s.Cancel();
                Complete();
                return false;
            }

            public override Option<T> Poll()
            {
                return qs.Poll()
                    .Filter(val =>
                    {
                        if (fusionMode == FusionMode.Async)
                        {
                            actual.OnComplete();
                        }

                        return predicate(val);
                    });
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }
    }
}
