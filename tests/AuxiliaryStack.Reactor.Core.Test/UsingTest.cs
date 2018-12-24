using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class UsingTest
    {
        [Fact]
        public void Using_Normal()
        {
            Flux.Using(() => 1, s => Flux.Range(1, 5), s => { })
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void Using_Normal_Fused()
        {
            Flux.Using(() => 1, s => Flux.Range(1, 5), s => { })
                .Test(fusionMode: FusionMode.Any)
                .AssertFusionMode(FusionMode.Sync)
                .AssertResult(1, 2, 3, 4, 5);
        }
    }
}
