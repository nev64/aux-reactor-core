using System.Threading.Tasks;
using AuxiliaryStack.Monads;
using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test.Publisher
{
    public class FromTests
    {
        [Fact]
        public void Mono_From_Task()
        {
            var subscriber = Mono.From(new Task(() => { })).Test();
            subscriber.AssertValues(Unit.Instance);
            
            subscriber.AssertNoError();
            subscriber.AssertComplete();
        }
    }
}