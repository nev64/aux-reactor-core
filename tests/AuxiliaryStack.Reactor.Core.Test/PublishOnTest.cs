using AuxiliaryStack.Reactor.Core.Flow;
using AuxiliaryStack.Reactor.Core.Scheduler;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class PublishOnTest
    {
        [Fact]
        public void PublishOn_Normal()
        {
            Flux.Range(1, 1000000)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Normal_Double()
        {
            Flux.Range(1, 1000000)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Normal_Double_Fused()
        {
            Flux.Range(1, 1000000)
                .PublishOn(DefaultScheduler.Instance)
                .PublishOn(DefaultScheduler.Instance)
                .Test(fusionMode: FuseableHelper.ANY)
                .AwaitTerminalEvent()
                .AssertFusionMode(FuseableHelper.ASYNC)
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Normal_FusedIn()
        {
            Flux.Range(1, 1000000)
                .PublishOn(DefaultScheduler.Instance)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Conditional()
        {
            Flux.Range(1, 1000000)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Filter(v => true)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Conditional_Double()
        {
            Flux.Range(1, 1000000)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Hide()
                .PublishOn(DefaultScheduler.Instance)
                .Filter(v => true)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Conditional_Double_Fused()
        {
            Flux.Range(1, 1000000)
                .PublishOn(DefaultScheduler.Instance)
                .PublishOn(DefaultScheduler.Instance)
                .Filter(v => true)
                .Test(fusionMode: FuseableHelper.ANY)
                .AwaitTerminalEvent()
                .AssertFusionMode(FuseableHelper.ASYNC)
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }

        [Fact]
        public void PublishOn_Conditional_FusedIn()
        {
            Flux.Range(1, 1000000)
                .PublishOn(DefaultScheduler.Instance)
                .Filter(v => true)
                .Test()
                .AwaitTerminalEvent()
                .AssertValueCount(1000000)
                .AssertNoError()
                .AssertComplete();
        }
    }
}
