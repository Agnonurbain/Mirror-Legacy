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

        // ---- The trials of the power ladder (L4c: interpretations of L1-L2 moved into the data) ----

        private static void AssertBalanceRefused(System.Action<Newtonsoft.Json.Linq.JObject> edit)
        {
            var balance = Newtonsoft.Json.Linq.JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.BalanceFile));
            edit(balance);
            var error = Assert.Throws<System.IO.InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.BalanceFile ? balance.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.BalanceFile, error.Message);
        }

        [Test]
        public void Load_Refuses_ABalanceWithoutItsTrials()
        {
            AssertBalanceRefused(b => b.Remove("trials"));
        }

        [Test]
        public void Load_Refuses_AChakraChanceAboveAHundred()
        {
            AssertBalanceRefused(b => b["trials"]["chakraChances"]["InnerLakeChakra"] = 101);
        }

        [Test]
        public void Load_Refuses_ADeviationTableOutOfOrder()
        {
            // a minor failure up to the first roll, a major one up to the second, death beyond
            AssertBalanceRefused(b => b["trials"]["majorFailureMaxRoll"] = 50);
        }
    }
}
