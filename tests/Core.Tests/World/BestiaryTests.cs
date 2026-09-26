using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The spirit beasts of the world (L2c.2; user decision, 2026-09-26): each place has its beasts, drawn from the
    /// world's seed; most belong to the powers living there, solitary ones are rare and roam the wild places.
    /// </summary>
    [TestFixture]
    public class BestiaryTests
    {
        private static GameSession NewGame(int seed = 1) => GameSession.NewGame(Fixtures.Setup(seed));

        private static string World(GameSession s) =>
            string.Join(",", s.Bestiary.Beasts.Select(b => $"{b.Id}:{b.SpeciesId}@{b.RegionId}/{b.Realm}{b.Stage}/{b.OwnerFaction}"));

        [Test]
        public void ANewGame_PlacesBeastsOnThePlacesOfTheMap()
        {
            var s = NewGame();
            var places = Fixtures.Content.Regions.Where(r => r.ParentId != null).Select(r => r.Id).ToHashSet();
            Assert.IsTrue(s.Bestiary.Beasts.Count > 0);
            Assert.IsTrue(s.Bestiary.Beasts.All(b => places.Contains(b.RegionId)));
            Assert.IsTrue(s.Bestiary.Beasts.All(b => Fixtures.Content.BeastSpecies.Any(sp => sp.Id == b.SpeciesId)));
        }

        [Test]
        public void MostBeasts_WherePowersLive_BelongToThem()
        {
            var s = NewGame();
            var powers = s.Factions.Factions.Select(f => f.RegionId).ToHashSet();
            var there = s.Bestiary.Beasts.Where(b => powers.Contains(b.RegionId)).ToList();
            Assert.Greater(there.Count(b => b.OwnerFaction != null), there.Count / 2);
            Assert.IsTrue(there.Where(b => b.OwnerFaction != null)
                .All(b => s.Factions.GetFactionByName(b.OwnerFaction).RegionId == b.RegionId), "a beast belongs to a power of its place");
        }

        [Test]
        public void WherePowersDoNotLive_BeastsAreSolitary()
        {
            var s = NewGame();
            var powers = s.Factions.Factions.Select(f => f.RegionId).ToHashSet();
            Assert.IsTrue(s.Bestiary.Beasts.Where(b => !powers.Contains(b.RegionId)).All(b => b.OwnerFaction == null));
        }

        [Test]
        public void TheSameSeed_PlacesTheSameBeasts()
        {
            Assert.AreEqual(World(NewGame(7)), World(NewGame(7)));
            Assert.AreNotEqual(World(NewGame(7)), World(NewGame(8)));
        }

        [Test]
        public void RoundTrip_KeepsTheWorldsBeasts()
        {
            var s = NewGame(3);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(World(s), World(reloaded));
        }
    }
}
