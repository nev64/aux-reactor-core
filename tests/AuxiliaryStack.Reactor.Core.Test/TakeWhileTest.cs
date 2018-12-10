using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class TakeWhileTest
    {
        [Fact]
        public void TakeWhile_Normal()
        {
            Flux.Range(1, 10).TakeWhile(v => v <= 5)
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeWhile_Conditional()
        {
            Flux.Range(1, 10).TakeWhile(v => v <= 5)
                .Filter(v => true)
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeWhile_Normal_Fused()
        {
            Flux.Range(1, 10).TakeWhile(v => v <= 5)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeWhile_Conditional_Fused()
        {
            Flux.Range(1, 10).TakeWhile(v => v <= 5)
                .Filter(v => true)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(1, 2, 3, 4, 5);
        }

    }
}
