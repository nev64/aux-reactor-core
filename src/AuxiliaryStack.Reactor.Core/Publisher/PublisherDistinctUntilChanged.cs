using System;
using System.Collections.Generic;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDistinctUntilChanged<T, K> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, K> keySelector;

        readonly IEqualityComparer<K> comparer;

        internal PublisherDistinctUntilChanged(IPublisher<T> source, Func<T, K> keySelector, IEqualityComparer<K> comparer)
        {
            this.source = source;
            this.keySelector = keySelector;
            this.comparer = comparer;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new DistinctUntilChangedConditionalSubscriber((IConditionalSubscriber<T>)s, keySelector, comparer));
            }
            else
            {
                source.Subscribe(new DistinctUntilChangedSubscriber(s, keySelector, comparer));
            }
        }

        sealed class DistinctUntilChangedSubscriber : BasicFuseableSubscriber<T, T>, IConditionalSubscriber<T>
        {
            readonly Func<T, K> keySelector;

            readonly IEqualityComparer<K> comparer;

            K last;

            bool nonEmpty;

            public DistinctUntilChangedSubscriber(ISubscriber<T> actual, Func<T, K> keySelector, IEqualityComparer<K> comparer) : base(actual)
            {
                this.keySelector = keySelector;
                this.comparer = comparer;
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
                if (fusionMode == FusionMode.Async)
                {
                    actual.OnNext(t);
                    return;
                }

                if (!TryOnNext(t))
                {
                    s.Request(1);
                }
            }

            public bool TryOnNext(T t)
            {

                if (!nonEmpty)
                {
                    nonEmpty = true;
                    last = keySelector(t);
                    actual.OnNext(t);
                }
                else
                {
                    K k;
                    bool c;

                    try
                    {
                        k = keySelector(t);
                        c = comparer.Equals(last, k);
                    }
                    catch (Exception ex)
                    {
                        Fail(ex);
                        return true;
                    }

                    if (c)
                    {
                        last = k;
                        return false;
                    }
                    last = k;
                    actual.OnNext(t);
                }

                return true;
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                var qs = this.qs;
                if (qs != null)
                {
                    var m = qs.RequestFusion(mode);
                    if (m != FusionMode.None)
                    {
                        fusionMode = m;
                    }
                    return m;
                }
                return FusionMode.None;
            }

            public override Option<T> Poll()
            {
                var qs = this.qs;
                T local;
                if (fusionMode == FusionMode.Sync)
                {
                    for (;;)
                    {
                        var elem = qs.Poll();
                        if (elem.IsJust)
                        {
                            local = elem.GetValue();
                            K k = keySelector(local);
                            if (!nonEmpty)
                            {
                                nonEmpty = true;
                                last = k;
                                return Just(local);
                            }
                            if (comparer.Equals(last, k))
                            {
                                last = k;
                                continue;
                            }

                            last = k;
                            return Just(local);
                        }
                        
                        return None<T>();
                    }
                }
                else
                {
                    long p = 0L;
                    for (;;)
                    {
                        var elem = qs.Poll();
                        if (elem.IsJust)
                        {
                            local = elem.GetValue();
                            K k = keySelector(local);
                            if (comparer.Equals(last, k))
                            {
                                last = k;
                                p++;
                                continue;
                            }

                            last = k;
                            if (p != 0L)
                            {
                                qs.Request(p);
                            }

                            return Just(local);
                        }
                        else
                        {
                            if (p != 0L)
                            {
                                qs.Request(p);
                            }
                            
                            return None<T>();
                        }
                    }
                }
            }
        }

        sealed class DistinctUntilChangedConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, K> keySelector;

            readonly IEqualityComparer<K> comparer;

            K last;

            bool nonEmpty;

            public DistinctUntilChangedConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, K> keySelector, IEqualityComparer<K> comparer) : base(actual)
            {
                this.keySelector = keySelector;
                this.comparer = comparer;
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
                if (fusionMode == FusionMode.Async)
                {
                    actual.OnNext(t);
                    return;
                }

                if (!TryOnNext(t))
                {
                    s.Request(1);
                }
            }

            public override bool TryOnNext(T t)
            {

                if (!nonEmpty)
                {
                    nonEmpty = true;
                    last = keySelector(t);
                    actual.OnNext(t);
                }
                else
                {
                    K k;
                    bool c;

                    try
                    {
                        k = keySelector(t);
                        c = comparer.Equals(last, k);
                    }
                    catch (Exception ex)
                    {
                        Fail(ex);
                        return true;
                    }

                    if (c)
                    {
                        last = k;
                        return false;
                    }
                    last = k;
                    return actual.TryOnNext(t);
                }

                return true;
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                var qs = this.qs;
                if (qs != null)
                {
                    var m = qs.RequestFusion(mode);
                    if (m != FusionMode.None)
                    {
                        fusionMode = m;
                    }
                    return m;
                }
                return FusionMode.None;
            }

            public override Option<T> Poll()
            {
                var qs = this.qs;
                T local;
                if (fusionMode == FusionMode.Sync)
                {
                    for (;;)
                    {
                        //todo: review everything and refactor
                        var elem = qs.Poll();
                        if (elem.IsJust)
                        {
                            local = elem.GetValue();
                            K k = keySelector(local);
                            if (!nonEmpty)
                            {
                                nonEmpty = true;
                                last = k;
                                return Just(local);
                            }
                            if (comparer.Equals(last, k))
                            {
                                last = k;
                                continue;
                            }

                            last = k;
                            return Just(local);
                        }

                        return None<T>();
                    }
                }
                else
                {
                    long p = 0L;
                    for (;;)
                    {
                        var elem = qs.Poll();
                        if (elem.IsJust)
                        {
                            local = elem.GetValue();
                            K k = keySelector(local);
                            if (comparer.Equals(last, k))
                            {
                                last = k;
                                p++;
                                continue;
                            }

                            last = k;
                            if (p != 0L)
                            {
                                qs.Request(p);
                            }
                            return Just(local);
                        }
                        else
                        {
                            if (p != 0L)
                            {
                                qs.Request(p);
                            }

                            return None<T>();
                        }
                    }
                }
            }
        }
    }
}
