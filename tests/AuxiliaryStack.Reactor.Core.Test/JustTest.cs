using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class JustTest
    {
        [Fact]
        public void Just_Normal()
        {
            Assert.Equal(1, Flux.Just(1).BlockLast());
        }
    }
}
