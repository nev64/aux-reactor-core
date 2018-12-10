using System;

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// The class representing a rejected task's IDisposable
    /// </summary>
    sealed class RejectedDisposable : IDisposable
    {
        public void Dispose()
        {
            // ignored
        }
    }
}