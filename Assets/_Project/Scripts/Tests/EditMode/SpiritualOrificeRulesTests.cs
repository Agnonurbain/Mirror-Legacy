using System;
using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    /// <summary>
    /// Hereditary spiritual orifice, mortals and Talisman Seeds (LORE.md §4, decision D3, §11.5).
    /// </summary>
    [TestFixture]
    public class SpiritualOrificeRulesTests
    {
        private static CharacterData Cultivator(CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 1)
        {
            return new CharacterData { Realm = realm, RealmStage = stage, HasSpiritualOrifice = true, OrificeKnown = true };
        }

        [TestCase(0, 0.003)]
        [TestCase(1, 0.35)]
        [TestCase(2, 0.5)]
        public void OrificeChance_RisesWithParentsWhoHaveOne(int parents, double expected)
        {
            Assert.AreEqual(expected, SpiritualOrificeRules.OrificeChance(parents), 1e-9);
        }

        [TestCase(0, 0.002, true)]
        [TestCase(0, 0.004, false)]
        [TestCase(1, 0.34, true)]
        [TestCase(1, 0.36, false)]
        [TestCase(2, 0.49, true)]
        [TestCase(2, 0.51, false)]
        public void HasOrificeAtBirth_ComparesRollToChance(int parents, double roll, bool expected)
        {
            Assert.AreEqual(expected, SpiritualOrificeRules.HasOrificeAtBirth(parents, roll));
        }

        [Test]
        public void CountParentsWithOrifice_IgnoresMissingParents()
        {
            Assert.AreEqual(1, SpiritualOrificeRules.CountParentsWithOrifice(Cultivator(), null));
        }

        [Test]
        public void CanCultivate_ReturnsFalse_WhenHumanMortal()
        {
            Assert.IsFalse(SpiritualOrificeRules.CanCultivate(new CharacterData()));
        }

        [Test]
        public void CanCultivate_ReturnsTrue_WhenTalismanSeedReplacesOrifice()
        {
            Assert.IsTrue(SpiritualOrificeRules.CanCultivate(new CharacterData { HasTalismanSeed = true }));
        }

        [Test]
        public void CanCultivate_ReturnsTrue_WhenSpiritBeastAwakensNaturally()
        {
            Assert.IsTrue(SpiritualOrificeRules.CanCultivate(new CharacterData { Species = Species.SpiritBeast }));
        }

        [TestCase(CultivationRealm.Embryonic, 4, false)]
        [TestCase(CultivationRealm.Embryonic, 5, true)]
        [TestCase(CultivationRealm.QiRefinement, 1, true)]
        public void CanDetectOrifice_NeedsTheSummitEyeChakra(CultivationRealm realm, int stage, bool expected)
        {
            Assert.AreEqual(expected, SpiritualOrificeRules.CanDetectOrifice(Cultivator(realm, stage)));
        }

        [TestCase(0.0, 60)]
        [TestCase(0.5, 70)]
        [TestCase(0.999, 80)]
        public void MortalLifespan_StaysBetweenSixtyAndEighty(double roll, int expected)
        {
            Assert.AreEqual(expected, SpiritualOrificeRules.MortalLifespan(roll));
        }

        [Test]
        public void CanReceiveTalismanSeed_ReturnsTrue_WhenMortalAndCapacityLeft()
        {
            Assert.IsTrue(SpiritualOrificeRules.CanReceiveTalismanSeed(new CharacterData(), activeSeeds: 1, capacity: 2));
        }

        [Test]
        public void CanReceiveTalismanSeed_ReturnsFalse_WhenCapacityIsFull()
        {
            Assert.IsFalse(SpiritualOrificeRules.CanReceiveTalismanSeed(new CharacterData(), activeSeeds: 2, capacity: 2));
        }

        [Test]
        public void CanReceiveTalismanSeed_ReturnsFalse_WhenAlreadyHasOrifice()
        {
            Assert.IsFalse(SpiritualOrificeRules.CanReceiveTalismanSeed(Cultivator(), activeSeeds: 0, capacity: 2));
        }

        [TestCase(0, 2)]
        [TestCase(3, 5)]
        public void TalismanSeedCapacity_GrowsWithMirrorFragments(int fragments, int expected)
        {
            Assert.AreEqual(expected, SpiritualOrificeRules.TalismanSeedCapacity(fragments));
        }

        [Test]
        public void Normalize_GivesKnownOrifice_WhenOldSaveHasACultivator()
        {
            var c = new CharacterData { Realm = CultivationRealm.QiRefinement, RealmStage = 3 };
            SpiritualOrificeRules.Normalize(c);
            Assert.IsTrue(c.HasSpiritualOrifice && c.OrificeKnown);
        }

        [Test]
        public void RevealOrifices_MarksEveryoneKnown_WhenADetectorIsPresent()
        {
            var child = new CharacterData { HasSpiritualOrifice = true };
            var mortal = new CharacterData();
            int revealed = SpiritualOrificeRules.RevealOrifices(new List<CharacterData> { Cultivator(), child, mortal });
            Assert.IsTrue(revealed == 2 && child.OrificeKnown && mortal.OrificeKnown);
        }

        [Test]
        public void RevealOrifices_ChangesNothing_WhenNoDetector()
        {
            var child = new CharacterData { HasSpiritualOrifice = true };
            int revealed = SpiritualOrificeRules.RevealOrifices(new List<CharacterData> { child, Cultivator(CultivationRealm.Embryonic, 2) });
            Assert.IsTrue(revealed == 0 && !child.OrificeKnown);
        }
    }
}
