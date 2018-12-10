using System;
using System.Collections.Generic;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class FlatMapTest
    {
        [Fact]
        public void FlatMap_Normal()
        {
            Flux.Range(1, 5)
                .FlatMap(v => Flux.Range(v, 2))
                .Test().AssertResult(1, 2, 2, 3, 3, 4, 4, 5, 5, 6);
        }

        [Fact]
        public void FlatMap_Error()
        {
            Flux.Error<int>(new Exception("Forced failure"))
                .FlatMap(v => Flux.Just(1))
                .Test()
                .AssertNoValues().AssertErrorMessage("Forced failure").AssertNotComplete();
        }

        [Fact]
        public void FlatMap_Mono_Enumerable()
        {
            Mono.Just(1).FlatMap(v => new List<int>(new[] { 1, 2, 3, 4, 5 }))
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void FlatMap_Mono_Mono()
        {
            Mono.Just(1).FlatMap(v => Mono.Just(v + 1))
                .Test().AssertResult(2);
        }

        [Fact]
        public void FlatMap_Mono_Publisher()
        {
            Mono.Just(1).FlatMap(v => Flux.Range(v, 2))
                .Test().AssertResult(1, 2);
        }

    }
}
