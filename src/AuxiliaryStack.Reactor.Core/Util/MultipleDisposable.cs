using System;

namespace AuxiliaryStack.Reactor.Core.Util
{
    /// <summary>
    /// An IDisposable holding onto another IDisposable and allows
    /// replacing it atomically, optionally disposing the previous one.
    /// </summary>
    internal sealed class MultipleDisposable : IDisposable
    {
        IDisposable d;

        internal MultipleDisposable()
        {
        }

        internal MultipleDisposable(IDisposable d)
        {
            this.d = d;
        }

        public void Dispose()
        {
            DisposableHelper.Dispose(ref d);
        }

        public bool Replace(IDisposable next)
        {
            return DisposableHelper.Replace(ref d, next);
        }

        public bool Set(IDisposable next)
        {
            return DisposableHelper.Set(ref d, next);
        }
    }
}
