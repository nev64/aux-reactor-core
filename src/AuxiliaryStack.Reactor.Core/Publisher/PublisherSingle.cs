using System;
using AuxiliaryStack.Reactor.Core.Subscriber;


namespace AuxiliaryStack.Reactor.Core.Publisher
{
    sealed class PublisherSingle<T> : IFlux<T>, IMono<T>
    {
        readonly IPublisher<T> source;

        readonly bool allowEmpty;

        readonly bool hasDefault;

        readonly T defaultValue;

        internal PublisherSingle(IPublisher<T> source, bool allowEmpty, bool hasDefault, T defaultValue)
        {
            this.source = source;
            this.allowEmpty = allowEmpty;
            this.hasDefault = hasDefault;
            this.defaultValue = defaultValue;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            source.Subscribe(new SingleSubscriber(s, allowEmpty, hasDefault, defaultValue));
        }

        sealed class SingleSubscriber : DeferredScalarSubscriber<T, T>
        {
            readonly bool allowEmpty;
                
            readonly bool hasDefault;

            readonly T defaultValue;

            bool hasValue;

            bool done;

            public SingleSubscriber(ISubscriber<T> actual, bool allowEmpty, bool hasDefault, T defaultValue)
                : base(actual)
            {
                this.allowEmpty = allowEmpty;
                this.hasDefault = hasDefault;
                this.defaultValue = defaultValue;
            }

            protected override void OnStart()
            {
                s.Request(long.MaxValue);
            }

            public override void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;

                if (hasValue)
                {
                    Complete(value);
                }
                else
                {
                    if (hasDefault)
                    {
                        Complete(defaultValue);
                    }
                    else
                    {
                        if (allowEmpty)
                        {
                            Complete();
                        }
                        else
                        {
                            Error(new IndexOutOfRangeException("The source is empty."));
                        }
                    }
                }
            }

            public override void OnError(Exception e)
            {
                if (done)
                {
                    ExceptionHelper.OnErrorDropped(e);
                    return;
                }
                done = true;
                value = default(T);
                Error(e);
            }

            public override void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                if (!hasValue)
                {
                    hasValue = true;
                    value = t;
                }
                else
                {
                    done = true;
                    s.Cancel();
                    Error(new IndexOutOfRangeException("The source has more than one value."));
                }
            }
        }
    }
}
