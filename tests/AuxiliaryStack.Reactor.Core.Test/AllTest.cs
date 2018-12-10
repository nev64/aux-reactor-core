using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class AllTest
    {
        [Fact]
        public void All_Normal()
        {
            Flux.Range(1, 5).All(v => v < 6).Test().AssertResult(true);
        }

        [Fact]
        public void All_Empty()
        {
            Flux.Empty<int>().All(v => v == 3).Test().AssertResult(true);
        }

        [Fact]
        public void All_Normal_2()
        {
            Flux.Range(1, 5).All(v => v < 5).Test().AssertResult(false);
        }
    }
}
