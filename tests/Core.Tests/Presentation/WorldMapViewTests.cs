using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>What the world map screen shows (LORE.md §7): the places, their borders, the powers of each place.</summary>
    [TestFixture]
    public class WorldMapViewTests
    {
        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static MapPlace Place(GameSession s, string id) => WorldMapView.Places(s).Single(p => p.Id == id);

        [Test]
        public void Places_CoverTheWholeMap()
        {
            Assert.AreEqual(Fixtures.Content.Regions.Count, WorldMapView.Places(NewGame()).Count);
        }

        [Test]
        public void Places_MarkTheClansHome()
        {
            var home = WorldMapView.Places(NewGame()).Single(p => p.IsHome);
            Assert.AreEqual("jingshui-lake", home.Id);
        }

        [Test]
        public void Places_TellStatesAndSeasFromThePlacesInside()
        {
            var s = NewGame();
            Assert.IsTrue(Place(s, "linxi").IsState && !Place(s, "heshan").IsState);
        }

        [Test]
        public void Places_ListThePowersLivingThere()
        {
            CollectionAssert.AreEquivalent(new[] { "Famille Lou", "Famille Fang", "Famille Lü", "Famille Tao", "Famille Kang" },
                Place(NewGame(), "jingshui-lake").Factions.Select(f => f.Name));
        }

        [Test]
        public void Places_ShowEachPowersKindRelationAndStrongestRealm()
        {
            var s = NewGame();
            s.Factions.ChangeRelation(s.Factions.GetFactionByName("Famille Ruan").ID, 7);
            var ruan = Place(s, "heshan").Factions.Single();
            Assert.AreEqual(new MapFaction("Famille Ruan", "Famille", -3, "Manoir Pourpre"), ruan); // Ruan Chuhe, their patriarch (wiki)
        }

        [Test]
        public void Borders_JoinNeighbours_Once()
        {
            var borders = WorldMapView.Borders(NewGame());
            Assert.AreEqual(borders.Count, borders.Distinct().Count());
            Assert.IsTrue(borders.Contains(new MapBorder("heshan", "jingshui-lake")));
            Assert.IsFalse(borders.Any(b => b.A == b.B));
            int ends = Fixtures.Content.Regions.Sum(r => r.Neighbours.Count);
            Assert.AreEqual(ends, borders.Count * 2);
        }

        [Test]
        public void Unplaced_HoldsThePowersOfAnOlderSave_WhoseRegionIsUnknown()
        {
            // saves made before L5 keep their invented factions, which have no place on the map
            var s = NewGame();
            s.Factions.AddFaction(new FactionData { Name = "Famille Wang" });
            CollectionAssert.AreEqual(new[] { "Famille Wang" }, WorldMapView.Unplaced(s).Select(f => f.Name));
        }

        [Test]
        public void KindLabel_NamesEveryKindDifferently()
        {
            var labels = Enum.GetValues(typeof(FactionKind)).Cast<FactionKind>().Select(WorldMapView.KindLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }
    }
}
