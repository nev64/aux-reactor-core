using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class OnBackpressureLatestTest
    {
        [Fact]
        public void OnBackpressureLatest_Normal()
        {
            var ps = new DirectProcessor<int>();

            var ts = ps.OnBackpressureLatest().Test(0);

            ps.OnNext(1);
            ps.OnNext(2);

            ts.Request(1);

            ps.OnNext(3);

            ts.Request(2);

            ps.OnNext(4);
            ps.OnComplete();

            ts.AssertResult(2, 3, 4);
        }
    }
}
