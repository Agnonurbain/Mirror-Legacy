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
            CollectionAssert.AreEquivalent(new[] { "Famille Lou", "Famille Fang", "Famille Lü", "Famille Tao", "Famille Kang", "Famille Xun" }, // 📚 the Xun of Mount Tiaoyun (audit §4.8)
                Place(NewGame(), "jingshui-lake").Factions.Select(f => f.Name));
        }

        [Test]
        public void Places_ShowEachPowersKindRelationAndStrongestRealm()
        {
            var s = NewGame();
            s.Factions.ChangeRelation(s.Factions.GetFactionByName("Famille Ruan").ID, 7);
            var ruan = Place(s, "heshan").Factions.Single();
            Assert.AreEqual(new MapFaction("Famille Ruan", "Famille", 27, "Manoir Pourpre"), ruan); // Ruan Chuhe, their patriarch (wiki); the clan's protector (audit §4.4)
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

        [Test]
        public void KnownBeasts_ShowOnlyWhatTheClanScouted()
        {
            var s = NewGame();
            var beast = s.Bestiary.In("heshan").First();
            Assert.AreEqual(0, WorldMapView.KnownBeastsOf(s, "heshan").Count);

            s.Knowledge.Reveal(MirrorChronicles.World.FactKind.Beast, beast.Id, MirrorChronicles.World.KnowledgeSource.Studied);

            var shown = WorldMapView.KnownBeastsOf(s, "heshan").Single();
            string species = Fixtures.Content.BeastSpecies.Single(b => b.Id == beast.SpeciesId).Name;
            Assert.AreEqual(species, shown.Species);
            Assert.AreEqual(beast.OwnerFaction ?? "solitaire", shown.Owner);
        }

        // ---- The Qi of a place (L5b) ----

        [Test]
        public void QiOf_TellsTheDensity_TheQi_AndTheirAbundance()
        {
            var qi = WorldMapView.QiOf(NewGame(), "jingshui-lake");

            Assert.AreEqual("Qi ordinaire", qi.Density);
            Assert.IsTrue(qi.Qi.Any(q => q.Name == "Qi de la Source Claire" && !string.IsNullOrEmpty(q.Abundance)));
            Assert.IsNull(qi.Atmosphere);
        }

        [Test]
        public void QiOf_NamesTheAtmosphere_AndWhatItFavours()
        {
            var qi = WorldMapView.QiOf(NewGame(), "baishi");

            Assert.AreEqual("Grand Entrepôt des Esprits Funestes", qi.Atmosphere);
            StringAssert.Contains("Eau Nourricière", qi.AtmosphereEffect);
            StringAssert.Contains("Dao du Diable", qi.AtmosphereEffect);
        }

        [Test]
        public void QiOf_TellsTheStormsWeight()
        {
            StringAssert.Contains("−2,5 %", WorldMapView.QiOf(NewGame(), "zhanghe").AtmosphereEffect);
        }

        [Test]
        public void QiOf_AnUnknownPlace_IsNull()
        {
            Assert.IsNull(WorldMapView.QiOf(NewGame(), "nowhere"));
        }

        [Test]
        public void Places_CarryTheirQiDensity_AndTheAtmosphereOverThem()
        {
            var s = NewGame();
            Assert.AreEqual(1.0, Place(s, "jingshui-lake").QiDensity, 1e-9);
            Assert.IsNull(Place(s, "jingshui-lake").Atmosphere);
            Assert.AreEqual(("Grand Entrepôt des Esprits Funestes", false), (Place(s, "baishi").Atmosphere, Place(s, "baishi").AtmosphereHarsh));
            Assert.IsTrue(Place(s, "zhanghe").AtmosphereHarsh, "a storm weighs on all");
        }
    }
}
