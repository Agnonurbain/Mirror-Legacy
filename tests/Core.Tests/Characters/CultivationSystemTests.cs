using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Yearly Qi gathering and the sub-levels that need no trial (LORE.md §5).</summary>
    [TestFixture]
    public class CultivationSystemTests
    {
        private GameContext ctx;
        private ClanKarmaSystem karma;
        private TechniqueLibrary library;
        private ResourceManager resources;
        private CultivationSystem cultivation;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            var clan = new ClanManager(ctx, "Mo");
            karma = new ClanKarmaSystem(ctx, clan);
            library = new TechniqueLibrary(ctx);
            resources = new ResourceManager(ctx);
            cultivation = new CultivationSystem(ctx, karma, library, resources);
        }

        /// <summary>A breathing member at the sixth chakra with the XP to enter Qi Cultivation.</summary>
        private static CharacterData AtTheFirstBreath(string method = null)
        {
            var c = Fixtures.Cultivator(age: 18, realm: CultivationRealm.Embryonic, stage: 6);
            c.CultivationMethodId = method;
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Embryonic);
            return c;
        }

        // ---- Methods and grades (LORE.md §2) ----

        [Test]
        public void ProcessYearlyCultivation_FollowsTheGradeOfTheMethod()
        {
            var c = Disciple(root: 40);
            c.CultivationMethodId = "common-breath-method"; // grade 2
            c.QiId = "common-breath-qi";
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(24, c.CultivationXP); // 30 × 0.8
        }

        [Test]
        public void ProcessYearlyCultivation_GainsNothing_ForAQiCultivatorWithoutAMethod()
        {
            var c = Disciple(root: 40);
            c.CultivationMethodId = null;
            cultivation.ProcessYearlyCultivation(c);
            Assert.AreEqual(0, c.CultivationXP);
        }

        [Test]
        public void EnteringQi_AbsorbsAPortionOfTheMethodsQi()
        {
            library.Learn("clear-spring-sutra");
            resources.AddQi("clear-spring-qi", 1);
            var c = AtTheFirstBreath("clear-spring-sutra");

            cultivation.AdvanceSubLevels(c);

            Assert.IsTrue(c.Realm == CultivationRealm.QiRefinement && c.RealmStage == 1 && c.QiId == "clear-spring-qi");
            Assert.AreEqual(0, resources.QiPortions("clear-spring-qi"));
        }

        [Test]
        public void EnteringQi_Waits_WithoutAPortion()
        {
            library.Learn("clear-spring-sutra");
            var c = AtTheFirstBreath("clear-spring-sutra");

            cultivation.AdvanceSubLevels(c);

            Assert.IsTrue(c.Realm == CultivationRealm.Embryonic && c.RealmStage == 6 && c.QiId == null);
            Assert.AreEqual(PowerLadder.XpForNextStage(CultivationRealm.Embryonic), c.CultivationXP);
        }

        [Test]
        public void EnteringQi_WithoutAChosenMethod_TakesTheBestKnownOneWhoseQiIsInStore()
        {
            library.Learn("clear-spring-sutra");
            library.Learn("measured-rain-method"); // grade 4, but no Qi in store
            resources.AddQi("clear-spring-qi", 1);
            var c = AtTheFirstBreath();

            cultivation.AdvanceSubLevels(c);

            Assert.IsTrue(c.Realm == CultivationRealm.QiRefinement && c.CultivationMethodId == "clear-spring-sutra");
        }

        [Test]
        public void EnteringQi_NeverChoosesACappedMethodOnTheirBehalf()
        {
            // The Souffle Commun stops at Qi Cultivation (§11.2): only the patriarch's explicit choice imposes it
            library.Learn("common-breath-method");
            var c = AtTheFirstBreath();

            cultivation.AdvanceSubLevels(c);

            Assert.AreEqual(CultivationRealm.Embryonic, c.Realm);
        }

        [Test]
        public void EnteringQi_WithTheSouffleCommun_NeedsNoPortion()
        {
            library.Learn("common-breath-method");
            var c = AtTheFirstBreath("common-breath-method");

            cultivation.AdvanceSubLevels(c);

            Assert.IsTrue(c.Realm == CultivationRealm.QiRefinement && c.QiId == "common-breath-qi");
        }

        [Test]
        public void IsReadyForTrial_ReturnsFalse_WhenTheMethodStopsBeforeTheNextRealm()
        {
            var capped = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9);
            capped.CultivationMethodId = "common-breath-method";
            capped.QiId = "common-breath-qi";
            capped.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            var clan = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9);
            clan.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            resources.AddQi(Fixtures.ClanQi, 1);  // the wall absorbs a portion (L4.2)
            resources.AddQi("common-breath-qi", 1);

            Assert.IsFalse(cultivation.IsReadyForTrial(capped));
            Assert.IsTrue(cultivation.IsReadyForTrial(clan));
        }

        [Test]
        public void ApplyStep_KeepsTheFlawOfTheMethod()
        {
            var c = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 5);
            c.CultivationMethodId = "path-watcher";
            cultivation.ApplyStep(c, PowerLadder.Next(c.Realm, c.RealmStage));
            Assert.AreEqual(114, c.MaxLifespan); // 120 × 0.95
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
            Assert.IsTrue(cultivation.IsReadyForTrial(c));
        }

        [Test]
        public void IsReadyForTrial_ReturnsFalse_ForAMortal()
        {
            var mortal = Fixtures.Mortal();
            mortal.CultivationXP = 1000;
            Assert.IsFalse(cultivation.IsReadyForTrial(mortal));
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
