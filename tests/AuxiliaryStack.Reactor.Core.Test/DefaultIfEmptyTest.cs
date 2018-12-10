﻿using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class DefaultIfEmptyTest
    {
        [Fact]
        public void DefaultIfEmpty_Non_Empty()
        {
            Flux.Just(1).DefaultIfEmpty(0).Test().AssertResult(1);
        }

        [Fact]
        public void DefaultIfEmpty_Empty()
        {
            Flux.Empty<int>().DefaultIfEmpty(0).Test().AssertResult(0);
        }

        [Fact]
        public void DefaultIfEmpty_Empty_Backpressured()
        {
            var ts = Flux.Empty<int>().DefaultIfEmpty(0).Test(0);

            ts.AssertNoValues();

            ts.Request(1);

            ts.AssertResult(0);
        }

        [Fact]
        public void DefaultIfEmpty_Non_Empty_Backpressured()
        {
            var ts = Flux.Range(1, 5).DefaultIfEmpty(0).Test(0);

            ts.AssertNoValues();

            ts.Request(1);

            ts.AssertValues(1);

            ts.Request(2);

            ts.AssertValues(1, 2, 3);

            ts.Request(2);

            ts.AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void DefaultIfEmpty_Non_Empty_Conditional()
        {
            Flux.Just(1).DefaultIfEmpty(0).Filter(v => true).Test().AssertResult(1);
        }

        [Fact]
        public void DefaultIfEmpty_Empty_Conditional()
        {
            Flux.Empty<int>().DefaultIfEmpty(0).Filter(v => true).Test().AssertResult(0);
        }

        [Fact]
        public void DefaultIfEmpty_Empty_Backpressured_Conditional()
        {
            var ts = Flux.Empty<int>().DefaultIfEmpty(0).Filter(v => true).Test(0);

            ts.AssertNoValues();

            ts.Request(1);

            ts.AssertResult(0);
        }

        [Fact]
        public void DefaultIfEmpty_Non_Empty_Backpressured_Conditional()
        {
            var ts = Flux.Range(1, 5).DefaultIfEmpty(0).Filter(v => true).Test(0);

            ts.AssertNoValues();

            ts.Request(1);

            ts.AssertValues(1);

            ts.Request(2);

            ts.AssertValues(1, 2, 3);

            ts.Request(2);

            ts.AssertResult(1, 2, 3, 4, 5);
        }
    }
}
