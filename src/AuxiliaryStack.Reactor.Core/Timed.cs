﻿namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// Structure holding a value and an Utc timestamp or time interval.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    public struct Timed<T>
    {
        /// <summary>
        /// The held value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// The held timestamp
        /// </summary>
        public long TimeMillis { get; private set; }

        /// <summary>
        /// Initializes the Timed instance.
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="timeMillis">The time in milliseconds</param>
        public Timed(T value, long timeMillis)
        {
            Value = value;
            TimeMillis = timeMillis;
        }
    }
}
