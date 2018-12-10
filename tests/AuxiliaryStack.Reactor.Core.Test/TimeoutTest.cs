using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class TimeoutTest
    {
        [Fact]
        public void Timeout_Normal()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Test();

            source.OnNext(1);

            first.OnNext(1);

            source.OnNext(2, 3, 4);
            source.OnComplete();

            ts.AssertResult(1, 2, 3, 4);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }

        [Fact]
        public void Timeout_FirstTimesOut()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Test();

            first.OnNext(1);

            ts.AssertResult(100);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }

        [Fact]
        public void Timeout_SecondTimesOut()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Test();

            source.OnNext(1);

            item.OnNext(1);

            ts.AssertResult(1, 100);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }

        [Fact]
        public void Timeout_Conditional()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Filter(v => true)
                .Test();

            source.OnNext(1);

            first.OnNext(1);

            source.OnNext(2, 3, 4);
            source.OnComplete();

            ts.AssertResult(1, 2, 3, 4);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }

        [Fact]
        public void Timeout_Conditional_FirstTimesOut()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Filter(v => true)
                .Test();

            first.OnNext(1);

            ts.AssertResult(100);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }

        [Fact]
        public void Timeout_Conditional_SecondTimesOut()
        {
            var first = new DirectProcessor<int>();
            var item = new DirectProcessor<int>();
            var source = new DirectProcessor<int>();

            var ts = source.Timeout(first, v => item, Flux.Just(100))
                .Filter(v => true)
                .Test();

            source.OnNext(1);

            item.OnNext(1);

            ts.AssertResult(1, 100);

            Assert.False(first.HasSubscribers, "first has subscribers?!");
            Assert.False(item.HasSubscribers, "item has subscribers?!");
            Assert.False(source.HasSubscribers, "source has subscribers?!");
        }
    }
}
