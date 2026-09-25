using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The divine abilities of the Purple Mansion (LORE.md §5.4.3-5.4.4): condensed one after another from the
    /// Dao Partners — by cultivating an aligned technique, by resources (fast but shallow), or by the Dao Graft
    /// (a clan member's foundation consumed) — the fourth past the Threshold of Immortality; the stage follows.
    /// </summary>
    [TestFixture]
    public class DivineAbilityTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.995;
        private const string Sea = "orthodox-water:boundless-sea";
        private const string Farewell = "orthodox-water:river-farewell";

        /// <summary>A Purple Mansion cultivator of the Orthodox Water holding the given abilities.</summary>
        private static CharacterData Master(TestWorld w, params string[] abilities)
        {
            var c = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 1);
            c.CultivationMethodId = "inner-sun-manual";
            c.FoundationId = Sea;
            c.DivineAbilities = new List<string>(abilities.Length > 0 ? abilities : new[] { Sea });
            c.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(c.DivineAbilities.Count);
            return w.Join(c);
        }

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        // ---- Choosing the next ability ----

        [Test]
        public void Pursue_TakesARevealedPartnerOfTheLineage()
        {
            var w = new TestWorld();
            var c = Master(w);
            Assert.IsTrue(w.Abilities.Pursue(c, Farewell));
            Assert.AreEqual(Farewell, c.PursuedAbility);
        }

        [TestCase("orthodox-water:boundless-sea")]   // already held
        [TestCase("mutable-water:mist-veil")]        // another lineage
        public void Pursue_Refuses(string ability)
        {
            var w = new TestWorld();
            Assert.IsFalse(w.Abilities.Pursue(Master(w), ability));
        }

        [Test]
        public void Pursue_Refuses_AnAbilityTheWorldHasNotRevealed()
        {
            // P3: knowledge is a resource — the Nourishing Water keeps two of its abilities unknown
            var w = new TestWorld();
            var c = Master(w, "nourishing-water:winter-drizzle");
            Assert.IsTrue(w.Abilities.Pursue(c, "nourishing-water:dawn-abyss"));
            Assert.IsFalse(w.Abilities.Pursue(c, "nourishing-water:unrevealed-4"));
        }

        // ---- Cultivating it: a technique aligned on the partner, cultivated to the Purple Mansion ----

        [Test]
        public void Cultivation_CondensesThePursuedAbility_WithAnAlignedMethodAndItsQi()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Techniques.Learn("upstream-brook-method"); // its Qi builds the River Farewell
            w.Resources.AddQi("upstream-brook-qi", 1);
            var c = Master(w);
            w.Abilities.Pursue(c, Farewell);
            c.CultivationXP = Xp;

            w.Abilities.ProcessBreakthroughPhase();

            CollectionAssert.AreEqual(new[] { Sea, Farewell }, c.DivineAbilities);
            Assert.AreEqual(1, c.RealmStage); // 1-2 abilities: early Purple Mansion
            Assert.AreEqual(0, w.Resources.QiPortions("upstream-brook-qi"));
        }

        [Test]
        public void Cultivation_Waits_WithoutAnAlignedMethod()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w);
            w.Abilities.Pursue(c, Farewell);
            c.CultivationXP = Xp;

            w.Abilities.ProcessBreakthroughPhase();

            Assert.AreEqual(1, c.DivineAbilities.Count);
        }

        [Test]
        public void FourthAbility_FailsTheThresholdOfImmortality_AndLosesTheXp()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            w.Techniques.Learn("upstream-brook-method");
            w.Resources.AddQi("upstream-brook-qi", 1);
            var c = Master(w, Sea, "orthodox-water:ford-watcher", "orthodox-water:storm-sky");
            w.Abilities.Pursue(c, Farewell);
            c.CultivationXP = Xp;

            w.Abilities.ProcessBreakthroughPhase();

            Assert.IsTrue(c.DivineAbilities.Count == 3 && c.CultivationXP == 0);
        }

        [Test]
        public void FourthAbility_PassesTheThreshold_IntoTheLatePurpleMansion()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Techniques.Learn("upstream-brook-method");
            w.Resources.AddQi("upstream-brook-qi", 1);
            var c = Master(w, Sea, "orthodox-water:ford-watcher", "orthodox-water:storm-sky");
            w.Abilities.Pursue(c, Farewell);
            c.CultivationXP = Xp;

            w.Abilities.ProcessBreakthroughPhase();

            Assert.IsTrue(c.DivineAbilities.Count == 4 && c.RealmStage == 3);
        }

        // ---- Resources: fast, but shallow foundations ----

        [Test]
        public void Resources_CondenseAtHalfTheXp_ButLeaveTheAbilityShallow()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var cost = w.Ctx.Content.Balance.DivineAbilities;
            w.Resources.AddSpiritStones(cost.ResourceStones);
            w.Resources.AddHerbs(cost.ResourceHerbs);
            w.Resources.AddOres(cost.ResourceOres);
            int stones = w.Resources.SpiritStones;
            var c = Master(w);
            c.CultivationXP = Xp / 2;

            Assert.IsTrue(w.Abilities.CondenseWithResources(c, Farewell));

            CollectionAssert.Contains(c.DivineAbilities, Farewell);
            CollectionAssert.Contains(c.ShallowAbilities, Farewell);
            Assert.AreEqual(stones - cost.ResourceStones, w.Resources.SpiritStones);
        }

        [Test]
        public void Resources_Refuse_WhenTheTreasuryCannotPay()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Resources.SetSpiritStones(0);
            var c = Master(w);
            c.CultivationXP = Xp;
            Assert.IsFalse(w.Abilities.CondenseWithResources(c, Farewell));
        }

        // ---- The Dao Graft: a prodigy's foundation consumed (§5.4.3) ----

        [Test]
        public void Graft_ConsumesAClanMembersPartnerFoundation_IntoAnAbility()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w);
            var prodigy = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            prodigy.FoundationId = Farewell;

            Assert.IsTrue(w.Abilities.GraftDaoPartner(c, prodigy));

            CollectionAssert.Contains(c.DivineAbilities, Farewell);
            CollectionAssert.Contains(c.GraftedAbilities, Farewell);
            Assert.IsTrue(prodigy.Realm == CultivationRealm.QiRefinement && prodigy.RealmStage == 9 && prodigy.FoundationId == null);
        }

        [Test]
        public void Graft_Refuses_AFoundationOfAnotherLineage()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w);
            var other = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            other.FoundationId = "mutable-water:mist-veil";
            Assert.IsFalse(w.Abilities.GraftDaoPartner(c, other));
        }

        [Test]
        public void FiveAbilities_AreTheGrandPerfection_AndThereIsNoSixth()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = Master(w, Sea, "orthodox-water:ford-watcher", "orthodox-water:storm-sky", "orthodox-water:dike-guard", Farewell);
            Assert.AreEqual(4, c.RealmStage);
            Assert.IsFalse(w.Abilities.Pursue(c, "orthodox-water:peril-refuge"));
        }
    }
}
