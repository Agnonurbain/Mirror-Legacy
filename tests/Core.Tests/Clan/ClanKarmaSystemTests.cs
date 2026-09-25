using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>Generations (one per new patriarch), births, deaths and the karma they bring.</summary>
    [TestFixture]
    public class ClanKarmaSystemTests
    {
        private GameContext ctx;
        private ClanManager clan;
        private ClanKarmaSystem karma;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            clan = new ClanManager(ctx, "Mo");
            karma = new ClanKarmaSystem(ctx, clan);
        }

        [Test]
        public void Births_CountOnlyNewborns()
        {
            clan.AddMember(Fixtures.Cultivator(age: 30)); // a spouse marrying in
            clan.GenerateChild(null, null);
            Assert.AreEqual(1, karma.TotalBirths);
        }

        [Test]
        public void Deaths_AreCounted()
        {
            var c = Fixtures.Cultivator();
            clan.AddMember(c);
            clan.Kill(c, DeathCause.Illness);
            Assert.AreEqual(1, karma.TotalDeaths);
        }

        [Test]
        public void NewPatriarch_StartsANewGenerationAtTheNextYear()
        {
            var first = Fixtures.Cultivator(age: 60);
            var heir = Fixtures.Cultivator(age: 30);
            clan.AddMember(first);
            clan.AddMember(heir);
            clan.AppointPatriarch(first);
            ctx.Events.TriggerYearStarted(1);

            clan.Kill(first, DeathCause.OldAge);
            ctx.Events.TriggerYearStarted(2);

            Assert.AreEqual(2, karma.GenerationCount);
        }

        [Test]
        public void SamePatriarch_KeepsTheGeneration()
        {
            var patriarch = Fixtures.Cultivator();
            clan.AddMember(patriarch);
            clan.AppointPatriarch(patriarch);
            ctx.Events.TriggerYearStarted(1);
            ctx.Events.TriggerYearStarted(2);
            Assert.AreEqual(1, karma.GenerationCount);
        }

        [TestCase(1, 0.0)]
        [TestCase(5, 0.08)]
        public void CultivationSpeedBonus_GrowsTwoPercentPerLaterGeneration(int generations, double expected)
        {
            karma.Restore(generations, 0, 0, null);
            Assert.AreEqual(expected, karma.GetCultivationSpeedBonus(), 1e-9);
        }

        [TestCase(4, 0)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        public void BonusXp_RisesAtFiveAndTenGenerations(int generations, int expected)
        {
            karma.Restore(generations, 0, 0, null);
            Assert.AreEqual(expected, karma.GetBonusXP());
        }

        [Test]
        public void AncestralTechnique_UnlocksAtTenGenerations()
        {
            karma.Restore(10, 0, 0, null);
            Assert.IsTrue(karma.HasAncestralTechniqueUnlock());
        }
    }
}
