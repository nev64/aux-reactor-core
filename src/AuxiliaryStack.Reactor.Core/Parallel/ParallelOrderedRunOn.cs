using AuxiliaryStack.Reactor.Core.Flow;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelOrderedRunOn<T> : ParallelOrderedFlux<T>
    {
        readonly ParallelOrderedFlux<T> source;

        readonly IScheduler scheduler;

        readonly int prefetch;

        public override int Parallelism
        {
            get
            {
                return source.Parallelism;
            }
        }

        internal ParallelOrderedRunOn(ParallelOrderedFlux<T> source, IScheduler scheduler,
            int prefetch)
        {
            this.source = source;
            this.scheduler = scheduler;
            this.prefetch = prefetch;
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
                var worker = scheduler.CreateWorker();
                var s = subscribers[i];
                if (s is IConditionalSubscriber<IOrderedItem<T>>)
                {
                    parents[i] = new ParallelUnorderedRunOn<IOrderedItem<T>>
                        .ParallelObserveOnConditionalSubscriber(
                        (IConditionalSubscriber<IOrderedItem<T>>)s, prefetch, worker);
                }
                else
                {
                    parents[i] = new ParallelUnorderedRunOn<IOrderedItem<T>>
                        .ParallelObserveOnSubscriber(s, prefetch, worker);
                }
            }

            source.SubscribeMany(parents);
        }
    }
}
