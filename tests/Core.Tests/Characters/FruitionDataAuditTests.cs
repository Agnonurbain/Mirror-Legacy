using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Lore audit §8.7 and §8.9 (the user's choices of 2026-10-15). 8.7: a substitution taken from another lineage is a copy
    /// of its original — whoever holds the original Course of the Fool to the Summit holds the Grand Shaman's substitute, and
    /// cultivates it by the original's aligned method. 8.9: the Refuge of Unity is the Orthodox Wood's substitution, the key
    /// of its Surplus (Back to Noon joins its centre); the Lian hold the Wandering Metal and the Huyan the Orthodox Wood from
    /// the start; the Hidden Metal's founding Qi is the Deep Lock Metal Qi; a group or an Immortal Lord never comes back to
    /// reclaim a Fruition.
    /// </summary>
    [TestFixture]
    public class FruitionDataAuditTests
    {
        private static GameContent Content => Fixtures.Content;
        private static FruitionDefinition Lineage(string id) => Content.Fruitions.Single(f => f.Id == id);
        private static DivineAbilityDefinition Ability(string lineage, string id) => Lineage(lineage).Abilities.Single(a => a.Id == id);

        // ---- 8.7 A substitution is a copy of its original ----

        [Test]
        public void TheOriginalEarthCourse_OpensTheGrandShamansSurplus()
        {
            var four = Lineage("grand-shaman").Abilities.Where(FoundationRules.IsOfTheCentre).Take(4).Select(a => $"grand-shaman:{a.Id}");
            var held = four.Append("orthodox-earth:fools-summit-race").ToList();
            Assert.AreEqual(PositionRoute.Surplus, GoldenCoreRules.RouteTo(held, "grand-shaman", Content.Fruitions), "Gongye Shu's way");
        }

        [Test]
        public void ACopiedSubstitution_NamesItsOriginal()
        {
            Assert.AreEqual("orthodox-earth:fools-summit-race", Ability("grand-shaman", "fools-summit-race-substitute").Original);
            Assert.AreEqual("supreme-yin:drowned-moon", Ability("mutable-water", "drowned-moon-substitute").Original);
        }

        [Test]
        public void TheLoader_RefusesASubstitutionCopyingNothing()
        {
            string fruitions = Fixtures.ReadDataFile(GameContentLoader.FruitionsFile);
            string broken = fruitions.Replace("\"original\": \"orthodox-earth:fools-summit-race\"", "\"original\": \"orthodox-earth:nowhere\"");
            Assert.AreNotEqual(fruitions, broken, "the data names the original");
            Assert.Throws<System.IO.InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.FruitionsFile ? broken : Fixtures.ReadDataFile(name)));
        }

        // ---- 8.9 The data ----

        [Test]
        public void TheRefugeOfUnity_IsTheOrthodoxWoodsSubstitution_AndBackToNoonJoinsItsCentre()
        {
            Assert.IsTrue(Ability("orthodox-wood", "unity-refuge").Substitute);
            Assert.IsTrue(FoundationRules.IsOfTheCentre(Ability("orthodox-wood", "back-to-noon")));
        }

        [TestCase("wandering-metal", "Lian")]
        [TestCase("orthodox-wood", "Huyan")]
        public void AFamilysLineage_IsHeldByIt_FromTheStart(string lineage, string family)
        {
            var w = new TestWorld();
            w.Fruitions.DrawWorld(new System.Random(42));
            var state = w.Fruitions.State(lineage);
            Assert.AreEqual(FruitionStatus.Occupied, state.Status);
            StringAssert.Contains(family, state.Holder);
        }

        [Test]
        public void TheHiddenMetalsFoundingQi_IsTheDeepLockMetalQi()
        {
            Assert.IsTrue(Content.Qi.Any(q => q.Name == "Qi Métallique du Verrou Profond" && q.Foundation == "hidden-metal:silver-pillar"));
        }

        [TestCase("gathered-wood")]   // two Devil Monarchs: a group comes back to nobody's body
        [TestCase("nourishing-fire")] // the Master of Douxuan, an Immortal Lord, reclaims no one
        public void AGroupOrAnImmortalLord_NeverReclaimsAFruition(string lineage)
        {
            Assert.IsFalse(Lineage(lineage).FormerHoldersReturn);
            Assert.IsTrue(Lineage("orthodox-water").FormerHoldersReturn, "Yu may come back");
        }
    }
}
