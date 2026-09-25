using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Reaching the Dao Embryo lifts an ancestor out of the mortal plane.</summary>
    [TestFixture]
    public class AscensionSystemTests
    {
        private GameContext ctx;
        private ClanManager clan;
        private AscensionSystem ascension;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            clan = new ClanManager(ctx, "Mo");
            ascension = new AscensionSystem(ctx, clan);
        }

        [Test]
        public void BreakthroughToDaoEmbryo_AscendsTheAncestor()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo);
            clan.AddMember(c);
            ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.DaoEmbryo);
            Assert.IsTrue(!c.IsAlive && ascension.AscendedAncestorsCount == 1);
        }

        [Test]
        public void BreakthroughBelowDaoEmbryo_KeepsTheMemberInTheClan()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.GoldenCore);
            clan.AddMember(c);
            ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.GoldenCore);
            Assert.IsTrue(c.IsAlive && ascension.AscendedAncestorsCount == 0);
        }
    }
}
