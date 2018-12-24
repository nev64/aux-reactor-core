using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ThenTest
    {
        [Fact]
        public void Then_EmptyVoid()
        {
            Flux.Range(1, 10).Then(Mono.Empty<Void>())
                .Test().AssertResult();
        }

        [Fact]
        public void Then_EmptyVoid_Fused()
        {
            Flux.Range(1, 10).Then(Mono.Empty<Void>())
                .Test(fusionMode: FusionMode.Any)
                .AssertFusionMode(FusionMode.Async)
                .AssertResult();
        }
    }
}
