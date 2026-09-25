using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Everyone ages each new year; each dies when their own lifespan runs out.</summary>
    [TestFixture]
    public class AgingSystemTests
    {
        private GameContext ctx;
        private ClanManager clan;
        private AgingSystem aging;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            clan = new ClanManager(ctx, "Mo");
            aging = new AgingSystem(ctx, clan);
        }

        [Test]
        public void AgeOneYear_AgesEveryLivingMember()
        {
            var c = Fixtures.Cultivator(age: 30);
            clan.AddMember(c);
            aging.AgeOneYear();
            Assert.AreEqual(31, c.Age);
        }

        [Test]
        public void AgeOneYear_KillsOfOldAge_WhenTheLifespanIsReached()
        {
            var mortal = Fixtures.Mortal(age: 69); // lifespan 70
            clan.AddMember(mortal);
            aging.AgeOneYear();
            Assert.AreEqual(DeathCause.OldAge, mortal.CauseOfDeath);
        }

        [Test]
        public void AgeOneYear_SparesThoseWithYearsLeft()
        {
            var mortal = Fixtures.Mortal(age: 60);
            clan.AddMember(mortal);
            aging.AgeOneYear();
            Assert.IsTrue(mortal.IsAlive);
        }

        [Test]
        public void AgeOneYear_NeverKillsAnUnboundedLifespan()
        {
            var immortal = Fixtures.Cultivator(age: 5000, realm: CultivationRealm.DaoEmbryo);
            clan.AddMember(immortal);
            aging.AgeOneYear();
            Assert.IsTrue(immortal.IsAlive);
        }
    }
}
