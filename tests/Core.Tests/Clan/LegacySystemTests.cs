using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>What the dead leave behind: their savings, and for the greatest, a Dao fragment.</summary>
    [TestFixture]
    public class LegacySystemTests
    {
        private static (TestWorld world, LegacySystem legacy) Build()
        {
            var w = new TestWorld();
            return (w, new LegacySystem(w.Ctx, w.Clan, w.Resources, w.Deduction));
        }

        [Test]
        public void MemberDeath_ReturnsTheirSavingsToTheTreasury()
        {
            var (w, _) = Build();
            w.Clan.Kill(w.Join(Fixtures.Cultivator()), DeathCause.OldAge);
            Assert.AreEqual(1000 + LegacySystem.InheritedStones, w.Resources.SpiritStones);
        }

        [Test]
        public void GoldenCoreDeath_LeavesADaoFragmentOfTheirAffinity()
        {
            var (w, _) = Build();
            var monarch = Fixtures.Cultivator(realm: CultivationRealm.GoldenCore);
            monarch.Affinity = Element.Lightning;
            w.Clan.Kill(w.Join(monarch), DeathCause.OldAge);
            Assert.IsTrue(w.Deduction.Fragments.Count == 1 && w.Deduction.Fragments[0].Element == Element.Lightning);
        }

        [Test]
        public void StrangerDeath_LeavesNothingToTheClan()
        {
            var (w, _) = Build();
            w.Clan.Kill(Fixtures.Cultivator(), DeathCause.Combat);
            Assert.AreEqual(1000, w.Resources.SpiritStones);
        }
    }
}
