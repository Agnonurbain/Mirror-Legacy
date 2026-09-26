using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>
    /// The map of the world (regions.json, LORE.md §7): states, seas, regions and places with their relative
    /// positions and neighbours, checked at load and faithful to the renamed geography.
    /// </summary>
    [TestFixture]
    public class RegionContentTests
    {
        private static RegionDefinition Region(string id) => Fixtures.Content.Regions.Single(r => r.Id == id);

        private static string RegionsWith(Action<JArray> edit)
        {
            var file = JArray.Parse(Fixtures.ReadDataFile(GameContentLoader.RegionsFile));
            edit(file);
            return file.ToString();
        }

        private static JObject RegionJson(JArray file, string id) => (JObject)file.Single(r => (string)r["id"] == id);

        private static void AssertRefused(string file, string replacement)
        {
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == file ? replacement : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(file, error.Message);
        }

        // ---- Faithful to the lore ----

        [Test]
        public void ShippedRegions_HoldTheStatesAndSeasOfTheMap()
        {
            // LORE.md §7.1
            CollectionAssert.IsSubsetOf(new[]
            {
                "Royaume de Linxi", "Empire de Kun", "Principauté de Tai", "Royaume de Hanshan", "Duché de Pei",
                "Terres chamaniques de Nanwu", "Mer Orientale", "Mer du Nord", "Steppes de l'Ouest"
            }, Fixtures.Content.Regions.Where(r => r.ParentId == null).Select(r => r.Name));
        }

        [Test]
        public void ShippedRegions_PlaceTheClanOnLakeJingshui_InLinxi()
        {
            var home = Region(Fixtures.Content.Clan.HomeRegion);
            Assert.AreEqual("Lac Jingshui", home.Name);
            Assert.AreEqual("linxi", home.ParentId);
        }

        [TestCase("heshan", "jingshui-lake")]           // the Ruan, direct neighbours east of the lake
        [TestCase("qingyan-mountains", "jingshui-lake")] // south of the lake, under the Fox's pact
        [TestCase("wuyang", "jingshui-lake")]            // west of the lake
        public void ShippedRegions_KeepTheNeighboursOfTheLake(string region, string neighbour)
        {
            CollectionAssert.Contains(Region(region).Neighbours, neighbour);
        }

        [Test]
        public void ShippedRegions_AreNeighboursBothWays()
        {
            foreach (var region in Fixtures.Content.Regions)
                foreach (var neighbour in region.Neighbours)
                    CollectionAssert.Contains(Region(neighbour).Neighbours, region.Id, $"{region.Id} ↔ {neighbour}");
        }

        [Test]
        public void ShippedRegions_ArePlacedOnTheMap()
        {
            Assert.IsTrue(Fixtures.Content.Regions.All(r => r.X >= 0 && r.X <= 1 && r.Y >= 0 && r.Y <= 1));
        }

        [Test]
        public void ShippedRegions_SayTheirPositionsAreInterpreted()
        {
            // the lore gives relative positions, not coordinates: every position is an interpretation to refine
            Assert.IsTrue(Fixtures.Content.Regions.All(r => r.InterpretedFields.Contains("X") && r.InterpretedFields.Contains("Y")));
        }

        [Test]
        public void GapsReport_ListsTheRegions()
        {
            Assert.IsTrue(ContentGaps.Report(Fixtures.Content).Any(line => line.StartsWith(GameContentLoader.RegionsFile)));
        }

        // ---- Refused at load ----

        [Test]
        public void Load_Refuses_TwoRegionsWithTheSameId()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => f.Add(RegionJson(f, "heshan").DeepClone())));
        }

        [Test]
        public void Load_Refuses_AnUnknownNeighbour()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => ((JArray)RegionJson(f, "heshan")["neighbours"]).Add("nowhere")));
        }

        [Test]
        public void Load_Refuses_ANeighbourThatDoesNotAnswer()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f =>
            {
                var lake = (JArray)RegionJson(f, "jingshui-lake")["neighbours"];
                lake.Remove(lake.Single(n => (string)n == "heshan"));
            }));
        }

        [Test]
        public void Load_Refuses_AnUnknownParent()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => RegionJson(f, "heshan")["parentId"] = "atlantis"));
        }

        [Test]
        public void Load_Refuses_APositionOffTheMap()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => RegionJson(f, "heshan")["x"] = 1.5));
        }

        [Test]
        public void Load_Refuses_AClanWithoutAKnownHome()
        {
            var clan = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.ClanFile));
            clan["homeRegion"] = "atlantis";
            AssertRefused(GameContentLoader.ClanFile, clan.ToString());
        }
    }
}
