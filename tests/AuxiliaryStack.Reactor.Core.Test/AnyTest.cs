using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class AnyTest
    {
        [Fact]
        public void Any_Normal()
        {
            Flux.Range(1, 5).Any(v => v == 3).Test().AssertResult(true);
        }

        [Fact]
        public void Any_Empty()
        {
            Flux.Empty<int>().Any(v => v == 3).Test().AssertResult(false);
        }

        [Fact]
        public void Any_Normal_2()
        {
            Flux.Range(1, 5).Any(v => v == 7).Test().AssertResult(false);
        }
    }
}
