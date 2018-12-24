using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherAll<T> : IMono<bool>
    {
        private readonly IPublisher<T> _source;
        private readonly Func<T, bool> _predicate;

        public PublisherAll(IPublisher<T> source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public void Subscribe(ISubscriber<bool> subscriber) => 
            _source.Subscribe(new AllSubscriber(subscriber, _predicate));

        private sealed class AllSubscriber : DeferredScalarSubscriber<T, bool>
        {
            private readonly Func<T, bool> _predicate;
            private bool _isDone;

            public AllSubscriber(ISubscriber<bool> actual, Func<T, bool> predicate) : base(actual)
            {
                _predicate = predicate;
            }

            protected override void OnStart()
            {
                _subscription.Request(long.MaxValue);
            }


            public override void OnComplete()
            {
                if (_isDone)
                {
                    return;
                }
                Complete(true);
            }

            public override void OnError(Exception e)
            {
                if (_isDone)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                _isDone = true;
                Error(e);
            }

            public override void OnNext(T value)
            {
                if (_isDone)
                {
                    return;
                }

                bool b;

                try
                {
                    b = _predicate(value);
                }
                catch (Exception ex)
                {
                    _isDone = true;
                    Fail(ex);
                    return;
                }
                if (!b)
                {
                    _subscription.Cancel();
                    _isDone = true;
                    Complete(false);
                }
            }
        }
    }
}
