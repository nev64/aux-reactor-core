using System;
using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Subscription;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherFunc<T> : IFlux<T>, IMono<T>, ICallable<T>
    {
        readonly Func<T> supplier;

        readonly bool nullMeansEmpty;

        public T Value
        {
            get
            {
                return supplier();
            }
        }

        public PublisherFunc(Func<T> supplier, bool nullMeansEmpty)
        {
            this.supplier = supplier;
            this.nullMeansEmpty = nullMeansEmpty;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            var parent = new FuncSubscription(s);
            s.OnSubscribe(parent);

            T v;
            try
            {
                v = supplier();
            }
            catch (Exception ex)
            {
                ExceptionHelper.ThrowIfFatal(ex);
                parent.Error(ex);
                return;
            }

            if (nullMeansEmpty && v == null)
            {
                s.OnComplete();
                return;
            }

            parent.Complete(v);
        }

        sealed class FuncSubscription : DeferredScalarSubscription<T>
        {
            public FuncSubscription(ISubscriber<T> actual) : base(actual)
            {
            }
        }
    }
}
