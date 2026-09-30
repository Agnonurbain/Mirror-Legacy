using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The methods that lead to the Purple Mansion (LORE.md §11.10; the user's decisions of 2026-09-30): the most precious
    /// thing in this world, never sold for stones; the mirror deduces one of its own will once three shards are restored and
    /// it has gathered enough knowledge — good fragments, and a lineage the clan knows.
    /// </summary>
    [TestFixture]
    public class AscentMethodTests
    {
        private static TechniqueSettings Settings => Fixtures.Content.Balance.Techniques;

        // ---- Never for stones ----

        [Test]
        public void AnAscentMethod_IsNeverSoldForStones()
        {
            var w = new TestWorld();
            w.Factions.AddFaction(new FactionData { Name = "Famille Bai", Kind = FactionKind.Family, RegionId = "linxi",
                RelationWithPlayer = 100, Techniques = { "silent-tide-sutra", "woven-heart-sutra" } });
            w.Resources.AddSpiritStones(1_000_000);

            StringAssert.Contains("ne s'achète pas", w.Exchange.PurchaseRefusal("Famille Bai", "silent-tide-sutra"));
            Assert.IsFalse(w.Exchange.BuyTechnique("Famille Bai", "silent-tide-sutra"));
            Assert.IsNull(w.Exchange.PurchaseRefusal("Famille Bai", "woven-heart-sutra"), "an ordinary method still sells");
        }

        // ---- The mirror's own deduction ----

        /// <summary>A mirror ready to deduce: three shards restored and awake, good fragments, a lineage known, power enough.</summary>
        private static TestWorld Ready()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, Settings.AscentDeductionShards);
            for (int i = 0; i < Settings.AscentDeductionFragments; i++) w.Deduction.AddFragment(Element.Water, Settings.AscentDeductionQuality);
            w.Knowledge.Reveal(new Fact(FactKind.Lineage, "orthodox-water"), KnowledgeSource.Mirror);
            return w;
        }

        [Test]
        public void TheMirror_DeducesAnAscentMethod_OnceReady()
        {
            var w = Ready();
            int power = w.Mirror.MirrorPower;

            var method = w.Deduction.DeduceAscentMethod();

            Assert.IsNotNull(method);
            Assert.IsTrue(method.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(method) && method.Grade >= 5);
            Assert.IsTrue(w.Techniques.Knows(method.ID));
            var qi = w.Techniques.FindQi(method.RequiredQiId);
            Assert.IsNotNull(qi);
            var (fruition, _) = FoundationRef.Parse(qi.Foundation);
            Assert.IsTrue(!qi.Vanished && fruition == "orthodox-water", "on a Qi of a lineage the clan knows");
            Assert.AreEqual(power - Settings.AscentDeductionPower, w.Mirror.MirrorPower);
            Assert.IsEmpty(w.Deduction.Fragments);
        }

        [Test]
        public void TheMirror_NeedsThreeShards()
        {
            var w = Ready();
            w.Mirror.Restore(w.Mirror.MirrorPower, Settings.AscentDeductionShards - 1);
            StringAssert.Contains("éclats", w.Deduction.AscentRefusal());
        }

        [Test]
        public void TheMirror_NeedsGoodFragments()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, Settings.AscentDeductionShards);
            w.Knowledge.Reveal(new Fact(FactKind.Lineage, "orthodox-water"), KnowledgeSource.Mirror);
            for (int i = 0; i < Settings.AscentDeductionFragments; i++) w.Deduction.AddFragment(Element.Water, 1);
            StringAssert.Contains("fragments", w.Deduction.AscentRefusal());
        }

        [Test]
        public void TheMirror_NeedsALineageTheClanKnows()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, Settings.AscentDeductionShards);
            for (int i = 0; i < Settings.AscentDeductionFragments; i++) w.Deduction.AddFragment(Element.Water, Settings.AscentDeductionQuality);
            Assume.That(w.Knowledge.Keys.Any(k => k.StartsWith("Lineage:")), Is.False, "the fixture's clan knows no lineage yet");
            StringAssert.Contains("lignée", w.Deduction.AscentRefusal());
        }

        [Test]
        public void TheMirror_DeducesNothing_WhileItSleeps()
        {
            var w = Ready();
            w.Mirror.Restore(w.Mirror.MirrorPower, w.Mirror.RestoredFragments, asleepUntil: w.Ctx.Clock.Year + 2);
            StringAssert.Contains("dort", w.Deduction.AscentRefusal());
            Assert.IsNull(w.Deduction.DeduceAscentMethod());
        }
    }
}
