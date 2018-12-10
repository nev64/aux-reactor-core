﻿using Xunit;

namespace AuxiliaryStack.Reactor.Core.Test
{
    public class MapNotificationTest
    {
        [Fact]
        public void MapNotification_Normal()
        {
            Flux.Range(1, 3).FlatMap(t => Flux.Range(t, 2), t => Flux.Just(100), () => Flux.Just(50))
                .Test().AssertResult(1, 2, 2, 3, 3, 4, 50);
        }
    }
}
