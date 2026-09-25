using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The registry of Dao lineages (fruitions.json, LORE.md §6) and the foundation each Qi builds
    /// (qi.json): checked at load, faithful to the lore.
    /// </summary>
    [TestFixture]
    public class FruitionContentTests
    {
        private static FruitionDefinition Fruition(string name) => Fixtures.Content.Fruitions.Single(f => f.Name == name);

        private static void AssertRefused(string file, string replacement)
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == file ? replacement : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(file, error.Message);
        }

        private static string FruitionsWith(Action<JObject> edit)
        {
            var file = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.FruitionsFile));
            edit(file);
            return file.ToString();
        }

        private static JObject FruitionJson(JObject file, string id) =>
            (JObject)((JArray)file["fruitions"]).Single(f => (string)f["id"] == id);

        // ---- Refused at load ----

        [Test]
        public void Load_Refuses_TwoFruitionsWithTheSameId()
        {
            AssertRefused(GameContentLoader.FruitionsFile, FruitionsWith(f =>
                ((JArray)f["fruitions"]).Add(FruitionJson(f, "orthodox-water").DeepClone())));
        }

        [Test]
        public void Load_Refuses_AFruitionWithoutItsFiveOrthodoxAbilities()
        {
            AssertRefused(GameContentLoader.FruitionsFile, FruitionsWith(f =>
                ((JArray)FruitionJson(f, "orthodox-water")["abilities"]).RemoveAt(0)));
        }

        [Test]
        public void Load_Refuses_AnOccupiedFruitionWithoutAHolder()
        {
            AssertRefused(GameContentLoader.FruitionsFile, FruitionsWith(f => FruitionJson(f, "mutable-water")["holder"] = null));
        }

        [Test]
        public void Load_Refuses_AFreeFruitionWithAHolder()
        {
            AssertRefused(GameContentLoader.FruitionsFile, FruitionsWith(f => FruitionJson(f, "orthodox-water")["holder"] = "Quelqu'un"));
        }

        [Test]
        public void Load_Refuses_AQiLeadingToAnUnknownFoundation()
        {
            var qi = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.QiFile));
            qi.Single(q => (string)q["id"] == "clear-spring-qi")["foundation"] = "orthodox-water:nowhere";
            AssertRefused(GameContentLoader.QiFile, qi.ToString());
        }

        [Test]
        public void Load_Refuses_AQiOfAFoundationMethodWithoutAFoundation()
        {
            // LORE.md §5.3.1: the Foundation fuses the chakras into the foundation of one's Qi
            var qi = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.QiFile));
            qi.Single(q => (string)q["id"] == "clear-spring-qi")["foundation"] = null;
            AssertRefused(GameContentLoader.QiFile, qi.ToString());
        }

        [Test]
        public void Load_Refuses_FruitionOddsThatDoNotSumToOne()
        {
            var balance = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.BalanceFile));
            balance["unspecifiedFruitionOdds"] = new JObject { ["free"] = 0.6, ["occupied"] = 0.6, ["broken"] = 0.1 };
            AssertRefused(GameContentLoader.BalanceFile, balance.ToString());
        }

        // ---- Faithful to the lore ----

        [Test]
        public void ShippedFruitions_HoldTheTwoDualities()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Yang Suprême", "Yang Lumineux", "Yang Mineur", "Yin Suprême", "Yin Voilé", "Yin Mineur" },
                Fixtures.Content.Fruitions.Where(f => f.Groups.Contains(FruitionGroup.TwoDualities)).Select(f => f.Name));
        }

        [Test]
        public void ShippedFruitions_CrossEveryElementWithEveryManifestation()
        {
            // LORE.md §6.3: the Five Virtues are the 25 crossings of element and manifestation
            var virtues = Fixtures.Content.Fruitions.Where(f => f.Groups.Contains(FruitionGroup.FiveVirtues)).ToList();
            var elements = new[] { Element.Wood, Element.Fire, Element.Earth, Element.Metal, Element.Water };
            var manifestations = new[] { Manifestation.Orthodox, Manifestation.Gathered, Manifestation.Nourishing, Manifestation.Mutable, Manifestation.Hidden };
            foreach (var e in elements)
                foreach (var m in manifestations)
                    Assert.AreEqual(1, virtues.Count(f => f.Element == e && f.Manifestation == m), $"{e} × {m}");
        }

        [Test]
        public void ShippedFruitions_OrderTheTwelveQiByTheirCycle()
        {
            var twelve = Fixtures.Content.Fruitions.Where(f => f.Groups.Contains(FruitionGroup.TwelveQi)).OrderBy(f => f.CycleIndex).Select(f => f.Name);
            CollectionAssert.AreEqual(new[]
            {
                "Qi Pur", "Qi Véritable", "Qi Brillant", "Qi Violet", "Qi Sec", "Rite Supérieur",
                "Rite Inférieur", "Qi Froid", "Qi Faste", "Qi Funeste", "Qi Profond", "Qi Exilé"
            }, twelve);
        }

        [Test]
        public void ShippedFruitions_HoldTheTenLineagesOfTheAncientFusion()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                "Régent Céleste", "Culture de Linxi", "Sentinelle de la Cité", "Élixir Parfait", "Corps de Kui",
                "Grand Chaman", "Chouette des Seuils", "Jade Premier", "Rite Équilibré", "Proclamation Céladon"
            }, Fixtures.Content.Fruitions.Where(f => f.Groups.Contains(FruitionGroup.AncientFusion)).Select(f => f.Name));
            CollectionAssert.AreEquivalent(new[] { "Grand Chaman", "Chouette des Seuils", "Jade Premier" },
                Fixtures.Content.Fruitions.Where(f => f.IsShaman).Select(f => f.Name));
        }

        [Test]
        public void ShippedFruitions_EachHaveFiveOrthodoxAbilities()
        {
            Assert.IsTrue(Fixtures.Content.Fruitions.All(f => f.Abilities.Count(a => !a.Substitute) >= 5));
        }

        [TestCase("Eau Muable", FruitionStatus.Occupied, "Tan Qing")]
        [TestCase("Yang Lumineux", FruitionStatus.Occupied, "Empereur Zhen Guangling")]
        [TestCase("Eau Orthodoxe", FruitionStatus.Free, null)]
        [TestCase("Qi Sec", FruitionStatus.Free, null)]
        [TestCase("Feu Nourricier", FruitionStatus.Broken, null)]
        [TestCase("Métal Caché", FruitionStatus.Broken, null)]
        [TestCase("Qi Violet", FruitionStatus.Hidden, null)]
        [TestCase("Qi Exilé", FruitionStatus.Suspected, "Marquis de la Nuit")]
        [TestCase("Feu Orthodoxe", FruitionStatus.Unspecified, null)]
        public void ShippedFruitions_KeepTheStateOfTheWorld(string name, FruitionStatus status, string holder)
        {
            // LORE.md §6.8
            var fruition = Fruition(name);
            Assert.AreEqual(status, fruition.Status);
            Assert.AreEqual(holder, fruition.Holder);
        }

        [TestCase("clear-spring-qi", "Eau Orthodoxe", "Mer sans Rivage")]
        [TestCase("upstream-brook-qi", "Eau Orthodoxe", "Adieu au Fleuve")]
        [TestCase("silent-tide-qi", "Eau Muable", "Voile de Brouillard")]
        [TestCase("night-frost-qi", "Yin Suprême", "Lune Noyée")]
        [TestCase("clear-edge-qi", "Métal Muable", "Stèle Gravée")]
        [TestCase("ashen-lightning-qi", "Tonnerre Céleste", "Bassin d'Orage")]
        [TestCase("winter-wind-qi", "Qi Froid", "Givre sur les Cèdres")]
        [TestCase("seven-terraces-qi", "Proclamation Céladon", "Bélier des Profondeurs")]
        [TestCase("court-frost-armour-qi", "Jade Premier", "Cour du Jade Général")]
        [TestCase("ember-phoenix-qi", "Feu Nourricier", "Faisan de Braise")]
        public void ShippedQi_BuildTheFoundationTheWikiNames(string qiId, string fruition, string foundation)
        {
            var qi = Fixtures.Content.Qi.Single(q => q.Id == qiId);
            var (fruitionId, abilityId) = FoundationRef.Parse(qi.Foundation);
            var f = Fixtures.Content.Fruitions.Single(x => x.Id == fruitionId);
            Assert.AreEqual(fruition, f.Name);
            Assert.AreEqual(foundation, f.Abilities.Single(a => a.Id == abilityId).Name);
        }
    }
}
