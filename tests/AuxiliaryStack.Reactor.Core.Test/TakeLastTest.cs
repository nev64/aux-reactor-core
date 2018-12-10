using AuxiliaryStack.Reactor.Core.Flow;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class TakeLastTest
    {
        [Fact]
        public void TakeLast_Longer()
        {
            Flux.Range(1, 10).TakeLast(15)
                .Test().AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }


        [Fact]
        public void TakeLast_Longer_Backpressured()
        {
            var ts = Flux.Range(1, 10).TakeLast(15)
                .Test(0);

            ts.AssertNoEvents();

            ts.Request(2);

            ts.AssertValues(1, 2);

            ts.Request(5);

            ts.AssertValues(1, 2, 3, 4, 5, 6, 7);

            ts.Request(3);

            ts.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void TakeLast_Shorter()
        {
            Flux.Range(1, 10).TakeLast(5)
                .Test().AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public void TakeLast_Longer_Conditional()
        {
            Flux.Range(1, 10).TakeLast(15)
                .Filter(v => true)
                .Test().AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void TakeLast_Shorter_Conditional()
        {
            Flux.Range(1, 10).TakeLast(5)
                .Filter(v => true)
                .Test().AssertResult(6, 7, 8, 9, 10);
        }

        [Fact]
        public void TakeLast_Single()
        {
            Flux.Range(1, 10).TakeLast(1)
                .Test().AssertResult(10);
        }

        [Fact]
        public void TakeLast_Single_Fused()
        {
            Flux.Range(1, 10).TakeLast(1)
                .Test(fusionMode: FuseableHelper.ANY)
                .AssertFusionMode(FuseableHelper.ASYNC)
                .AssertResult(10);
        }
    }
}
