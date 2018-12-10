using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Parallel
{
    sealed class ParallelFromPublishers<T> : ParallelUnorderedFlux<T>
    {
        readonly IPublisher<T>[] sources;

        internal ParallelFromPublishers(IPublisher<T>[] sources)
        {
            this.sources = sources;
        }

        public override int Parallelism => sources.Length;

        public override void Subscribe(ISubscriber<T>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }
            int n = subscribers.Length;
            for (int i = 0; i < n; i++)
            {
                sources[i].Subscribe(subscribers[i]);
            } 
        }
    }
}
