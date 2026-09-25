using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The content the game ships respects the lore's decisions and never uses a proper noun of the
    /// source novel (CLAUDE.md; the renaming lexicon of LORE.md §13 is the only place they appear).
    /// </summary>
    [TestFixture]
    public class ShippedContentTests
    {
        /// <summary>Source-novel names specific enough to never appear by accident (LORE.md §13, left column).</summary>
        private static readonly string[] SourceNovelNames =
        {
            "Xiangping", "Mutian", "Jiangqian", "Dali", "Xiping", "Milin", "Lixia", "Libu", "Qiong", "Shanji",
            "Jiachuan", "Yufu", "Quanwu", "Linghai", "Dongli", "Helin", "Cangwu", "Hengdong", "Tongmo", "Sanlang",
            "Beiming", "Pingming", "Yincheng", "Shengle", "Yanyang", "Luoxia", "Changxiao", "Hengzhu", "Xiukui",
            "Wanyu", "Zhaoyuan", "Tuoba", "Qianyuan", "Xunquan", "Yingze", "Beiyao", "Xiyan", "Xiyang", "Xuantan",
            "Dongfang", "Qiyan", "Moongaze", "Regard Lunaire", "Azure Pond", "Étang d'Azur"
        };

        [Test]
        public void ShippedData_ContainsNoProperNounOfTheSourceNovel()
        {
            var found = GameContentLoader.Files
                .SelectMany(file => SourceNovelNames
                    .Where(name => Regex.IsMatch(Fixtures.ReadDataFile(file), $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase))
                    .Select(name => $"{file}: {name}"))
                .ToList();
            CollectionAssert.IsEmpty(found);
        }

        [Test]
        public void ShippedData_NeverUsesTheSourceNovelsFamilyName()
        {
            var content = Fixtures.Content;
            Assert.IsTrue(content.Clan.ClanName != "Li" && !content.Names.OutsiderFamilies.Contains("Li")
                && content.Factions.All(f => f.FamilyName != "Li"));
        }

        [Test]
        public void ShippedClan_IsTheMoClanOfTheLexicon()
        {
            Assert.AreEqual("Mo", Fixtures.Content.Clan.ClanName);
        }

        [Test]
        public void ShippedBalance_KeepsTheOrificeOddsOfDecisionD3()
        {
            var odds = Fixtures.Content.Balance.OrificeOdds; // ~3/1000 alone, ~30-50% with one gifted parent
            Assert.IsTrue(odds.Commoner <= 0.01 && odds.OneParent >= 0.30 && odds.OneParent <= 0.50
                && odds.TwoParents >= odds.OneParent);
        }

        [Test]
        public void ShippedStory_CoversEveryMilestoneTheGameRaises()
        {
            var raised = new[]
            {
                StoryTriggerType.FirstFoundation, StoryTriggerType.FirstGoldenCore, StoryTriggerType.PatriarchBetrayal,
                StoryTriggerType.FirstAscension, StoryTriggerType.ClanExtinctionThreat
            };
            CollectionAssert.IsSubsetOf(raised, Fixtures.Content.StoryEvents.Select(e => e.TriggerType));
        }

        /// <summary>The Qi cultivation methods of LORE.md §2.4, by their game names.</summary>
        private static readonly string[] LoreQiMethods =
        {
            "Pas du Phénix de Braise", "Sutra de la Source Claire", "Canon du Givre Nocturne", "Sutra de la Marée Silencieuse",
            "Méthode du Ruisseau Remonté", "Sutra du Verbe Premier", "Art secret de la Perle Grise", "Méthode de l'Averse Mesurée",
            "Méthode du Rempart d'Airain", "Méthode du Tranchant Clair", "Sutra du Cœur Tissé", "Méthode de la Source Souterraine",
            "Manuel du Soleil Intérieur", "Art du Brasier Englouti", "Méthode du Souffle Commun", "Canon des Sept Terrasses",
            "Art secret de l'Éclair Cendré", "Méthode de la Perle de Rosée", "Art du Grondement Lointain", "Méthode de l'Écorce Scellée",
            "Méthode des Vapeurs du Littoral", "Art du Murmure des Cèdres", "Méthode de la Bise Hivernale", "Veilleur du Sentier",
            "Méthode des Six Harmonies", "Méthode du Qi Limpide"
        };

        [Test]
        public void ShippedTechniques_HoldTheTwentySixQiMethodsOfTheLore()
        {
            var methods = Fixtures.Content.Techniques.Where(t => t.Kind == TechniqueKind.Cultivation).Select(t => t.Name).ToList();
            CollectionAssert.IsSubsetOf(LoreQiMethods, methods);
            Assert.AreEqual(26, LoreQiMethods.Length);
        }

        [Test]
        public void ShippedTechniques_KnowOnlyOneTrueImmortalManual()
        {
            // LORE.md §2.2: a single grade 7+ technique is known
            var grade7 = Fixtures.Content.Techniques.Where(t => t.Grade == 7).Select(t => t.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Dialogue de Gongye Shu avec le Pêcheur du Saule" }, grade7);
        }

        [Test]
        public void ShippedTechniques_KeepTheGradesOfTheLore()
        {
            var byName = Fixtures.Content.Techniques.ToDictionary(t => t.Name);
            Assert.AreEqual(3, byName["Sutra de la Source Claire"].Grade);
            Assert.AreEqual(2, byName["Méthode du Souffle Commun"].Grade);
            Assert.AreEqual(6, byName["Canon des Sept Terrasses"].Grade);
            Assert.AreEqual(5, byName["Intuition du Lotus Blanc"].Grade);
            Assert.AreEqual(TechniqueCategory.Secret, byName["Veilleur du Sentier"].Category);
            Assert.AreEqual(TechniqueCategory.Ancestral, byName["Sutra de l'Aîné qui Frappe à la Cour"].Category);
        }

        [Test]
        public void ShippedQi_KeepTheHarvestOfTheSevenTerraces()
        {
            // LORE.md §2.4: ten years of wisps, then ten years of refining
            var canon = Fixtures.Content.Techniques.Single(t => t.Name == "Canon des Sept Terrasses");
            var qi = Fixtures.Content.Qi.Single(q => q.Id == canon.RequiredQiId);
            Assert.AreEqual("Qi Profond du Bélier Variant", qi.Name);
            Assert.AreEqual(20, qi.YearsPerPortion);
        }

        [Test]
        public void ShippedClan_StartsWithTheClearSpringSutra()
        {
            // LORE.md §2.2: the Sutra de la Source Claire is the Mo clan's technique
            var clearSpring = Fixtures.Content.Techniques.Single(t => t.Name == "Sutra de la Source Claire");
            CollectionAssert.Contains(Fixtures.Content.Clan.StartingTechniques, clearSpring.ID);
            Assert.IsTrue(Fixtures.Content.Clan.Founders.Where(f => f.Realm >= CultivationRealm.QiRefinement)
                .All(f => f.CultivationMethod == clearSpring.ID));
        }

        [Test]
        public void ShippedEvents_CoverEveryKindOfRandomEvent()
        {
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(RandomEventType)),
                Fixtures.Content.RandomEvents.Select(e => e.EventType).Distinct());
        }
    }
}
