using System;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class SingleTest
    {
        [Fact]
        public void Single_Single()
        {
            Flux.Just(1).Single().Test().AssertResult(1);
        }

        [Fact]
        public void Single_Empty_Default()
        {
            Flux.Empty<int>().Single(2).Test().AssertResult(2);
        }

        [Fact]
        public void Single_OrEmpty_Empty()
        {
            Flux.Empty<int>().SingleOrEmpty().Test().AssertResult();
        }

        [Fact]
        public void Single_OrEmpty_Single()
        {
            Flux.Just(3).SingleOrEmpty().Test().AssertResult(3);
        }

        [Fact]
        public void Single_Empty()
        {
            Flux.Empty<int>().Single().Test()
                .AssertNoValues()
                .AssertError(e => e is IndexOutOfRangeException)
                .AssertNotComplete();
        }

        [Fact]
        public void Single_Many()
        {
            Flux.Range(1, 10).Single().Test()
                .AssertNoValues()
                .AssertError(e => e is IndexOutOfRangeException)
                .AssertNotComplete();
        }
    }
}
