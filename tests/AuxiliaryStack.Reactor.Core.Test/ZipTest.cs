using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class ZipTest
    {
        [Fact]
        public void Zip_Normal()
        {
            Flux.Zip(Flux.Range(1, 2).Hide(), Flux.Range(1, 2).Hide(), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_First_Shorter()
        {
            Flux.Zip(Flux.Range(1, 2).Hide(), Flux.Range(1, 3).Hide(), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_Second_Shorter()
        {
            Flux.Zip(Flux.Range(1, 3).Hide(), Flux.Range(1, 2).Hide(), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_First_Empty()
        {
            Flux.Zip(Flux.Empty<int>().Hide(), Flux.Range(1, 3).Hide(), (a, b) => a * 10 + b)
                .Test().AssertResult();
        }

        [Fact]
        public void Zip_Second_Empty()
        {
            Flux.Zip(Flux.Range(1, 2).Hide(), Flux.Empty<int>().Hide(), (a, b) => a * 10 + b)
                .Test().AssertResult();
        }

        [Fact]
        public void Zip_Normal_Fused()
        {
            Flux.Zip(Flux.Range(1, 2), Flux.Range(1, 2), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_First_Shorter_Fused()
        {
            Flux.Zip(Flux.Range(1, 2), Flux.Range(1, 3), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_Second_Shorter_Fused()
        {
            Flux.Zip(Flux.Range(1, 3), Flux.Range(1, 2), (a, b) => a * 10 + b)
                .Test().AssertResult(11, 22);
        }

        [Fact]
        public void Zip_First_Empty_Fused()
        {
            Flux.Zip(Flux.Empty<int>(), Flux.Range(1, 3), (a, b) => a * 10 + b)
                .Test().AssertResult();
        }

        [Fact]
        public void Zip_Second_Empty_Fused()
        {
            Flux.Zip(Flux.Range(1, 2), Flux.Empty<int>(), (a, b) => a * 10 + b)
                .Test().AssertResult();
        }

    }
}
