using System;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    public class DelegateInvocationException : Exception
    {
        public DelegateInvocationException(Exception inner, int index) 
            : base("Delegate threw exception.", inner)
        {
            Index = index;
        }

        public int Index { get; }
    }
}