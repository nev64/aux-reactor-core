using System;
using AuxiliaryStack.Reactor.Core.Flow;

namespace AuxiliaryStack.Reactor.Core.Publisher
{
    internal sealed class MonoPublishOn<T> : IMono<T>
    {
        private readonly IMono<T> _source;
        private readonly IScheduler _scheduler;

        internal MonoPublishOn(IMono<T> source, IScheduler scheduler)
        {
            _source = source;
            _scheduler = scheduler;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            _source.Subscribe(new PublishOnSubscriber(s, _scheduler));
        }

        private sealed class PublishOnSubscriber : ISubscriber<T>, IQueueSubscription<T>
        {
            private readonly ISubscriber<T> _actual;
            private readonly IScheduler _scheduler;
            private ISubscription _subscription;
            private bool _hasValue;
            private bool _isValueTaken;
            private T _value;
            
            internal PublishOnSubscriber(ISubscriber<T> actual, IScheduler scheduler)
            {
                _actual = actual;
                _scheduler = scheduler;
            }

            public void Cancel() => _subscription.Cancel();

            public void OnComplete()
            {
                if (!_hasValue)
                {
                    _scheduler.Schedule(() => _actual.OnComplete());
                }
            }

            public void OnError(Exception e) => _scheduler.Schedule(() => _actual.OnError(e));

            public void OnNext(T t)
            {
                _hasValue = true;
                _value = t;
                _scheduler.Schedule(() =>
                {
                    _actual.OnNext(_value);
                    _actual.OnComplete();
                });
            }

            public void OnSubscribe(ISubscription subscription)
            {
                _subscription = subscription;
                _actual.OnSubscribe(this);
            }

            public void Request(long n) => _subscription.Request(n);

            public int RequestFusion(int mode) => mode & FuseableHelper.ASYNC;

            public bool Offer(T value) => FuseableHelper.DontCallOffer();

            public bool Poll(out T value)
            {
                if (!_isValueTaken)
                {
                    _isValueTaken = true;
                    value = _value;
                    return true;
                }
                value = default;
                return false;
            }

            public bool IsEmpty() => !_hasValue || _isValueTaken;

            public void Clear()
            {
                _hasValue = false;
                _value = default;
            }
        }
    }
}
