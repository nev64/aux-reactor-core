namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// Basic implementation of IOrderedItem.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class OrderedItem<T> : IOrderedItem<T>
    {
        readonly long index;

        readonly T value;

        /// <inheritdoc/>
        public long Index
        {
            get
            {
                return index;
            }
        }

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                return value;
            }
        }

        /// <summary>
        /// Constructs an ordered item with the given index and value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public OrderedItem(long index, T value)
        {
            this.index = index;
            this.value = value;
        }

        /// <inheritdoc/>
        public int CompareTo(IOrderedItem<T> other)
        {
            return index < other.Index ? -1 : (index > other.Index ? 1 : 0);
        }

        /// <inheritdoc/>
        public IOrderedItem<R> Replace<R>(R value)
        {
            return new OrderedItem<R>(index, value);
        }
    }
}
