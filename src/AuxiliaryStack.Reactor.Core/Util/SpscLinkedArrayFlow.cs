﻿using System.Runtime.InteropServices;
using System.Threading;
using AuxiliaryStack.Monads;
using AuxiliaryStack.Reactor.Core.Flow;
using static AuxiliaryStack.Monads.Option;

/* 
 * The algorithm was inspired by the Fast-Flow implementation in the JCTools library at
 * https://github.com/JCTools/JCTools/blob/master/jctools-core/src/main/java/org/jctools/queues/SpscArrayQueue.java
 * 
 * The difference, as of now, is there is no item padding and no lookahead.
 */

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// A single-producer, single-consumer, unbounded concurrent queue
    /// with an island size to avoid frequent reallocation.
    /// </summary>
    /// <typeparam name="T">The stored value type</typeparam>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public sealed class SpscLinkedArrayFlow<T> : IFlow<T>
    {
        readonly int mask;

        Pad120 p0;

        long producerIndex;
        Entry[] producerArray;

        Pad112 p1;

        long consumerIndex;
        Entry[] consumerArray;

        Pad112 p2;

        /// <summary>
        /// Constructs an instance with the given capacity rounded up to
        /// the next power-of-2 value.
        /// </summary>
        /// <param name="capacity">The target capacity.</param>
        public SpscLinkedArrayFlow(int capacity)
        {
            int c = QueueHelper.Round(capacity < 2 ? 2 : capacity);
            mask = c - 1;
            consumerArray = producerArray = new Entry[c];
            Volatile.Write(ref consumerIndex, 0L); // FIXME not sure if C# constructor with readonly field does release or not
        }

        /// <inheritdoc/>
        public bool Offer(T value)
        {
            var a = producerArray;
            int m = mask;
            long pi = producerIndex;

            int offset = (int)(pi + 1) & m;

            if (a[offset].Flag != 0)
            {
                offset = (int)pi & m;
                var b = new Entry[m + 1];
                b[offset].value = value;
                b[offset].FlagPlain = 1;
                a[offset].next = b;
                producerArray = b;
                a[offset].Flag = 2;
            }
            else
            {
                offset = (int)pi & m;
                a[offset].value = value;
                a[offset].Flag = 1;
            }

            Volatile.Write(ref producerIndex, pi + 1);
            return true;
        }

        /// <inheritdoc/>
        public Option<T> Poll()
        {
            var a = consumerArray;
            int m = mask;
            long ci = consumerIndex;

            int offset = (int)ci & m;

            int f = a[offset].Flag;

            if (f == 0)
            {
                return None<T>();
            }

            T value;
            if (f == 1)
            {
                value = a[offset].value;
                a[offset].value = default;
                a[offset].Flag = 0;
            } else
            {
                var b = a[offset].next;
                consumerArray = b;
                a[offset].next = null;
                value = b[offset].value;
                b[offset].value = default(T);
                b[offset].Flag = 0;
            }

            Volatile.Write(ref consumerIndex, ci + 1);
            return Just(value);
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return Volatile.Read(ref producerIndex) == Volatile.Read(ref consumerIndex);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            QueueHelper.Clear(this);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct Entry
        {
            /// <summary>
            /// Indicates the occupancy of the entry.
            /// </summary>
            int flag;

            /// <summary>
            /// The entry value.
            /// </summary>
            internal T value;

            /// <summary>
            /// Pointer to the next array.
            /// </summary>
            internal Entry[] next;

            /// <summary>
            /// Pad out to 32 bytes.
            /// </summary>
            long pad;

            /// <summary>
            /// Accesses the flag field with Volatile.
            /// </summary>
            internal int Flag
            {
                get
                {
                    return Volatile.Read(ref flag);
                }
                set
                {
                    Volatile.Write(ref flag, value);
                }
            }

            /// <summary>
            /// Write into the flag with plain access mode.
            /// </summary>
            internal int FlagPlain
            {
                set
                {
                    flag = value;
                }
            }
        }
    }
}
