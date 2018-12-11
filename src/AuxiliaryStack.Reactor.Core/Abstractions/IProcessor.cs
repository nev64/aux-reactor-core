

namespace AuxiliaryStack.Reactor.Core
{
    /// <summary>
    /// A Processor represents a processing stage—which is both a <see cref="ISubscriber{T}"/>
    /// and a <see cref="IPublisher{T}"/> and obeys the contracts of both.
    /// </summary>
    /// <typeparam name="T">The type of element signaled to the <see cref="ISubscriber{T}"/> and to the <see cref="IPublisher{T}"/> side</typeparam>
    public interface IProcessor<T> : IProcessor<T, T>
    {

    }

    public interface IProcessor<in TSub, out TPub> : ISubscriber<TSub>, IPublisher<TPub>
    {
        
    }
}
