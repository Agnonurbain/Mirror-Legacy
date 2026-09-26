using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The powers of the world (factions.json, LORE.md §7-§10): the three sects and eight gates of Linxi, the
    /// lake's clans and the named families, the temples, orders and states — renamed, placed on the map, with
    /// the techniques the lore gives them.
    /// </summary>
    [TestFixture]
    public class FactionContentTests
    {
        private static FactionData Faction(string name) => Fixtures.Content.Factions.Single(f => f.Name == name);

        private static void AssertRefused(System.Action<JArray> edit)
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.FactionsFile));
            edit(file);
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.FactionsFile ? file.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.FactionsFile, error.Message);
        }

        private static JObject FactionJson(JArray file, string name) => (JObject)file.Single(f => (string)f["name"] == name);

        // ---- Faithful to the lore ----

        [Test]
        public void ShippedFactions_HoldTheThreeSectsAndEightGatesOfLinxi()
        {
            // LORE.md §7.2 (D5: eight gates, as on the map)
            var linxi = Fixtures.Content.Factions.Where(f => Fixtures.Content.Regions.Single(r => r.Id == f.RegionId).ParentId == "linxi").ToList();
            CollectionAssert.AreEquivalent(new[] { "Secte du Pic des Nuées", "Secte des Mille Lames", "Secte de la Lune Pâle" },
                linxi.Where(f => f.Kind == FactionKind.Sect).Select(f => f.Name));
            CollectionAssert.AreEquivalent(new[]
            {
                "Porte du Fer Ardent", "Porte du Roc Obscur", "Porte du Givre Blanc", "Porte de l'Épée Stellaire",
                "Porte des Brumes d'Ambre", "Porte du Chrysanthème Noir", "Porte des Saules Pleureurs", "Porte du Tambour Sacré"
            }, linxi.Where(f => f.Kind == FactionKind.Gate).Select(f => f.Name));
        }

        [Test]
        public void ShippedFactions_HoldTheNamedFamilies_ButNotThePlayersClan()
        {
            // LORE.md §8
            var families = Fixtures.Content.Factions.Where(f => f.Kind == FactionKind.Family).ToList();
            CollectionAssert.IsSubsetOf(new[] { "Ruan", "Gu", "Bai", "Zang", "Lü", "Tao", "Lou", "Xun", "Qiao", "Fang", "Qu" },
                families.Select(f => f.FamilyName));
            Assert.IsFalse(families.Any(f => f.FamilyName == Fixtures.Content.Clan.ClanName));
            Assert.IsTrue(families.All(f => f.Name == $"Famille {f.FamilyName}"));
        }

        [Test]
        public void ShippedFactions_InventNothing()
        {
            // the eight invented factions of the prototype are gone (LORE.md §7: ⚠️)
            var invented = new[] { "Famille Wang", "Guilde marchande Zhao", "Secte des Nuées Célestes", "Salle du Poing de Fer",
                "Pavillon du Phénix de Jade", "Secte du Voile d'Ombre", "Ermitage du Bambou Verdoyant", "Empire du Soleil d'Or" };
            Assert.IsFalse(Fixtures.Content.Factions.Any(f => invented.Contains(f.Name)));
        }

        [TestCase("Famille Lou", "jingshui-lake")]      // the five clans of the lake: Lou, Fang, Mo, Lü, Tao
        [TestCase("Famille Fang", "jingshui-lake")]
        [TestCase("Famille Lü", "jingshui-lake")]
        [TestCase("Famille Tao", "jingshui-lake")]
        [TestCase("Famille Ruan", "heshan")]
        [TestCase("Famille Gu", "grey-reed-plain")]
        [TestCase("Secte du Pic des Nuées", "mount-yunfeng")]
        [TestCase("Porte du Roc Obscur", "yunmen")]
        [TestCase("Porte du Fer Ardent", "fiery-iron-lands")]
        [TestCase("Porte du Givre Blanc", "white-frost-lands")]
        [TestCase("Secte de la Lune Pâle", "pale-moon-lands")]
        [TestCase("Atoll des Perles Noires", "black-pearl-atoll")]
        public void ShippedFactions_LiveWhereTheMapPutsThem(string name, string region)
        {
            Assert.AreEqual(region, Faction(name).RegionId);
        }

        [TestCase("Secte du Pic des Nuées", new[] { "night-frost-canon", "measured-rain-method", "dewdrop-pearl-method" })]
        [TestCase("Famille Gu", new[] { "upstream-brook-method", "seven-terraces-canon" })]
        [TestCase("Famille Ruan", new[] { "clear-spring-sutra", "upstream-brook-method" })]
        [TestCase("Porte du Fer Ardent", new[] { "clear-edge-method" })]
        [TestCase("Atoll des Perles Noires", new[] { "engulfed-blaze-art" })]
        public void ShippedFactions_HoldTheTechniquesTheLoreGivesThem(string name, string[] techniques)
        {
            // LORE.md §2.4
            CollectionAssert.IsSubsetOf(techniques, Faction(name).Techniques);
        }

        [Test]
        public void ShippedFactions_TheCloudPeakSect_IsTheStrongestOfLinxi_BackedByATrueMonarch()
        {
            // §5.5: every great sect is backed by at least one True Monarch; §7.2: the dominant sect
            var peak = Faction("Secte du Pic des Nuées");
            Assert.AreEqual(CultivationRealm.GoldenCore, peak.HighestRealm);
            var linxi = Fixtures.Content.Factions.Where(f => Fixtures.Content.Regions.Single(r => r.Id == f.RegionId).ParentId == "linxi");
            Assert.AreEqual(peak.PowerLevel, linxi.Max(f => f.PowerLevel));
        }

        [Test]
        public void ShippedFactions_OfOtherDaos_KeepTheirPath()
        {
            Assert.AreEqual(CultivationPath.Buddhist, Faction("Temple du Lotus").Path);
            Assert.AreEqual(CultivationPath.Shamanic, Faction("Terres chamaniques de Nanwu").Path);
        }

        [Test]
        public void GapsReport_ListsTheFactionsInterpretations()
        {
            Assert.IsTrue(ContentGaps.Report(Fixtures.Content).Any(line => line.StartsWith(GameContentLoader.FactionsFile)));
        }

        // ---- Refused at load ----

        [Test]
        public void Load_Refuses_AFactionInAnUnknownRegion()
        {
            AssertRefused(f => FactionJson(f, "Famille Ruan")["regionId"] = "atlantis");
        }

        [Test]
        public void Load_Refuses_AFactionWithAnUnknownTechnique()
        {
            AssertRefused(f => ((JArray)FactionJson(f, "Famille Ruan")["techniques"]).Add("no-such-technique"));
        }

        [Test]
        public void Load_Refuses_AFactionWithoutAListOfTechniques()
        {
            AssertRefused(f => FactionJson(f, "Famille Lou")["techniques"] = null);
        }

        [Test]
        public void Load_Refuses_TwoFactionsWithTheSameName()
        {
            AssertRefused(f => f.Add(FactionJson(f, "Famille Ruan").DeepClone()));
        }
    }
}
