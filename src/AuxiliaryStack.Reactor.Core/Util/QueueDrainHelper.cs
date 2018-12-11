using System.Threading;
using AuxiliaryStack.Reactor.Core.Flow;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// Helper methods to work with the regular queue-drain serialization approach
    /// </summary>
    public static class QueueDrainHelper
    {
        /// <summary>
        /// Atomically increment the work-in-progress counter and return true if
        /// it transitioned from 0 to 1.
        /// </summary>
        /// <param name="wip">The work-in-progress field</param>
        /// <returns>True if the counter transitioned from 0 to 1</returns>
        public static bool Enter(ref int wip)
        {
            return Interlocked.Increment(ref wip) == 1;
        }

        /// <summary>
        /// Atomically try to decrement the work-in-progress counter and return
        /// its new value.
        /// </summary>
        /// <param name="wip">The target work-in-progress counter field</param>
        /// <param name="missed">The number to decrement the counter, positive (not verified)</param>
        /// <returns>The new work-in-progress value</returns>
        public static int Leave(ref int wip, int missed)
        {
            int w = Volatile.Read(ref wip);
            if (w == missed)
            {
                return Interlocked.Add(ref wip, -missed);
            }
            else
            {
                return w;
            }
        }

        /// <summary>
        /// Constructs a queue based on the prefetch value.
        /// </summary>
        /// <typeparam name="T">The queue element type</typeparam>
        /// <param name="capacityHint">If negative, an SpscLinkedArrayQueue is created with
        /// capacity hint as the absolute of capacityHint,
        /// if one, an SpscOneQueue is created. Otherwise, an SpscArrayQueue is created with
        /// the capacityHint.</param>
        /// <returns></returns>
        public static IFlow<T> CreateQueue<T>(int capacityHint)
        {
            if (capacityHint < 0)
            {
                return new SpscLinkedArrayFlow<T>(-capacityHint);
            }
            else
            if (capacityHint == 1)
            {
                return new SpscOneFlow<T>();
            }
            return new SpscArrayFlow<T>(capacityHint);
        }

        /// <summary>
        /// Tries to enter the drain mode via a fast-path method.
        /// </summary>
        /// <param name="wip">The work-in-progress field to change</param>
        /// <returns>True if successful</returns>
        public static bool TryEnter(ref int wip)
        {
            return Volatile.Read(ref wip) == 0 && Interlocked.CompareExchange(ref wip, 1, 0) == 0;
        }
    }
}
