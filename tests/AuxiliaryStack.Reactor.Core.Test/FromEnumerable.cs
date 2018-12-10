using System.Collections.Generic;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class FromEnumerableTest
    {
        [Fact]
        public void FromEnumerable_Normal()
        {
            IEnumerable<int> en = new List<int>(new int[] { 1, 2, 3, 4, 5 });
            Flux.From(en)
                .Test().AssertResult(1, 2, 3, 4, 5);
        }
    }
}
