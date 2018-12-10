using System.Collections.Generic;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class BufferBoundaryTest
    {
        [Fact]
        public void BufferBoundary_Normal()
        {
            var other = new DirectProcessor<int>();

            var main = new DirectProcessor<int>();

            var ts = main.Buffer(other).Test();

            main.OnNext(1, 2, 3);

            other.OnNext(1);

            main.OnNext(4, 5);

            other.OnNext(2, 3);

            main.OnNext(6);
            main.OnComplete();

            Assert.False(main.HasSubscribers);
            Assert.False(other.HasSubscribers);

            ts.AssertResult(
                new List<int>(new[] { 1, 2, 3 }),
                new List<int>(new[] { 4, 5, }),
                new List<int>(new[] { 6 })
            );
        }

        [Fact]
        public void BufferBoundary_Other_Completes_Immediately()
        {
            var main = new DirectProcessor<int>();

            var ts = main.Buffer(Flux.Empty<int>()).Test();

            ts.AssertSubscribed()
                .AssertResult();
        }
    }
}
