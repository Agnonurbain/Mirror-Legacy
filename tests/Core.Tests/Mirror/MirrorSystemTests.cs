using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>The bronze mirror (the player): its power and its divine interventions.</summary>
    [TestFixture]
    public class MirrorSystemTests
    {
        [Test]
        public void NewMirror_HoldsFiftyPower()
        {
            Assert.AreEqual(50, new TestWorld().Mirror.MirrorPower);
        }

        [Test]
        public void NewYear_RechargesOnePower()
        {
            var w = new TestWorld();
            w.Ctx.Events.TriggerYearStarted(2);
            Assert.AreEqual(51, w.Mirror.MirrorPower);
        }

        [Test]
        public void Breakthrough_RechargesFivePower()
        {
            var w = new TestWorld();
            w.Ctx.Events.TriggerBreakthroughSuccess(Fixtures.Cultivator(), CultivationRealm.QiRefinement);
            Assert.AreEqual(55, w.Mirror.MirrorPower);
        }

        [Test]
        public void AddPower_NeverExceedsTheMaximum()
        {
            var w = new TestWorld();
            w.Mirror.AddPower(500);
            Assert.AreEqual(MirrorSystem.MaxMirrorPower, w.Mirror.MirrorPower);
        }

        [Test]
        public void ConsumePower_Refuses_WhenShort()
        {
            var w = new TestWorld();
            bool spent = w.Mirror.ConsumePower(60);
            Assert.IsTrue(!spent && w.Mirror.MirrorPower == 50);
        }

        [Test]
        public void UseAncestralShield_ArmsTheNextBreakthrough()
        {
            var w = new TestWorld();
            w.Mirror.UseAncestralShield();
            Assert.IsTrue(w.Breakthroughs.AncestralShieldActive && w.Mirror.MirrorPower == 25);
        }

        [Test]
        public void UseMirrorJudgment_StrikesTheTargetDown()
        {
            var w = new TestWorld();
            var traitor = w.Join(Fixtures.Cultivator());
            w.Mirror.UseMirrorJudgment(traitor);
            Assert.IsTrue(!traitor.IsAlive && traitor.CauseOfDeath == DeathCause.QiDeviation && w.Mirror.MirrorPower == 0);
        }

        [Test]
        public void GrantTalismanSeed_LetsAMortalCultivate()
        {
            var w = new TestWorld();
            var mortal = w.Join(Fixtures.Mortal());
            bool granted = w.Mirror.GrantTalismanSeed(mortal);
            Assert.IsTrue(granted && mortal.HasTalismanSeed && w.Mirror.MirrorPower == 10);
        }

        [Test]
        public void GrantTalismanSeed_Refuses_WhenEverySeedIsInUse()
        {
            var w = new TestWorld();
            w.Mirror.AddPower(50);
            for (int i = 0; i < 2; i++)
                w.Join(Fixtures.Mortal()).HasTalismanSeed = true;
            Assert.IsFalse(w.Mirror.GrantTalismanSeed(w.Join(Fixtures.Mortal())));
        }

        [Test]
        public void GrantTalismanSeed_Refuses_ACultivator()
        {
            var w = new TestWorld();
            Assert.IsFalse(w.Mirror.GrantTalismanSeed(w.Join(Fixtures.Cultivator())));
        }

        [Test]
        public void UseQiPulse_RestoresAThirdOfTheQiAndSomeVitality()
        {
            var w = new TestWorld();
            var unit = new MirrorChronicles.Combat.CombatUnit(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement), isAlly: true);
            unit.ConsumeQi(50);
            unit.TakeDamage(60);
            int qi = unit.CurrentQi, vitality = unit.CurrentVitality;

            w.Mirror.UseQiPulse(unit);

            Assert.IsTrue(unit.CurrentQi == qi + 30 && unit.CurrentVitality == vitality + 22 && w.Mirror.MirrorPower == 40);
        }

        [Test]
        public void Restore_SetsPowerAndFragments()
        {
            var w = new TestWorld();
            w.Mirror.Restore(80, 3);
            Assert.IsTrue(w.Mirror.MirrorPower == 80 && w.Mirror.TalismanSeedCapacity == 5);
        }
    }
}
