using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherScan<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly Func<T, T, T> scanner;

        internal PublisherScan(IPublisher<T> source, Func<T, T, T> scanner)
        {
            this.source = source;
            this.scanner = scanner;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new ScanSubscriber(s, scanner));
        }

        sealed class ScanSubscriber : BasicSubscriber<T, T>
        {
            readonly Func<T, T, T> scanner;

            T value;

            bool hasValue;

            public ScanSubscriber(ISubscriber<T> actual, Func<T, T, T> scanner) : base(actual)
            {
                this.scanner = scanner;
            }

            public override void OnComplete()
            {
                _actual.OnComplete();
            }

            public override void OnError(Exception e)
            {
                value = default(T);
                _actual.OnError(e);
            }

            public override void OnNext(T t)
            {
                if (!hasValue)
                {
                    hasValue = true;
                    value = t;
                    _actual.OnNext(t);
                }
                else
                {
                    T v;
                    try
                    {
                        v = scanner(value, t);
                        value = v;
                    }
                    catch (Exception ex)
                    {
                        Fail(ex);
                        return;
                    }

                    _actual.OnNext(v);
                }
            }
        }
    }
}
