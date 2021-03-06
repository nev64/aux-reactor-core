﻿using System;
using System.Collections.Generic;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using static AuxiliaryStack.Monads.Option;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherDistinct<T, K> : IFlux<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, K> keySelector;

        readonly IEqualityComparer<K> comparer;

        internal PublisherDistinct(IPublisher<T> source, Func<T, K> keySelector, IEqualityComparer<K> comparer)
        {
            this.source = source;
            this.keySelector = keySelector;
            this.comparer = comparer;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new DistinctConditionalSubscriber((IConditionalSubscriber<T>)s, keySelector, comparer));
            }
            else
            {
                source.Subscribe(new DistinctSubscriber(s, keySelector, comparer));
            }
        }

        sealed class DistinctSubscriber : BasicFuseableSubscriber<T, T>, IConditionalSubscriber<T>
        {
            readonly Func<T, K> keySelector;

            readonly HashSet<K> set;

            public DistinctSubscriber(ISubscriber<T> actual, Func<T, K> keySelector, IEqualityComparer<K> comparer) : base(actual)
            {
                this.keySelector = keySelector;
                this.set = new HashSet<K>(comparer);
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
                if (fusionMode != FusionMode.None)
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
                K k;
                
                k = keySelector(t);

                if (set.Contains(k))
                {
                    return false;
                }

                set.Add(k);
                actual.OnNext(t);
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
                            if (set.Contains(k))
                            {
                                continue;
                            }
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
                        
                        //todo: map
                        var elem = qs.Poll();
                        if (elem.IsJust)
                        {
                            local = elem.GetValue();
                            K k = keySelector(local);
                            if (set.Contains(k))
                            {
                                p++;
                                continue;
                            }
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

        sealed class DistinctConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, K> keySelector;

            readonly HashSet<K> set;

            public DistinctConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, K> keySelector, IEqualityComparer<K> comparer) : base(actual)
            {
                this.keySelector = keySelector;
                this.set = new HashSet<K>(comparer);
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
                if (fusionMode != FusionMode.None)
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
                K k;

                k = keySelector(t);

                if (set.Contains(k))
                {
                    return false;
                }

                set.Add(k);
                return actual.TryOnNext(t);
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
                            if (set.Contains(k))
                            {
                                continue;
                            }
                            set.Add(k);
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
                            if (set.Contains(k))
                            {
                                p++;
                                continue;
                            }
                            if (p != 0L)
                            {
                                qs.Request(p);
                            }
                            
                            return Just(local);
                        }

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
