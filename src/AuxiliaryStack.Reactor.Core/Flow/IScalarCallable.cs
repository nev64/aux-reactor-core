namespace AuxiliaryStack.Reactor.Core.Flow
{
    /// <summary>
    /// Indicates an IPublisher holds a single, constant value that can
    /// be retrieved at assembly time.
    /// </summary>
    /// <typeparam name="T">The returned value type.</typeparam>
    public interface IScalarCallable<T> : ICallable<T>
    {

    }
}