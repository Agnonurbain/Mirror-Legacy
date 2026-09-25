using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The technique and Qi catalogs (techniques.json, qi.json) and the clan's starting knowledge are
    /// checked at load: a technique the rules of LORE.md §2 could not play is refused, naming its file.
    /// </summary>
    [TestFixture]
    public class TechniqueContentTests
    {
        private const string ClearSpring = "{ \"id\": \"t\", \"name\": \"T\", \"kind\": \"Cultivation\", \"grade\": 3, \"category\": \"Common\", \"requiredRealm\": \"QiRefinement\", \"requiredQiId\": \"clear-spring-qi\" }";

        private static void AssertRefused(string file, string replacement)
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == file ? replacement : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(file, error.Message);
        }

        /// <summary>The shipped techniques.json with its technique list replaced.</summary>
        private static string TechniquesWith(string techniquesArray)
        {
            var catalog = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.TechniquesFile));
            catalog["techniques"] = JArray.Parse(techniquesArray);
            return catalog.ToString();
        }

        /// <summary>The shipped clan.json, changed by the given edit.</summary>
        private static string ClanWith(Action<JObject> edit)
        {
            var clan = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.ClanFile));
            edit(clan);
            return clan.ToString();
        }

        [Test]
        public void Load_ReadsTheTechniqueAndQiCatalogs()
        {
            var content = GameContentLoader.Load(Fixtures.ReadDataFile);
            Assert.IsTrue(content.Techniques.Count > 0 && content.Qi.Count > 0 && content.DeductionNames != null);
        }

        [Test]
        public void Load_AcceptsAMinimalCatalog()
        {
            Assert.DoesNotThrow(() => GameContentLoader.Load(name => name == GameContentLoader.TechniquesFile
                ? TechniquesWith($"[ {ClearSpring} ]")
                : name == GameContentLoader.ClanFile
                    ? ClanWith(c => { c["startingTechniques"] = new JArray("t"); foreach (var f in c["founders"]) f["cultivationMethod"] = "t"; })
                    : Fixtures.ReadDataFile(name)));
        }

        [TestCase(0)]
        [TestCase(8)]
        public void Load_Refuses_AGradeOutsideOneToSeven(int grade)
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("\"grade\": 3", $"\"grade\": {grade}")} ]"));
        }

        [Test]
        public void Load_Refuses_TwoTechniquesWithTheSameId()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring}, {ClearSpring} ]"));
        }

        [Test]
        public void Load_Refuses_AQiMethodWithoutItsQi()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("\"requiredQiId\": \"clear-spring-qi\"", "\"requiredQiId\": null")} ]"));
        }

        [Test]
        public void Load_Refuses_AMethodNamingAnUnknownQi()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("clear-spring-qi", "nowhere-qi")} ]"));
        }

        [Test]
        public void Load_Refuses_AnArtThatNeedsAQi()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("\"Cultivation\"", "\"Weapon\"")} ]"));
        }

        [Test]
        public void Load_Refuses_AMethodWhoseSupremeRealmIsBelowItsFirst()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("\"grade\": 3", "\"grade\": 3, \"supremeRealm\": \"Embryonic\"")} ]"));
        }

        [Test]
        public void Load_Refuses_AnAncestralMethodWhoseQiCanStillBeHarvested()
        {
            // LORE.md §2.3: a method is ancestral because its Qi has vanished
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("\"Common\"", "\"Ancestral\"")} ]"));
        }

        [Test]
        public void Load_Refuses_ACommonMethodWhoseQiHasVanished()
        {
            AssertRefused(GameContentLoader.TechniquesFile, TechniquesWith($"[ {ClearSpring.Replace("clear-spring-qi", "red-dust-court-qi")} ]"));
        }

        [Test]
        public void Load_Refuses_AFlawCounteredByAnUnknownTechnique()
        {
            AssertRefused(GameContentLoader.TechniquesFile,
                TechniquesWith($"[ {ClearSpring.Replace("\"grade\": 3", "\"grade\": 3, \"flaws\": { \"counteredById\": \"nobody\" }")} ]"));
        }

        [Test]
        public void Load_Refuses_AQiThatCanNeverBeGathered()
        {
            var qi = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.QiFile));
            qi[0]["yearsPerPortion"] = 0;
            AssertRefused(GameContentLoader.QiFile, qi.ToString());
        }

        [Test]
        public void Load_Refuses_AStartingTechniqueTheCatalogLacks()
        {
            AssertRefused(GameContentLoader.ClanFile, ClanWith(c => c["startingTechniques"] = new JArray("lost-scroll")));
        }

        [Test]
        public void Load_Refuses_AStartingQiTheCatalogLacks()
        {
            AssertRefused(GameContentLoader.ClanFile, ClanWith(c => c["startingQi"] = new JObject { ["nowhere-qi"] = 1 }));
        }

        [Test]
        public void Load_Refuses_AQiCultivatorFounderWithoutAMethodForTheirRealm()
        {
            // LORE.md §5.2: nobody reaches Qi Cultivation without the matching method
            AssertRefused(GameContentLoader.ClanFile, ClanWith(c =>
            {
                foreach (var f in c["founders"])
                    if ((string)f["realm"] == "QiRefinement") f["cultivationMethod"] = null;
            }));
        }

        [Test]
        public void Load_Refuses_AFounderPractisingAMethodTheClanDoesNotKnow()
        {
            AssertRefused(GameContentLoader.ClanFile, ClanWith(c =>
            {
                foreach (var f in c["founders"])
                    if ((string)f["realm"] == "QiRefinement") f["cultivationMethod"] = "seven-terraces-canon";
            }));
        }

        [Test]
        public void Load_Refuses_ASpeedTableWithoutOneValuePerGrade()
        {
            var balance = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.BalanceFile));
            balance["techniqueSpeedByGrade"] = new JArray(1.0, 1.0);
            AssertRefused(GameContentLoader.BalanceFile, balance.ToString());
        }

        [Test]
        public void Load_Refuses_DeductionNamesMissingAKind()
        {
            var catalog = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.TechniquesFile));
            ((JObject)catalog["deductionNames"]["kinds"]).Remove("Weapon");
            AssertRefused(GameContentLoader.TechniquesFile, catalog.ToString());
        }
    }
}
