using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Tests.Events
{
    [TestFixture]
    public class GameEventBusTests
    {
        [Test]
        public void TriggerCharacterDied_NotifiesSubscribersInSubscriptionOrder()
        {
            var bus = new GameEventBus();
            var order = new List<string>();
            bus.OnCharacterDied += (c, cause) => order.Add("first");
            bus.OnCharacterDied += (c, cause) => order.Add("second");

            bus.TriggerCharacterDied(new CharacterData(), DeathCause.OldAge);

            CollectionAssert.AreEqual(new[] { "first", "second" }, order);
        }

        [Test]
        public void Buses_AreIndependent()
        {
            var heard = false;
            new GameEventBus().OnYearStarted += y => heard = true;
            new GameEventBus().TriggerYearStarted(2);
            Assert.IsFalse(heard);
        }
    }
}
