using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// A queue with capacity of one.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class SpscOneFlow<T> : IFlow<T>
    {
        private bool _hasValue;
        private T _value;

        /// <inheritdoc/>
        public void Clear()
        {
            _value = default;
            Volatile.Write(ref _hasValue, false);
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return !Volatile.Read(ref _hasValue);
        }

        /// <inheritdoc/>
        public bool Offer(T value)
        {
            if (Volatile.Read(ref _hasValue))
            {
                return false;
            }
            _value = value;
            Volatile.Write(ref _hasValue, true);
            return true;
        }

        /// <inheritdoc/>
        public Option<T> Poll()
        {
            if (Volatile.Read(ref _hasValue))
            {
                var value = _value;
                _value = default;
                Volatile.Write(ref _hasValue, false);
                return Just(value);
            }
            return None<T>();
        }
    }
}
