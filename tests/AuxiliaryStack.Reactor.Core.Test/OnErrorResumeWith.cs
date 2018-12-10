﻿using System;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class OnErrorResumeWithTest
    {
        [Fact]
        public void OnErrorResumeWith_Normal()
        {
            Flux.Range(1, 5).OnErrorResumeWith(e => Flux.Range(6, 5))
                .Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Fact]
        public void OnErrorResumeWith_Error()
        {
            Flux.Range(1, 5)
                .ConcatWith(Flux.Error<int>(new Exception()))
                .OnErrorResumeWith(e => Flux.Range(6, 5))
                .Test().AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void OnErrorResumeWith_Error_Backpressure()
        {
            var ts = Flux.Range(1, 5)
                .ConcatWith(Flux.Error<int>(new Exception()))
                .OnErrorResumeWith(e => Flux.Range(6, 5))
                .Test(4);

            ts.AssertValues(1, 2, 3, 4);

            ts.Request(4);

            ts.AssertValues(1, 2, 3, 4, 5, 6, 7, 8);

            ts.Request(2);

            ts.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }
    }
}
