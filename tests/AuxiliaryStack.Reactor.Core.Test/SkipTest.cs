using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class SkipTest
    {
        [Fact]
        public void Skip_Normal()
        {
            Flux.Range(1, 5).Skip(3).Test().AssertResult(4, 5);
        }
    }
}
