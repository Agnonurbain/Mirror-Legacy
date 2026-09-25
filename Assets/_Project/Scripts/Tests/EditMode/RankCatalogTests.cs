using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    /// <summary>
    /// Rank names per cultivation path and their asymmetric power equivalences (LORE.md §3.7, §5).
    /// </summary>
    [TestFixture]
    public class RankCatalogTests
    {
        private static CharacterData Make(CultivationPath path, CultivationRealm realm, int stage,
            CultivationSubPath subPath = CultivationSubPath.PurpleMansionGoldenCore)
        {
            return new CharacterData { Path = path, SubPath = subPath, Realm = realm, RealmStage = stage };
        }

        [Test]
        public void DisplayName_IsMortal_WhenExaminedWithoutOrifice()
        {
            var c = new CharacterData { OrificeKnown = true };
            Assert.AreEqual("Mortel", RankCatalog.DisplayName(c));
        }

        [Test]
        public void DisplayName_HidesTheOrifice_WhenNotYetExamined()
        {
            var c = new CharacterData { HasSpiritualOrifice = true };
            Assert.AreEqual("Orifice non examiné", RankCatalog.DisplayName(c));
        }

        [Test]
        public void DisplayName_NeverAsksForAnOrifice_WhenSpiritBeast()
        {
            var c = new CharacterData { Species = Species.SpiritBeast };
            Assert.AreEqual("Respiration Embryonnaire — sans chakra", RankCatalog.DisplayName(c));
        }

        [Test]
        public void DisplayName_ShowsNoChakraYet_WhenKnownOrificeBeforeFirstChakra()
        {
            var c = new CharacterData { HasSpiritualOrifice = true, OrificeKnown = true };
            Assert.AreEqual("Respiration Embryonnaire — sans chakra", RankCatalog.DisplayName(c));
        }

        [Test]
        public void DisplayName_ShowsChakraName_WhenImmortalEmbryonic()
        {
            var name = RankCatalog.DisplayName(Make(CultivationPath.Immortal, CultivationRealm.Embryonic, 1));
            Assert.AreEqual("Respiration Embryonnaire — Lac Intérieur", name);
        }

        [Test]
        public void DisplayName_ShowsLevelAndPhase_WhenImmortalQi()
        {
            var name = RankCatalog.DisplayName(Make(CultivationPath.Immortal, CultivationRealm.QiRefinement, 4));
            Assert.AreEqual("Culture du Qi — 4e niveau (milieu)", name);
        }

        [Test]
        public void DisplayName_ShowsPeak_WhenImmortalFoundationStageFour()
        {
            var name = RankCatalog.DisplayName(Make(CultivationPath.Immortal, CultivationRealm.Foundation, 4));
            Assert.AreEqual("Établissement des Fondations — apogée", name);
        }

        [Test]
        public void DisplayName_ShowsDaoistMasterTitle_WhenPurpleMansionLate()
        {
            var name = RankCatalog.DisplayName(Make(CultivationPath.Immortal, CultivationRealm.PurpleMansion, 3));
            Assert.AreEqual("Manoir Pourpre — fin (Grand Maître taoïste)", name);
        }

        [TestCase(CultivationRealm.QiRefinement, 3, "Moine")]
        [TestCase(CultivationRealm.Foundation, 2, "Maître Moine")]
        [TestCase(CultivationRealm.PurpleMansion, 1, "Miséricordieux")]
        [TestCase(CultivationRealm.PurpleMansion, 3, "Maha")]
        [TestCase(CultivationRealm.GoldenCore, 1, "Maître du Dharma")]
        [TestCase(CultivationRealm.DaoEmbryo, 1, "Vénérable")]
        public void DisplayName_UsesBuddhistRanks(CultivationRealm realm, int stage, string expected)
        {
            var c = Make(CultivationPath.Buddhist, realm, stage, CultivationSubPath.AncientBuddhism);
            Assert.AreEqual(expected, RankCatalog.DisplayName(c));
        }

        [TestCase(CultivationRealm.QiRefinement, "Manoir Divers")]
        [TestCase(CultivationRealm.Foundation, "Fournaise Unifiée")]
        public void DisplayName_UsesHeavenlyEmbryoDemonRanks(CultivationRealm realm, string expected)
        {
            var c = Make(CultivationPath.Devil, realm, 1, CultivationSubPath.HeavenlyEmbryoDemon);
            Assert.AreEqual(expected, RankCatalog.DisplayName(c));
        }

        [TestCase(CultivationRealm.QiRefinement, "Serviteur Divin")]
        [TestCase(CultivationRealm.PurpleMansion, "Serviteur Divin")]
        [TestCase(CultivationRealm.GoldenCore, "Noyau Divin")]
        public void DisplayName_UsesDivineRanks(CultivationRealm realm, string expected)
        {
            var c = Make(CultivationPath.Divine, realm, 1, CultivationSubPath.OwnedProfundity);
            Assert.AreEqual(expected, RankCatalog.DisplayName(c));
        }

        [Test]
        public void PowerOffset_PlacesMasterMonkSlightlyAboveFoundation()
        {
            var c = Make(CultivationPath.Buddhist, CultivationRealm.Foundation, 1, CultivationSubPath.AncientBuddhism);
            Assert.That(RankCatalog.PowerOffset(c), Is.GreaterThan(0f).And.LessThan(0.5f));
        }

        [Test]
        public void PowerOffset_PlacesMercifulOneSlightlyBelowPurpleMansion()
        {
            var c = Make(CultivationPath.Buddhist, CultivationRealm.PurpleMansion, 1, CultivationSubPath.ModernBuddhism);
            Assert.That(RankCatalog.PowerOffset(c), Is.LessThan(0f));
        }

        [Test]
        public void PowerOffset_PlacesMahaBetweenPurpleMansionAndGoldenCore()
        {
            var c = Make(CultivationPath.Buddhist, CultivationRealm.PurpleMansion, 3, CultivationSubPath.ModernBuddhism);
            Assert.That(RankCatalog.PowerOffset(c), Is.GreaterThanOrEqualTo(0.5f).And.LessThan(1f));
        }

        [Test]
        public void PowerOffset_IsZero_WhenImmortalPath()
        {
            Assert.AreEqual(0f, RankCatalog.PowerOffset(Make(CultivationPath.Immortal, CultivationRealm.Foundation, 2)));
        }
    }
}
