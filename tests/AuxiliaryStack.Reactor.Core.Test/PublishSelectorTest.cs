﻿using System;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class PublishSelectorTest
    {
        [Fact]
        public void PublishSelector_Normal()
        {
            Flux.Range(1, 10).Publish(o => Flux.Zip(o, o.Skip(1), (a, b) => new Tuple<int, int>(a, b)))
                .Test()
                .AssertResult(
                    new Tuple<int, int>(1, 2),
                    new Tuple<int, int>(2, 3),
                    new Tuple<int, int>(3, 4),
                    new Tuple<int, int>(4, 5),
                    new Tuple<int, int>(5, 6),
                    new Tuple<int, int>(6, 7),
                    new Tuple<int, int>(7, 8),
                    new Tuple<int, int>(8, 9),
                    new Tuple<int, int>(9, 10)
                );
        }
        [Fact]
        public void PublishSelector_Unrelated()
        {
            var dp = new DirectProcessor<int>();

            var ts = dp.Publish(o => Flux.Range(1, 10)).Test(0);

            Assert.True(dp.HasSubscribers, "No subscribers?");

            ts.Request(10);

            ts.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.False(dp.HasSubscribers, "Has subscribers?");
        }
    }
}
