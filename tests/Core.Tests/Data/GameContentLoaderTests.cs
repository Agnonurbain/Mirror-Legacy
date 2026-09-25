using System;
using System.IO;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>The loader reads the six data files and refuses content the game could not play.</summary>
    [TestFixture]
    public class GameContentLoaderTests
    {
        /// <summary>The shipped files, with one of them replaced.</summary>
        private static Func<string, string> ShippedWith(string file, string replacement) =>
            name => name == file ? replacement : Fixtures.ReadDataFile(name);

        private static void AssertRefused(string file, string replacement)
        {
            var error = Assert.Throws<InvalidDataException>(() => GameContentLoader.Load(ShippedWith(file, replacement)));
            StringAssert.Contains(file, error.Message);
        }

        [Test]
        public void Load_ReadsEverySection()
        {
            var content = GameContentLoader.Load(Fixtures.ReadDataFile);
            Assert.IsTrue(content.Clan != null && content.Names != null && content.Balance != null
                && content.Factions.Count > 0 && content.RandomEvents.Count > 0 && content.StoryEvents.Count > 0);
        }

        [Test]
        public void Load_NamesTheFileAtFault_WhenTheJsonIsBroken()
        {
            AssertRefused(GameContentLoader.EventsFile, "[ {");
        }

        [Test]
        public void Load_NamesTheFileAtFault_WhenItIsMissing()
        {
            AssertRefused(GameContentLoader.StoryFile, null);
        }

        [TestCase("[]")]
        [TestCase("{ \"clanName\": \"Mo\", \"founders\": [ { \"firstName\": \"A\", \"role\": \"Child\" } ] }")]
        [TestCase("{ \"clanName\": \"Mo\", \"founders\": [ { \"firstName\": \"A\", \"role\": \"Patriarch\" }, { \"firstName\": \"B\", \"role\": \"Patriarch\" } ] }")]
        public void Load_Refuses_AClanWithoutExactlyOnePatriarch(string clan)
        {
            AssertRefused(GameContentLoader.ClanFile, clan);
        }

        [Test]
        public void Load_Refuses_AClanWithoutName()
        {
            AssertRefused(GameContentLoader.ClanFile, "{ \"founders\": [ { \"firstName\": \"A\", \"role\": \"Patriarch\" } ] }");
        }

        [Test]
        public void Load_Refuses_OddsOutsideZeroToOne()
        {
            AssertRefused(GameContentLoader.BalanceFile,
                "{ \"orificeOdds\": { \"commoner\": 1.5, \"oneParent\": 0.35, \"twoParents\": 0.5 }, \"annualBirthChance\": 0.25, \"minMotherAge\": 16, \"maxMotherAge\": 45, \"annualMarriageChance\": 0.3 }");
        }

        [Test]
        public void Load_Refuses_AnInvertedMotherhoodWindow()
        {
            AssertRefused(GameContentLoader.BalanceFile,
                "{ \"orificeOdds\": { \"commoner\": 0.003, \"oneParent\": 0.35, \"twoParents\": 0.5 }, \"annualBirthChance\": 0.25, \"minMotherAge\": 45, \"maxMotherAge\": 16, \"annualMarriageChance\": 0.3 }");
        }

        [Test]
        public void Load_Refuses_AnEmptyNamePool()
        {
            AssertRefused(GameContentLoader.NamesFile, "{ \"male\": [], \"female\": [ \"Mei\" ], \"outsiderFamilies\": [ \"Wang\" ] }");
        }

        [Test]
        public void Load_Refuses_AFactionWithoutName()
        {
            AssertRefused(GameContentLoader.FactionsFile, "[ { \"personality\": \"Merchant\", \"powerLevel\": 100 } ]");
        }

        [Test]
        public void Load_Refuses_AnEventThatCanNeverBeDrawn()
        {
            AssertRefused(GameContentLoader.EventsFile, "[ { \"name\": \"Jamais\", \"eventType\": \"PeacefulYear\", \"weight\": 0 } ]");
        }

        [Test]
        public void Load_Refuses_AStoryChoiceNamingAnUnknownFaction()
        {
            AssertRefused(GameContentLoader.StoryFile,
                "[ { \"triggerType\": \"FirstFoundation\", \"name\": \"Visite\", \"choices\": [ { \"label\": \"Recevoir\", \"outcome\": { \"factionName\": \"Personne\", \"relationChange\": 5 } } ] } ]");
        }

        [Test]
        public void Load_Refuses_AStoryEventWithoutChoices()
        {
            AssertRefused(GameContentLoader.StoryFile, "[ { \"triggerType\": \"FirstFoundation\", \"name\": \"Sans choix\", \"choices\": [] } ]");
        }
    }
}
