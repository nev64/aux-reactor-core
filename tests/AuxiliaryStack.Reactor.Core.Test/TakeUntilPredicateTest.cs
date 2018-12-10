using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class TakeUntilPredicateTest
    {
        [Fact]
        public void TakeUntilPredicate_Normal()
        {
            Flux.Range(1, 10).TakeUntil(v => v == 5)
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeUntilPredicate_Conditional()
        {
            Flux.Range(1, 10).TakeUntil(v => v == 5)
                .Filter(v => true)
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeUntilPredicate_Normal_Fused()
        {
            Flux.Range(1, 10).TakeUntil(v => v == 5)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void TakeUntilPredicate_Conditional_Fused()
        {
            Flux.Range(1, 10).TakeUntil(v => v == 5)
                .Filter(v => true)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.SYNC)
                .AssertResult(1, 2, 3, 4, 5);
        }

    }
}
