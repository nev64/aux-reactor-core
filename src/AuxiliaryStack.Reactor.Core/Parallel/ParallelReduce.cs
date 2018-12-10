using System;
using AuxiliaryStack.Reactor.Core.Publisher;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelReduce<T, R> : ParallelUnorderedFlux<R>
    {
        readonly IParallelFlux<T> source;

        readonly Func<R> initialFactory;

        readonly Func<R, T, R> reducer;

        public override int Parallelism
        {
            get
            {
                return source.Parallelism;
            }
        }

        internal ParallelReduce(IParallelFlux<T> source, Func<R> initialFactory, Func<R, T, R> reducer)
        {
            this.source = source;
            this.initialFactory = initialFactory;
            this.reducer = reducer;
        }

        public override void Subscribe(ISubscriber<R>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }

            int n = subscribers.Length;
            var parents = new ISubscriber<T>[n];

            for (int i = 0; i < n; i++)
            {
                R accumulator;

                try
                {
                    accumulator = initialFactory();
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);
                    foreach (var s in subscribers)
                    {
                        EmptySubscription<R>.Error(s, ex);
                    }
                    return;
                }

                parents[i] = new PublisherReduceWith<T, R>.ReduceWithSubscriber(subscribers[i], accumulator, reducer);
            }

            source.Subscribe(parents);
        }
    }
}
