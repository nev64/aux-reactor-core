using System.Collections.Generic;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ZipEnumerableTest
    {

        static IEnumerable<int> Range(int start, int count)
        {
            for (int i = start; i != start + count; i++)
            {
                yield return i;
            }
            //yield break;
        }

        [Fact]
        public void ZipEnumerable_Normal()
        {
            Flux.Range(1, 5).ZipWith(Range(1, 5), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22, 33, 44, 55);
        }

        [Fact]
        public void ZipEnumerable_Main_Sorter()
        {
            Flux.Range(1, 5).ZipWith(Range(1, 6), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22, 33, 44, 55);
        }

        [Fact]
        public void ZipEnumerable_Other_Sorter()
        {
            Flux.Range(1, 5).ZipWith(Range(1, 4), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22, 33, 44);
        }

    }
}
