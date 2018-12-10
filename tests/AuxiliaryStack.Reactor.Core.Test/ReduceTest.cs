using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ReduceTest
    {
        [Fact]
        public void Reduce_Normal()
        {
            Flux.Range(1, 10).Reduce((a, b) => a + b).Test().AssertResult(55);
        }

        [Fact]
        public void Reduce_InitialValue()
        {
            Flux.Range(1, 10).Reduce(10, (a, b) => a + b).Test().AssertResult(65);
        }

    }
}
