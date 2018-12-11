using System;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;

namespace AuxiliaryStack.Reactor.Core.Util
{
    internal sealed class ArrayFlow<T> : IFlow<T>
    {
        internal T[] _array;
        internal int _mask;
        internal long _consumerIndex;
        internal long _producerIndex;

        public void Clear() => QueueHelper.Clear(this);

        public bool IsEmpty() => _producerIndex == _consumerIndex;

        public bool Offer(T value)
        {
            var array = _array;
            var mask = _mask;
            var producerIndex = _producerIndex;

            if (array == null)
            {
                array = new T[8];
                mask = 7;
                _mask = mask;
                _array = array;
                array[0] = value;
                _producerIndex = 1;
            }
            else
            if (_consumerIndex + mask + 1 == producerIndex)
            {
                var oldLen = array.Length;
                var offset = (int)producerIndex & mask;

                var newLen = oldLen << 1;
                mask = newLen - 1;

                var b = new T[newLen];

                var n = oldLen - offset;
                Array.Copy(array, offset, b, offset, n);
                Array.Copy(array, 0, b, oldLen, offset);

                _mask = mask;
                _array = b;
                b[(int)producerIndex & mask] = value;
                _producerIndex = producerIndex + 1;
            }
            else
            {
                var offset = (int)producerIndex & mask;
                array[offset] = value;
                _producerIndex = producerIndex + 1;
            }
            return true;
        }

        public Option<T> Poll()
        {
            var ci = _consumerIndex;
            if (ci != _producerIndex)
            {
                var offset = (int)ci & _mask;
                var value = _array[offset];

                _array[offset] = default;
                _consumerIndex = ci + 1;
                return Just(value);
            }
            return None<T>();
        }

        public void Drop()
        {
            var ci = _consumerIndex;
            if (ci != _producerIndex)
            {
                var offset = (int)ci & _mask;

                _array[offset] = default;
                _consumerIndex = ci + 1;
            }
        }
    }
}
