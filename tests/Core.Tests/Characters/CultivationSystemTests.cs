using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Yearly Qi gathering and the sub-levels that need no trial (LORE.md §5).</summary>
    [TestFixture]
    public class CultivationSystemTests
    {
        private GameContext ctx;
        private ClanKarmaSystem karma;
        private CultivationSystem cultivation;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            var clan = new ClanManager(ctx, "Mo");
            karma = new ClanKarmaSystem(ctx, clan);
            cultivation = new CultivationSystem(ctx, karma);
        }

        private static CharacterData Disciple(int root = 40, int stability = 70)
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1);
            c.SpiritualRoot = root;
            c.MentalStability = stability;
            c.CurrentTask = TaskType.Cultivation;
            return c;
        }

        [Test]
        public void ProcessYearlyCultivation_GainsTenPlusHalfTheRoot()
        {
            var c = Disciple(root: 40);
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(30, c.CultivationXP);
        }

        [Test]
        public void ProcessYearlyCultivation_SlowsDown_WhenStabilityIsLow()
        {
            var c = Disciple(root: 40, stability: 40);
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(24, c.CultivationXP);
        }

        [Test]
        public void ProcessYearlyCultivation_AddsTheClanKarma_OfLaterGenerations()
        {
            karma.Restore(generationCount: 5, totalBirths: 0, totalDeaths: 0, lastPatriarchId: null);
            var c = Disciple(root: 40);
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(38, c.CultivationXP); // (30 + 5 bonus XP) × 1.08
        }

        [Test]
        public void ProcessYearlyCultivation_GainsNothing_WhenNotCultivating()
        {
            var c = Disciple();
            c.CurrentTask = TaskType.Mine;
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(0, c.CultivationXP);
        }

        [Test]
        public void ProcessYearlyCultivation_GainsNothing_ForAMortal()
        {
            var mortal = Fixtures.Mortal();
            mortal.CurrentTask = TaskType.Cultivation;
            cultivation.ProcessYearlyCultivation(mortal);
            Assert.AreEqual(0, mortal.CultivationXP);
        }

        [Test]
        public void AdvanceSubLevels_ClimbsFreeStepsAndStopsBeforeATrial()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 1);
            c.CultivationXP = 3 * PowerLadder.XpForNextStage(CultivationRealm.Embryonic);
            cultivation.AdvanceSubLevels(c); // 1 → 2 is free; 2 → 3 is the Meridian Wheel trial
            Assert.IsTrue(c.RealmStage == 2 && c.CultivationXP == 2 * PowerLadder.XpForNextStage(CultivationRealm.Embryonic));
        }

        [Test]
        public void AdvanceSubLevels_DoesNothing_ForAMortal()
        {
            var mortal = Fixtures.Mortal();
            mortal.CultivationXP = 1000;
            cultivation.AdvanceSubLevels(mortal);
            Assert.AreEqual(0, mortal.RealmStage);
        }

        [Test]
        public void IsReadyForTrial_ReturnsTrue_WhenTheXpCoversATrialStep()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 2);
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Embryonic);
            Assert.IsTrue(CultivationSystem.IsReadyForTrial(c));
        }

        [Test]
        public void IsReadyForTrial_ReturnsFalse_ForAMortal()
        {
            var mortal = Fixtures.Mortal();
            mortal.CultivationXP = 1000;
            Assert.IsFalse(CultivationSystem.IsReadyForTrial(mortal));
        }

        [Test]
        public void ApplyStep_MovesTheCharacterAndRaisesItsLifespan()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9);
            cultivation.ApplyStep(c, PowerLadder.Next(c.Realm, c.RealmStage));
            Assert.IsTrue(c.Realm == CultivationRealm.Foundation && c.RealmStage == 1
                && c.MaxLifespan == PowerLadder.MaxLifespan(CultivationRealm.Foundation, 1));
        }
    }
}
