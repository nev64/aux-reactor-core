using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class SkipWhileTest
    {
        [Fact]
        public void SkipWhile_Normal()
        {
            Flux.Range(1, 10).SkipWhile(v => v < 6)
                .Test().AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public void SkipWhile_Conditional()
        {
            Flux.Range(1, 10).SkipWhile(v => v < 6)
                .Filter(v => true)
                .Test().AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public void SkipWhile_Normal_Fused()
        {
            Flux.Range(1, 10).SkipWhile(v => v < 6)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public void SkipWhile_Conditional_Fused()
        {
            Flux.Range(1, 10).SkipWhile(v => v < 6)
                .Filter(v => true)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(6, 7, 8, 9, 10);
        }

    }
}
