using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Realms and sub-levels of the Purple Mansion Golden Core Dao (LORE.md §5).
    /// </summary>
    [TestFixture]
    public class PowerLadderTests
    {
        [TestCase(CultivationRealm.Embryonic, 6)]
        [TestCase(CultivationRealm.QiRefinement, 9)]
        [TestCase(CultivationRealm.Foundation, 4)]
        [TestCase(CultivationRealm.PurpleMansion, 4)]
        [TestCase(CultivationRealm.GoldenCore, 4)]
        public void StageCount_MatchesLore(CultivationRealm realm, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.StageCount(realm));
        }

        [TestCase(1, QiPhase.Early)]
        [TestCase(3, QiPhase.Early)]
        [TestCase(4, QiPhase.Middle)]
        [TestCase(6, QiPhase.Middle)]
        [TestCase(7, QiPhase.Late)]
        [TestCase(9, QiPhase.Late)]
        public void QiPhaseOf_GroupsLevelsByThree(int level, QiPhase expected)
        {
            Assert.AreEqual(expected, PowerLadder.QiPhaseOf(level));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        public void PurpleMansionStage_FollowsDivineAbilityCount(int abilities, int expectedStage)
        {
            Assert.AreEqual(expectedStage, PowerLadder.PurpleMansionStageFromAbilities(abilities));
        }

        [TestCase(0, 1, TrialKind.InnerLakeChakra)]
        [TestCase(1, 2, TrialKind.None)]
        [TestCase(2, 3, TrialKind.MeridianWheelChakra)]
        [TestCase(3, 4, TrialKind.None)]
        [TestCase(4, 5, TrialKind.SummitEyeChakra)]
        [TestCase(5, 6, TrialKind.None)]
        public void Next_EmbryonicChakras_HaveThreeTrials(int stage, int expectedStage, TrialKind expectedTrial)
        {
            var step = PowerLadder.Next(CultivationRealm.Embryonic, stage);
            Assert.IsTrue(step.IsAvailable && step.TargetRealm == CultivationRealm.Embryonic
                && step.TargetStage == expectedStage && step.Trial == expectedTrial);
        }

        [Test]
        public void Next_ReturnsQiFirstLevel_WhenSixthChakraCondensed()
        {
            var step = PowerLadder.Next(CultivationRealm.Embryonic, 6);
            Assert.IsTrue(step.IsAvailable && step.TargetRealm == CultivationRealm.QiRefinement
                && step.TargetStage == 1 && step.Trial == TrialKind.None);
        }

        [Test]
        public void Next_ReturnsFoundationWall_WhenNinthQiLevelReached()
        {
            var step = PowerLadder.Next(CultivationRealm.QiRefinement, 9);
            Assert.IsTrue(step.IsAvailable && step.TargetRealm == CultivationRealm.Foundation
                && step.TargetStage == 1 && step.Trial == TrialKind.FoundationWall);
        }

        [Test]
        public void Next_AdvancesFoundationStagesWithoutTrial()
        {
            var step = PowerLadder.Next(CultivationRealm.Foundation, 2);
            Assert.IsTrue(step.IsAvailable && step.TargetStage == 3 && step.Trial == TrialKind.None);
        }

        [Test]
        public void Next_IsUnavailable_WhenGoldenCoreNeedsARoute()
        {
            var step = PowerLadder.Next(CultivationRealm.GoldenCore, 1);
            Assert.IsTrue(!step.IsAvailable && step.Trial == TrialKind.GoldenCoreRoute);
        }

        [TestCase(TrialKind.InnerLakeChakra, 30, 70)]
        [TestCase(TrialKind.MeridianWheelChakra, 30, 80)]
        [TestCase(TrialKind.SummitEyeChakra, 30, 75)]
        [TestCase(TrialKind.FoundationWall, 45, 35)]
        [TestCase(TrialKind.FoundationWall, 60, 35)]
        [TestCase(TrialKind.FoundationWall, 70, 15)]
        [TestCase(TrialKind.FoundationWall, 90, 5)]
        public void BaseTrialChance_UsesBalanceDefaults(TrialKind trial, int age, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.BaseTrialChance(trial, age));
        }

        [TestCase(40, 20)]
        [TestCase(60, 20)]
        [TestCase(70, 50)]
        [TestCase(100, 90)]
        public void DissolutionChanceOnFailure_RisesAfterSixty(int age, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.DissolutionChanceOnFailure(age));
        }

        [TestCase(CultivationRealm.Embryonic, 0, 80)]
        [TestCase(CultivationRealm.Embryonic, 1, 120)]
        [TestCase(CultivationRealm.QiRefinement, 5, 200)]
        [TestCase(CultivationRealm.Foundation, 1, 300)]
        [TestCase(CultivationRealm.PurpleMansion, 1, 500)]
        [TestCase(CultivationRealm.GoldenCore, 1, 1000)]
        [TestCase(CultivationRealm.DaoEmbryo, 1, int.MaxValue)]
        [TestCase(CultivationRealm.GoldenImmortal, 1, int.MaxValue)]
        public void MaxLifespan_FollowsLore(CultivationRealm realm, int stage, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.MaxLifespan(realm, stage));
        }

        [TestCase(CultivationRealm.Embryonic, 17)]
        [TestCase(CultivationRealm.QiRefinement, 56)]
        [TestCase(CultivationRealm.Foundation, 500)]
        public void XpForNextStage_SplitsFormerRealmThresholds(CultivationRealm realm, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.XpForNextStage(realm));
        }

        [Test]
        public void Normalize_GivesFirstLevel_WhenOldSaveHasQiRealmWithoutStage()
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 0 };
            PowerLadder.Normalize(c);
            Assert.AreEqual(1, c.RealmStage);
        }

        [Test]
        public void Normalize_ClampsStageToRealmMaximum()
        {
            var c = new CharacterData { Realm = CultivationRealm.Embryonic, RealmStage = 9 };
            PowerLadder.Normalize(c);
            Assert.AreEqual(6, c.RealmStage);
        }

        [Test]
        public void LifespanLimit_UsesTheIndividualLifespan_WhenSet()
        {
            var c = new CharacterData { MaxLifespan = 64 }; // mortal with a short life, or a Dao wound
            Assert.AreEqual(64, PowerLadder.LifespanLimit(c));
        }

        [Test]
        public void LifespanLimit_FallsBackToTheLadder_WhenUnset()
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 1 };
            Assert.AreEqual(200, PowerLadder.LifespanLimit(c));
        }

        [TestCase(0, 200)]    // missing: take the ladder
        [TestCase(500, 200)]  // above the realm's reach: clamp
        [TestCase(160, 160)]  // Dao wound: keep it
        public void NormalizeLifespan_KeepsReductionsButNeverExceedsTheRealm(int stored, int expected)
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 4, MaxLifespan = stored };
            PowerLadder.NormalizeLifespan(c);
            Assert.AreEqual(expected, c.MaxLifespan);
        }

        [Test]
        public void NormalizeLifespan_ClampsToTheWoundedReach_WhenDaoWounded()
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 4, MaxLifespan = 500, DaoWounds = 1 };
            PowerLadder.NormalizeLifespan(c);
            Assert.AreEqual(160, c.MaxLifespan);
        }

        [TestCase(200, 0, 200)]
        [TestCase(200, 1, 160)]
        [TestCase(200, 2, 128)]
        public void WoundedLifespan_TakesAFifthPerDaoWound(int lifespan, int wounds, int expected)
        {
            Assert.AreEqual(expected, PowerLadder.WoundedLifespan(lifespan, wounds));
        }

        [Test]
        public void WoundedLifespan_LeavesAnUnboundedLifespan()
        {
            Assert.AreEqual(PowerLadder.Unbounded, PowerLadder.WoundedLifespan(PowerLadder.Unbounded, 3));
        }

        [Test]
        public void LifespanAfterAdvance_RaisesToTheNewRealmReach()
        {
            var c = new CharacterData { Realm = CultivationRealm.Foundation, RealmStage = 1, MaxLifespan = 200 };
            Assert.AreEqual(300, PowerLadder.LifespanAfterAdvance(c));
        }

        [Test]
        public void LifespanAfterAdvance_KeepsDaoWoundsAcrossBreakthroughs()
        {
            var c = new CharacterData { Realm = CultivationRealm.Foundation, RealmStage = 1, MaxLifespan = 160, DaoWounds = 1 };
            Assert.AreEqual(240, PowerLadder.LifespanAfterAdvance(c));
        }

        [Test]
        public void LifespanAfterAdvance_NeverShortensALife()
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 5, MaxLifespan = 191, DaoWounds = 1 };
            Assert.AreEqual(191, PowerLadder.LifespanAfterAdvance(c));
        }

        [Test]
        public void LifespanAfterAdvance_ReplacesTheMortalRoll_AtTheFirstChakra()
        {
            var c = new CharacterData { Realm = CultivationRealm.Embryonic, RealmStage = 1, MaxLifespan = 65 };
            Assert.AreEqual(120, PowerLadder.LifespanAfterAdvance(c));
        }
    }
}
