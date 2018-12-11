using System;
using AuxiliaryStack.Reactor.Core.Subscriber;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherScanWith<T, A> : IFlux<A>, IMono<A>
    {
        readonly IPublisher<T> source;

        readonly Func<A> initialSupplier;

        readonly Func<A, T, A> scanner;

        internal PublisherScanWith(IPublisher<T> source, Func<A> initialSupplier, Func<A, T, A> scanner)
        {
            this.source = source;
            this.initialSupplier = initialSupplier;
            this.scanner = scanner;
        }

        public void Subscribe(ISubscriber<A> s)
        {
            A accumulator;

            try
            {
                accumulator = initialSupplier();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                EmptySubscription<A>.Error(s, ex);
                return;
            }
            source.Subscribe(new ScanWithSubscriber(s, accumulator, scanner));
        }

        sealed class ScanWithSubscriber : BasicSinglePostCompleteSubscriber<T, A>
        {
            readonly Func<A, T, A> scanner;

            A value;

            public ScanWithSubscriber(ISubscriber<A> actual, A initial, Func<A, T, A> scanner) : base(actual)
            {
                this.scanner = scanner;
                this.value = initial;
            }

            public override void OnComplete()
            {
                Complete(value);
            }

            public override void OnError(Exception e)
            {
                value = default(A);
                _actual.OnError(e);
            }

            public override void OnNext(T t)
            {
                _produced++;

                var v = value;
                _actual.OnNext(v);

                try
                {
                    value = scanner(v, t);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }
        }
    }
}
