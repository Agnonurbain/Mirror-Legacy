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

        // ---- Placed after the wiki's map of the region (2026-09-26, reference only: D1) ----

        private static bool West(string a, string b) => Region(a).X < Region(b).X;
        private static bool North(string a, string b) => Region(a).Y < Region(b).Y;

        [Test]
        public void ShippedRegions_FollowTheWikiMap_AroundTheLake()
        {
            Assert.IsTrue(West("jingshui-lake", "wuyang"), "Wuyang lies east of the lake");
            Assert.IsTrue(North("jingshui-lake", "beast-abyss"), "the Beast Abyss lies south of the lake");
            Assert.IsTrue(North("jingshui-lake", "qingyan-mountains"), "the Qingyan Mountains lie south of the lake");
            Assert.IsTrue(West("mount-fengxi", "jingshui-lake"), "Mount Fengxi lies south-west of the lake");
            Assert.IsTrue(West("jingshui-lake", "heshan"), "Heshan, the Ruan's, lies east of the lake");
        }

        [Test]
        public void ShippedRegions_FollowTheWikiMap_AcrossLinxi()
        {
            Assert.IsTrue(North("siwei", "haiyan") && North("mount-yunfeng", "haiyan"), "Haiyan lies far south, by the sea");
            Assert.IsTrue(West("qianshi", "mount-songhe"), "Mount Songhe lies east of Qianshi");
            Assert.IsTrue(West("liuhe", "singing-sands") && West("singing-sands", "jingshui-lake"), "the Thousand Blades' lands lie west");
            Assert.IsTrue(North("mount-jianfeng", "fiery-iron-lands"), "the Fiery Iron Gate lies below Mount Jianfeng");
            Assert.IsTrue(North("nanling", "southern-marches") || Region("nanling").Y > 0.85, "Nanling lies at the south edge of Linxi");
        }

        [Test]
        public void ShippedRegions_InventNoName()
        {
            // the seats the lore leaves unnamed take their gate's or sect's name, as the map does
            Assert.IsFalse(Fixtures.Content.Regions.Any(r => r.InterpretedFields.Contains("Name")));
        }

        [Test]
        public void ShippedRegions_OfTheWikiMap_SayWhereTheyComeFrom()
        {
            Assert.AreEqual(Provenance.Wiki, Region("wuyang").Provenance);
            Assert.IsFalse(Region("wuyang").InterpretedFields.Contains("X"));
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
        public void ShippedRegions_OffTheWikiMap_SayTheirPositionsAreInterpreted()
        {
            // the lands beyond the wiki's map keep positions guessed from the lore's relative geography
            Assert.IsTrue(Fixtures.Content.Regions.Where(r => r.Provenance != Provenance.Wiki)
                .All(r => r.InterpretedFields.Contains("X") && r.InterpretedFields.Contains("Y")));
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
        public void Load_Refuses_ARegionWithoutAListOfNeighbours()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f =>
            {
                RegionJson(f, "shiyuan")["neighbours"] = null;
                foreach (var region in f) // no border left pointing at it: only the missing list is wrong
                {
                    if (region["neighbours"] is not JArray neighbours) continue;
                    var back = neighbours.FirstOrDefault(n => (string)n == "shiyuan");
                    if (back != null) neighbours.Remove(back);
                }
            }));
        }

        [Test]
        public void Load_Refuses_ARegionBorderingItself()
        {
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => ((JArray)RegionJson(f, "heshan")["neighbours"]).Add("heshan")));
        }

        [Test]
        public void Load_Refuses_AParentThatIsNotAStateOrSea()
        {
            // the map has one level: places inside states and seas
            AssertRefused(GameContentLoader.RegionsFile, RegionsWith(f => RegionJson(f, "heshan")["parentId"] = "jingshui-lake"));
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
