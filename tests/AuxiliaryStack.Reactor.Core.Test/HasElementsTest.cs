using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class HasElementsTest
    {
        [Fact]
        public void HasElements_Normal()
        {
            Flux.Range(1, 5).HasElements().Test().AssertResult(true);
        }

        [Fact]
        public void HasElements_Empty()
        {
            Flux.Empty<int>().HasElements().Test().AssertResult(false);
        }
    }
}
