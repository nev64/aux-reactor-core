using Reactive.Streams;

namespace AuxiliaryStack.Reactor.Core.Test.Tck
{
    class ConcatTest : FluxPublisherVerification<int>
    {
        public override IPublisher<int> CreatePublisher(long elements)
            => Flux.From(Enumerate(elements/2)).ConcatWith(Flux.From(Enumerate((elements + 1)/2))).Tck();
    }
}
