using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ProcessTest
    {
        [Fact]
        public void Process_DirectProcessor()
        {

            var co = Flux.Range(1, 10).Process(() => new DirectProcessor<int>(), o => o.Map(v => v + 1));
            var ts = co.Test();

            co.Connect();

            ts.AssertResult(2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        }
    }
}
