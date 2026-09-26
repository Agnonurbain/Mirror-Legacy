using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Breakthrough odds and outcomes on the power ladder (LORE.md §5.1-5.3).
    /// </summary>
    [TestFixture]
    public class BreakthroughRulesTests
    {
        private static CharacterData Make(CultivationRealm realm, int stage, int root = 50, int stability = 70, int age = 20, int maxLifespan = 120)
        {
            return new CharacterData
            {
                Realm = realm, RealmStage = stage, SpiritualRoot = root,
                MentalStability = stability, Age = age, MaxLifespan = maxLifespan, IsAlive = true
            };
        }

        private static BalanceSettings Balance => Fixtures.Content.Balance;

        private static int Rate(CharacterData c) => BreakthroughRules.SuccessRate(c, Balance);

        private static BreakthroughOutcome Resolve(TrialKind trial, int rate, int age, int roll, int severityRoll) =>
            BreakthroughRules.Resolve(trial, rate, age, roll, severityRoll, Balance.Trials);

        /// <summary>The shipped trial settings with one value changed (the interpretations live in balance.json).</summary>
        private static TrialSettings Trials(System.Func<TrialSettings, TrialSettings> change) => change(Balance.Trials);

        [Test]
        public void Resolve_FollowsTheDeviationTableOfTheBalance()
        {
            var lenient = Trials(t => t with { MinorFailureMaxRoll = 99 });
            Assert.AreEqual(BreakthroughOutcome.MinorFailure, BreakthroughRules.Resolve(TrialKind.SummitEyeChakra, 75, 20, 76, 90, lenient));
        }

        [Test]
        public void SuccessRate_FollowsTheOldAgePenaltyOfTheBalance()
        {
            var old = Make(CultivationRealm.Embryonic, 4, age: 90, maxLifespan: 100);
            var harsh = Balance with { Trials = Trials(t => t with { OldAgePenalty = 30 }) };
            Assert.AreEqual(Rate(old) - 20, BreakthroughRules.SuccessRate(old, harsh));
        }

        [Test]
        public void SuccessRate_AddsRootBonus_WhenMeridianWheelTrial()
        {
            // base 80 + (50 - 10) / 5
            Assert.AreEqual(88, Rate(Make(CultivationRealm.Embryonic, 2)));
        }

        [Test]
        public void SuccessRate_UsesFoundationWall_WhenNinthQiLevel()
        {
            // base 35 + (50 - 30) / 5
            Assert.AreEqual(39, Rate(Make(CultivationRealm.QiRefinement, 9, maxLifespan: 200)));
        }

        [Test]
        public void SuccessRate_DropsWithAge_WhenFoundationWallAfterSixty()
        {
            var young = Rate(Make(CultivationRealm.QiRefinement, 9, age: 50, maxLifespan: 200));
            var late = Rate(Make(CultivationRealm.QiRefinement, 9, age: 75, maxLifespan: 200));
            Assert.That(late, Is.LessThan(young));
        }

        [Test]
        public void SuccessRate_AppliesStabilityPenalty_WhenBelowFifty()
        {
            var calm = Rate(Make(CultivationRealm.Embryonic, 0, stability: 70));
            var troubled = Rate(Make(CultivationRealm.Embryonic, 0, stability: 30));
            Assert.AreEqual(20, calm - troubled);
        }

        [Test]
        public void SuccessRate_AppliesAgePenalty_WhenPastEightyPercentOfLifespan()
        {
            var young = Rate(Make(CultivationRealm.Embryonic, 4, age: 20, maxLifespan: 100));
            var old = Rate(Make(CultivationRealm.Embryonic, 4, age: 90, maxLifespan: 100));
            Assert.AreEqual(10, young - old);
        }

        [Test]
        public void SuccessRate_IsZero_WhenNoTrialIsDue()
        {
            Assert.AreEqual(0, Rate(Make(CultivationRealm.Embryonic, 1)));
        }

        [Test]
        public void SuccessRate_IsZero_WhenGoldenCoreNeedsARoute()
        {
            Assert.AreEqual(0, Rate(Make(CultivationRealm.GoldenCore, 1, maxLifespan: 1000)));
        }

        [Test]
        public void SuccessRate_StaysBetweenOneAndNinetyNine()
        {
            var best = Rate(Make(CultivationRealm.Embryonic, 2, root: 100, stability: 100));
            var worst = Rate(Make(CultivationRealm.QiRefinement, 9, root: 1, stability: 10, age: 190, maxLifespan: 200));
            Assert.IsTrue(best >= 1 && best <= 99 && worst == 1, $"best {best}, worst {worst}");
        }

        [Test]
        public void Resolve_ReturnsSuccess_WhenRollWithinRate()
        {
            Assert.AreEqual(BreakthroughOutcome.Success, Resolve(TrialKind.InnerLakeChakra, 70, 20, 70, 1));
        }

        [TestCase(70, BreakthroughOutcome.MinorFailure)]
        [TestCase(95, BreakthroughOutcome.MajorFailure)]
        [TestCase(96, BreakthroughOutcome.QiDeviationDeath)]
        public void Resolve_UsesSeverityTable_WhenChakraTrialFails(int severityRoll, BreakthroughOutcome expected)
        {
            Assert.AreEqual(expected, Resolve(TrialKind.SummitEyeChakra, 75, 20, 76, severityRoll));
        }

        [Test]
        public void Resolve_ReturnsSpiritualDissolution_WhenFoundationWallFailsWithinDissolutionChance()
        {
            // dissolution chance at 70 years: 20 + 10 * 3 = 50
            Assert.AreEqual(BreakthroughOutcome.SpiritualDissolution, Resolve(TrialKind.FoundationWall, 15, 70, 16, 50));
        }

        [Test]
        public void Resolve_ReturnsMajorFailure_WhenFoundationWallFailsAboveDissolutionChance()
        {
            Assert.AreEqual(BreakthroughOutcome.MajorFailure, Resolve(TrialKind.FoundationWall, 35, 40, 36, 21));
        }
    }
}
