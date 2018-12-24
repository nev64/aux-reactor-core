using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSkipWhile<T> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, bool> predicate;

        internal PublisherSkipWhile(IPublisher<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new SkipWhileConditionalSubscriber((IConditionalSubscriber<T>)s, predicate));
            }
            else
            {
                source.Subscribe(new SkipWhileSubscriber(s, predicate));
            }
        }

        sealed class SkipWhileSubscriber : BasicFuseableSubscriber<T, T>, IConditionalSubscriber<T>
        {
            readonly Func<T, bool> predicate;

            bool passThrough;

            public SkipWhileSubscriber(ISubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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
                
                if (!TryOnNext(t))
                {
                    s.Request(1);
                }
            }

            public bool TryOnNext(T t)
            {
                if (done)
                {
                    return false;
                }

                if (fusionMode != FusionMode.None)
                {
                    actual.OnNext(t);
                    return true;
                }

                if (passThrough)
                {
                    actual.OnNext(t);
                    return true;
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

                if (!b)
                {
                    passThrough = true;
                    actual.OnNext(t);
                    return true;
                }
                return false;
            }

            public override Option<T> Poll()
            {
                for (;;)
                {
                    if (passThrough)
                    {
                        return qs.Poll();
                    }

                    var elem = qs.Poll();

                    if (elem.IsJust)
                    {
                        if (!predicate(elem.GetValue()))
                        {
                            passThrough = true;
                            return elem;
                        }
                        else
                        {
                            if (fusionMode != FusionMode.Sync)
                            {
                                s.Request(1);
                            }
                        }
                    } else
                    {
                        return elem;
                    }
                }
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }

        sealed class SkipWhileConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            bool passThrough;

            public SkipWhileConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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
                if (!TryOnNext(t))
                {
                    s.Request(1);
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

                if (passThrough)
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

                if (!b)
                {
                    passThrough = true;
                    return actual.TryOnNext(t);
                }
                return false;
            }

            public override Option<T> Poll()
            {
                for (;;)
                {
                    if (passThrough)
                    {
                        return qs.Poll();
                    }

                    var elem = qs.Poll();

                    if (elem.IsJust)
                    {
                        if (!predicate(elem.GetValue()))
                        {
                            passThrough = true;

                            return elem;
                        }
                        else
                        {
                            if (fusionMode != FusionMode.Sync)
                            {
                                s.Request(1);
                            }
                        }
                    }
                    else
                    {
                        return None<T>();
                    }
                }
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }
    }
}
