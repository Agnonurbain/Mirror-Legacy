using System;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>Wounds after combat, and the permanent Dao wound.</summary>
    [TestFixture]
    public class WoundSystemTests
    {
        private WoundSystem Build(Random rng)
        {
            var ctx = Fixtures.Context(rng);
            var stability = new MentalStabilitySystem(ctx, new ClanManager(ctx, "Mo"));
            return new WoundSystem(ctx, stability);
        }

        private static CharacterData Fighter() => Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 3);

        [TestCase(100, 0)]   // unscathed
        [TestCase(80, 0)]    // light
        [TestCase(50, -5)]   // moderate
        public void EvaluatePostCombatWounds_ShakesStabilityBySeverity(int remainingVitality, int expectedChange)
        {
            var c = Fighter();
            int before = c.MentalStability;
            Build(new FixedRandom(0.99)).EvaluatePostCombatWounds(c, 100, remainingVitality);
            Assert.AreEqual(before + expectedChange, c.MentalStability);
        }

        [Test]
        public void EvaluatePostCombatWounds_MayLeaveADaoWound_AfterASevereWound()
        {
            var c = Fighter();
            Build(new FixedRandom(0.1)).EvaluatePostCombatWounds(c, 100, 35); // 20% chance
            Assert.AreEqual(1, c.DaoWounds);
        }

        [Test]
        public void EvaluatePostCombatWounds_SparesTheDao_WhenTheSevereRollFails()
        {
            var c = Fighter();
            Build(new FixedRandom(0.5)).EvaluatePostCombatWounds(c, 100, 35);
            Assert.AreEqual(0, c.DaoWounds);
        }

        [Test]
        public void EvaluatePostCombatWounds_UsuallyLeavesADaoWound_AfterACriticalWound()
        {
            var c = Fighter();
            Build(new FixedRandom(0.5)).EvaluatePostCombatWounds(c, 100, 5); // 80% chance
            Assert.AreEqual(1, c.DaoWounds);
        }

        [Test]
        public void ApplyDaoWound_TakesAFifthOfTheLifespan()
        {
            var c = Fighter(); // lifespan 200
            Build(new Random(1)).ApplyDaoWound(c);
            Assert.IsTrue(c.DaoWounds == 1 && c.MaxLifespan == 160);
        }

        [Test]
        public void ApplyDaoWound_LeavesAYearOfGrace()
        {
            var c = Fighter();
            c.Age = 190;
            Build(new Random(1)).ApplyDaoWound(c);
            Assert.AreEqual(191, c.MaxLifespan);
        }

        [Test]
        public void ApplyDaoWound_KeepsAnUnboundedLifespan()
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo);
            Build(new Random(1)).ApplyDaoWound(c);
            Assert.AreEqual(PowerLadder.Unbounded, c.MaxLifespan);
        }
    }
}
