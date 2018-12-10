using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Utility methods to help with schedulers.
    /// </summary>
    public static class SchedulerHelper
    {
        private static readonly RejectedDisposable SRejected = new RejectedDisposable();

        private static readonly DateTimeOffset SEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// The singleton instance of a rejected IDisposable indicator.
        /// </summary>
        public static IDisposable Rejected => SRejected;

        /// <summary>
        /// Converts the DateTimeOffset into total number of milliseconds since the epoch.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long UtcMillis(this DateTimeOffset dt)
        {
            return (long)(dt - SEpoch).TotalMilliseconds;
        }
    }
}