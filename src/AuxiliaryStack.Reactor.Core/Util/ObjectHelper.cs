using System;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// Object-helper methods.
    /// </summary>
    internal static class ObjectHelper
    {
        /// <summary>
        /// Throws an Exception if the value is null, returns it otherwise.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The value.</returns>
        internal static T RequireNonNull<T>(T value, string errorMessage) where T : class
        {
            if (value == null)
            {
                throw new NullReferenceException(errorMessage);
            }
            return value;
        }
    }
}
