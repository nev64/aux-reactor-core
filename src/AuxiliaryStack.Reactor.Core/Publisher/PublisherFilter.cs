using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFilter<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, bool> predicate;

        internal PublisherFilter(IPublisher<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            if (s is IConditionalSubscriber<T>)
            {
                source.Subscribe(new FilterConditionalSubscriber((IConditionalSubscriber<T>)s, predicate));
            }
            else
            {
                source.Subscribe(new FilterSubscriber(s, predicate));
            }
        }

        sealed class FilterSubscriber : BasicFuseableSubscriber<T, T>, IConditionalSubscriber<T>
        {
            readonly Func<T, bool> predicate;

            internal FilterSubscriber(ISubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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

                if (fusionMode != FuseableHelper.NONE)
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
                    return true;
                }

                if (b)
                {
                    actual.OnNext(t);
                }

                return b;
            }

            public override bool Poll(out T value)
            {
                var qs = this.qs;
                T local;

                if (fusionMode == FuseableHelper.SYNC)
                {
                    for (;;)
                    {
                        if (qs.Poll(out local))
                        {
                            if (predicate(local))
                            {
                                value = local;
                                return true;
                            }
                        }
                        else
                        {
                            value = default(T);
                            return false;
                        }
                    }
                }
                else
                {
                    long p = 0;
                    for (;;)
                    {
                        if (qs.Poll(out local))
                        {
                            if (predicate(local))
                            {
                                if (p != 0)
                                {
                                    qs.Request(p);
                                }
                                value = local;
                                return true;
                            }
                            p++;
                        }
                        else
                        {
                            if (p != 0)
                            {
                                qs.Request(p);
                            }
                            value = default(T);
                            return false;
                        }
                    }
                }
            }

            public override int RequestFusion(int mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }

        sealed class FilterConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly Func<T, bool> predicate;

            internal FilterConditionalSubscriber(IConditionalSubscriber<T> actual, Func<T, bool> predicate) : base(actual)
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

                if (fusionMode != FuseableHelper.NONE)
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
                    return true;
                }

                if (b)
                {
                    return actual.TryOnNext(t);
                }
                return b;
            }

            public override bool Poll(out T value)
            {
                var qs = this.qs;
                T local;

                if (fusionMode == FuseableHelper.SYNC)
                {
                    for (;;)
                    {
                        if (qs.Poll(out local))
                        {
                            if (predicate(local))
                            {
                                value = local;
                                return true;
                            }
                        }
                        else
                        {
                            value = default(T);
                            return false;
                        }
                    }
                }
                else
                {
                    long p = 0;
                    for (;;)
                    {
                        if (qs.Poll(out local))
                        {
                            if (predicate(local))
                            {
                                if (p != 0)
                                {
                                    qs.Request(p);
                                }
                                value = local;
                                return true;
                            }
                            p++;
                        }
                        else
                        {
                            if (p != 0)
                            {
                                qs.Request(p);
                            }
                            value = default(T);
                            return false;
                        }
                    }
                }
            }

            public override int RequestFusion(int mode)
            {
                return TransitiveBoundaryFusion(mode);
            }
        }
    }
}
