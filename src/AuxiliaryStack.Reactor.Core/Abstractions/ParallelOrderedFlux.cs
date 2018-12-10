using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Base class for ordered parallel stream processing.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public abstract class ParallelOrderedFlux<T> : IParallelFlux<T>
    {
        /// <inheritdoc/>
        public bool IsOrdered => true;

        /// <inheritdoc/>
        public abstract int Parallelism { get; }

        /// <inheritdoc/>
        public void Subscribe(ISubscriber<T>[] subscribers)
        {
            if (!this.Validate(subscribers))
            {
                return;
            }
            int n = subscribers.Length;

            var a = new ISubscriber<IOrderedItem<T>>[n];

            for (int i = 0; i < n; i++)
            {
                a[i] = new RemoveOrdered<T>(subscribers[i]);
            }

            SubscribeMany(a);
        }

        /// <summary>
        /// Subscribe an array of ISubscribers to this ordered parallel stream
        /// that can consume IOrderedItems.
        /// </summary>
        /// <param name="subscribers">The array of IOrderedItem-receiving ISubscribers.</param>
        public abstract void SubscribeMany(ISubscriber<IOrderedItem<T>>[] subscribers);
    }
}