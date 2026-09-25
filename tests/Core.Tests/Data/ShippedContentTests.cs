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

        [Test]
        public void ShippedEvents_CoverEveryKindOfRandomEvent()
        {
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(RandomEventType)),
                Fixtures.Content.RandomEvents.Select(e => e.EventType).Distinct());
        }
    }
}
