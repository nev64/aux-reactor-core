using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class MaterializeTest
    {
        [Fact]
        public void Materialize_Normal()
        {
            Flux.Range(1, 5).Materialize()
                .Test().AssertValueCount(6).AssertComplete();
        }

        [Fact]
        public void Materialize_Dematerialize()
        {
            Flux.Range(1, 5).Materialize().Dematerialize()
                .Test().AssertResult(1, 2, 3, 4, 5);
        }
    }
}
