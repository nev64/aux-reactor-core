using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;
using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherCallableXMap<T, R>
    {
        /// <summary>
        /// Applies shortcuts if the source is the empty instance or an ICallable.
        /// </summary>
        /// <param name="source">The source IPublisher.</param>
        /// <param name="s">The ISubscriber</param>
        /// <param name="mapper">The function that takes a source value and maps it into an IPublisher.</param>
        /// <returns>True if the optimizations were applied.</returns>
        internal static bool CallableXMap(IPublisher<T> source, ISubscriber<R> s, Func<T, IPublisher<R>> mapper)
        {
            if (source == PublisherEmpty<T>.Instance)
            {
                EmptySubscription<R>.Complete(s);
                return true;
            }
            if (source is ICallable<T>)
            {
                T t;

                try
                {
                    t = (source as ICallable<T>).Value;
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);

                    EmptySubscription<R>.Error(s, ex);
                    return true;
                }

                IPublisher<R> p;

                try
                {
                    p = mapper(t);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.ThrowIfFatal(ex);

                    EmptySubscription<R>.Error(s, ex);
                    return true;
                }
                
                if (p == null)
                {
                    EmptySubscription<R>.Error(s, new NullReferenceException("The mapper returned a null IPublisher"));
                    return true;
                }

                if (p == PublisherEmpty<R>.Instance)
                {
                    EmptySubscription<R>.Complete(s);
                    return true;
                }

                if (p is ICallable<R>)
                {
                    R r;

                    try
                    {
                        r = (p as ICallable<R>).Value;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.ThrowIfFatal(ex);

                        EmptySubscription<R>.Error(s, ex);
                        return true;
                    }

                    s.OnSubscribe(new ScalarSubscription<R>(s, r));
                    return true;
                }

                p.Subscribe(s);

                return true;
            }

            return false;
        }

        
    }
}
