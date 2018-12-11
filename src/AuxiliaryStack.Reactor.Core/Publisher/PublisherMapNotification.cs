using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Util;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherMapNotification<T, R> : IFlux<IPublisher<R>>
    {
        readonly IPublisher<T> source;

        readonly Func<T, IPublisher<R>> onNextMapper;

        readonly Func<Exception, IPublisher<R>> onErrorMapper;

        readonly Func<IPublisher<R>> onCompleteMapper;

        public PublisherMapNotification(IPublisher<T> source,
            Func<T, IPublisher<R>> onNextMapper,
            Func<Exception, IPublisher<R>> onErrorMapper,
            Func<IPublisher<R>> onCompleteMapper)
        {
            this.source = source;
            this.onNextMapper = onNextMapper;
            this.onErrorMapper = onErrorMapper;
            this.onCompleteMapper = onCompleteMapper;
        }

        public void Subscribe(ISubscriber<IPublisher<R>> s)
        {
            source.Subscribe(new MapNotificationSubscriber(s, onNextMapper, onErrorMapper, onCompleteMapper));
        }

        sealed class MapNotificationSubscriber : BasicSinglePostCompleteSubscriber<T, IPublisher<R>>
        {
            readonly Func<T, IPublisher<R>> onNextMapper;

            readonly Func<Exception, IPublisher<R>> onErrorMapper;

            readonly Func<IPublisher<R>> onCompleteMapper;

            internal MapNotificationSubscriber(
                ISubscriber<IPublisher<R>> actual,
                Func<T, IPublisher<R>> onNextMapper,
                Func<Exception, IPublisher<R>> onErrorMapper,
                Func<IPublisher<R>> onCompleteMapper) : base(actual)
            {
                this.onNextMapper = onNextMapper;
                this.onErrorMapper = onErrorMapper;
                this.onCompleteMapper = onCompleteMapper;
            }

            public override void OnNext(T t)
            {
                _produced++;

                IPublisher<R> p;

                try
                {
                    p = ObjectHelper.RequireNonNull(onNextMapper(t), "The onNextMapper returned a null IPublisher");
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }

                _actual.OnNext(p);
            }

            public override void OnError(Exception e)
            {
                IPublisher<R> last;
                try
                {
                    last = ObjectHelper.RequireNonNull(onErrorMapper(e), "The onErrorMapper returned a null IPublisher");
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }

                Complete(last);
            }

            public override void OnComplete()
            {
                IPublisher<R> last;
                try
                {
                    last = ObjectHelper.RequireNonNull(onCompleteMapper(), "The onCompleteMapper returned a null IPublisher");
                }
                catch (Exception ex)
                {
                    Fail(ex);
                    return;
                }

                Complete(last);
            }
        }
    }
}
