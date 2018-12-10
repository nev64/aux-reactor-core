﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ToEnumerableTest
    {
        [Fact]
        public void ToEnumerable_Normal()
        {
            var ie = Flux.Range(1, 5).Hide().ToEnumerable();

            List<int> expected = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                expected.Add(i);
            }

            List<int> list = new List<int>();

            foreach (var i in ie)
            {
                list.Add(i);
            }

            Assert.Equal(5, list.Count);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void ToEnumerable_Normal_Sync_Fused()
        {
            var ie = Flux.Range(1, 5).ToEnumerable();

            List<int> expected = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                expected.Add(i);
            }

            List<int> list = new List<int>();

            foreach (var i in ie)
            {
                list.Add(i);
            }

            Assert.Equal(5, list.Count);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void ToEnumerable_Normal_Async_Fused_Online()
        {
            var up = new UnicastProcessor<int>();

            var ie = up.ToEnumerable();

            Task.Run(() =>
            {
                up.OnNext(1, 2, 3, 4, 5);
                up.OnComplete();
            });



            List<int> expected = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                expected.Add(i);
            }

            List<int> list = new List<int>();

            foreach (var i in ie)
            {
                list.Add(i);
            }

            Assert.Equal(5, list.Count);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void ToEnumerable_Normal_Sync_Fused_Offline()
        {
            var up = new UnicastProcessor<int>();

            List<int> expected = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                expected.Add(i);
                up.OnNext(i);
            }
            up.OnComplete();

            var ie = up.ToEnumerable();

            List<int> list = new List<int>();

            foreach (var i in ie)
            {
                list.Add(i);
            }

            Assert.Equal(5, list.Count);
            Assert.Equal(expected, list);
        }
    }
}
