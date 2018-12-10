using System;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class FilterTest
    {
        [Fact]
        public void Filter_Normal()
        {
            Flux.Range(1, 5).Filter(v => (v & 1) == 0).Test().AssertResult(2, 4);
        }

        [Fact]
        public void Filter_Error()
        {
            Flux.Error<int>(new Exception("Forced failure"))
                .Filter(v => (v & 1) == 0).Test()
                .AssertNoValues().AssertErrorMessage("Forced failure").AssertNotComplete();
        }
    }
}
