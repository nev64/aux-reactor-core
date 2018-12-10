﻿using AuxiliaryStack.Reactor.Core.Util;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherMergeArray<T> : IFlux<T>
    {
        readonly IPublisher<T>[] sources;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        readonly int prefetch;

        internal PublisherMergeArray(IPublisher<T>[] sources, bool delayErrors, int maxConcurrency, int prefetch)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
            this.prefetch = prefetch;
        }

        internal PublisherMergeArray<T> MergeWith(IPublisher<T> other, bool delayError)
        {
            if (delayError != this.delayErrors)
            {
                return new PublisherMergeArray<T>(new IPublisher<T>[] { this, other }, delayError, 2, prefetch);
            }
            var a = MultiSourceHelper.AppendLast(sources, other);

            return new PublisherMergeArray<T>(a, delayErrors, maxConcurrency != int.MaxValue ? maxConcurrency + 1 : int.MaxValue, prefetch);
        }

        public void Subscribe(ISubscriber<T> s)
        {
            var parent = new PublisherFlatMap<IPublisher<T>, T>.FlatMapSubscriber(s, v => v, delayErrors, maxConcurrency, prefetch);
            parent.OnSubscribe(new PublisherArray<IPublisher<T>>.ArraySubscription(parent, sources));
        }
    }
}
