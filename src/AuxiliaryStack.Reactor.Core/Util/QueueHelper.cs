using AuxiliaryStack.Reactor.Core.Flow;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// Utility methods to work with IQueues.
    /// </summary>
    internal static class QueueHelper
    {
        /// <summary>
        /// Rounds the value to a power-of-2 if not already power of 2
        /// </summary>
        /// <param name="v">The value to round</param>
        /// <returns>The rounded value.</returns>
        internal static int Round(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return v + 1;
        }

        /// <summary>
        /// Clear the queue by polling until no more items left.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="q">The source queue.</param>
        internal static void Clear<T>(IFlow<T> q)
        {
            while (q.Poll().IsJust && !q.IsEmpty())
            {
            }
        }
    }
}
