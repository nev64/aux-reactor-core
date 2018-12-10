﻿using System.Collections.Generic;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class GroupByTest
    {
        [Fact]
        public void GroupBy_Normal()
        {
            Flux.Range(1, 10).GroupBy(k => k & 1)
                .FlatMap(g => g.CollectList())
                .Test()
                .AssertResult(
                    new List<int>(new [] { 1, 3, 5, 7, 9 }),
                    new List<int>(new[] { 2, 4, 6, 8, 10 })
                );
        }
        [Fact]
        public void GroupBy_2_of_3_Groups()
        {
            Flux.Range(1, 10).GroupBy(k => k % 3)
                .Take(2)
                .FlatMap(g => g.CollectList())
                .Test()
                .AssertResult(
                    new List<int>(new[] { 1, 4, 7, 10 }),
                    new List<int>(new[] { 2, 5, 8 })
                );
        }
    }
}
