using System;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherUsing<T, S> : IFlux<T>, IMono<T>
    {
        readonly Func<S> stateFactory;

        readonly Func<S, IPublisher<T>> sourceFactory;

        readonly Action<S> stateDisposer;

        readonly bool eager;

        internal PublisherUsing(Func<S> stateFactory, 
            Func<S, IPublisher<T>> sourceFactory, Action<S> stateDisposer,
            bool eager)
        {
            this.stateFactory = stateFactory;
            this.sourceFactory = sourceFactory;
            this.stateDisposer = stateDisposer;
            this.eager = eager;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            S state;

            try
            {
                state = stateFactory();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<T>.Error(s, ex);
                return;
            }

            IPublisher<T> p;

            try
            {
                p = sourceFactory(state);
                if (p == null)
                {
                    throw new NullReferenceException("The sourceFactory returned a null IPublisher");
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);

                if (eager)
                {
                    try
                    {
                        stateDisposer(state);
                    }
                    catch (Exception exc)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        ex = new AggregateException(ex, exc);
                    }
                }

                EmptySubscription<T>.Error(s, ex);

                if (!eager)
                {
                    try
                    {
                        stateDisposer(state);
                    }
                    catch (Exception exc)
                    {
                        ExceptionHelper.ThrowOrDrop(exc);
                    }
                }
                return;
            }


            if (s is IConditionalSubscriber<T>)
            {
                p.Subscribe(new UsingConditionalSubscriber((IConditionalSubscriber<T>)s, state, stateDisposer, eager));
            }
            else
            {
                p.Subscribe(new UsingSubscriber(s, state, stateDisposer, eager));
            }
        }

        sealed class UsingSubscriber : BasicFuseableSubscriber<T, T>
        {
            readonly S state;

            readonly Action<S> stateDisposer;

            readonly bool eager;

            int once;

            public UsingSubscriber(ISubscriber<T> actual, S state, Action<S> stateDisposer, bool eager) : base(actual)
            {
                this.state = state;
                this.stateDisposer = stateDisposer;
                this.eager = eager;
            }

            public override void OnComplete()
            {
                if (eager)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        Error(ex);
                        return;
                    }
                }

                Complete();

                if (!eager)
                {
                    DisposeState();
                }
            }

            void DisposeState()
            {
                try
                {
                    Dispose();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowOrDrop(ex);
                }
            }

            public override void OnError(Exception e)
            {
                if (eager)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        e = new AggregateException(e, ex);
                    }
                }

                Error(e);

                if (!eager)
                {
                    DisposeState();
                }
            }

            public override void OnNext(T t)
            {
                actual.OnNext(t);
            }

            public override Option<T> Poll()
            {
                return qs.Poll();
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveAnyFusion(mode);
            }

            public override void Cancel()
            {
                Dispose();
                base.Cancel();
            }

            void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    stateDisposer(state);
                }
            }
        }

        sealed class UsingConditionalSubscriber : BasicFuseableConditionalSubscriber<T, T>
        {
            readonly S state;

            readonly Action<S> stateDisposer;

            readonly bool eager;

            int once;

            public UsingConditionalSubscriber(IConditionalSubscriber<T> actual, S state, Action<S> stateDisposer, bool eager) : base(actual)
            {
                this.state = state;
                this.stateDisposer = stateDisposer;
                this.eager = eager;
            }

            public override void OnComplete()
            {
                if (eager)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        Error(ex);
                        return;
                    }
                }

                Complete();

                if (!eager)
                {
                    DisposeState();
                }
            }

            void DisposeState()
            {
                try
                {
                    Dispose();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowOrDrop(ex);
                }
            }

            public override void OnError(Exception e)
            {
                if (eager)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);
                        e = new AggregateException(e, ex);
                    }
                }

                Error(e);

                if (!eager)
                {
                    DisposeState();
                }
            }

            public override void OnNext(T t)
            {
                actual.OnNext(t);
            }

            public override bool TryOnNext(T t)
            {
                return actual.TryOnNext(t);
            }

            public override Option<T> Poll()
            {
                return qs.Poll();
            }

            public override FusionMode RequestFusion(FusionMode mode)
            {
                return TransitiveAnyFusion(mode);
            }

            public override void Cancel()
            {
                Dispose();
                base.Cancel();
            }

            void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    stateDisposer(state);
                }
            }

        }

    }
}
