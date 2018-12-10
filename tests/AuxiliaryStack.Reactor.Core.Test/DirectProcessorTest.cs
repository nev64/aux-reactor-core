using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class DirectProcessorTest
    {
        [Fact]
        public void DirectProcessor_Normal()
        {
            DirectProcessor<int> dp = new DirectProcessor<int>();

            var ts = dp.Test();

            ts.AssertSubscribed()
                .AssertNoEvents();

            dp.OnNext(1);
            dp.OnNext(2);
            dp.OnComplete();

            Assert.False(dp.HasSubscribers);

            ts.AssertResult(1, 2);
        }
    }
}
