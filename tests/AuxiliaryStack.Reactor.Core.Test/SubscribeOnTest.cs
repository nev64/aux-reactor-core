using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Scheduler;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class SubscribeOnTest
    {
        [Fact]
        public void SubscribeOn_Normal()
        {
            Flux.Range(1, 10).SubscribeOn(DefaultScheduler.Instance)
                .Test().AwaitTerminalEvent()
                .AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Fact]
        public void SubscribeOn_Scalar()
        {
            Flux.Just(1).SubscribeOn(DefaultScheduler.Instance)
                .Test().AwaitTerminalEvent()
                .AssertResult(1);
        }

        [Fact]
        public void SubscribeOn_Callable()
        {
            Flux.From(() => 1).SubscribeOn(DefaultScheduler.Instance)
                .Test().AwaitTerminalEvent()
                .AssertResult(1);
        }

        [Fact]
        public void SubscribeOn_ScalarFused()
        {
            Flux.Just(1).SubscribeOn(DefaultScheduler.Instance)
                .Test(fusionMode: FusionMode.Any)
                .AwaitTerminalEvent()
                .AssertFusionMode(FusionMode.Async)
                .AssertResult(1);
        }
    }
}
