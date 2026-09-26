using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>The world's factions and their yearly behaviour toward the clan.</summary>
    [TestFixture]
    public class FactionManagerTests
    {
        private static (TestWorld world, FactionData faction) WithFaction(FactionPersonality personality, int relation, int power = 500)
        {
            var w = new TestWorld();
            var f = new FactionData { Name = "Test Faction", Personality = personality, RelationWithPlayer = relation, PowerLevel = power };
            w.Factions.AddFaction(f);
            return (w, f);
        }

        [Test]
        public void InitializeFactions_CopiesEveryFactionOfTheContent()
        {
            var w = new TestWorld();
            w.Factions.InitializeFactions();
            CollectionAssert.AreEqual(Fixtures.Content.Factions.Select(f => f.Name), w.Factions.Factions.Select(f => f.Name));
        }

        [Test]
        public void InitializeFactions_GivesEachFactionItsOwnIdentity()
        {
            var w = new TestWorld();
            w.Factions.InitializeFactions();
            var templates = Fixtures.Content.Factions.ToList();
            Assert.IsTrue(w.Factions.Factions.Select(f => f.ID).Distinct().Count() == templates.Count
                && w.Factions.Factions.All(f => templates.All(t => !ReferenceEquals(t, f))));
        }

        [Test]
        public void GetFactionByID_ReturnsNull_WhenUnknown()
        {
            Assert.IsNull(new TestWorld().Factions.GetFactionByID("nobody"));
        }

        [TestCase(90, 50, 100)]
        [TestCase(-90, -50, -100)]
        public void ChangeRelation_StaysWithinPlusMinusHundred(int start, int change, int expected)
        {
            var (w, f) = WithFaction(FactionPersonality.Merchant, start);
            w.Factions.ChangeRelation(f.ID, change);
            Assert.AreEqual(expected, f.RelationWithPlayer);
        }

        [TestCase(FactionPersonality.Merchant, 10, 12)]       // trades its way closer
        [TestCase(FactionPersonality.Isolationist, 10, 9)]    // drifts back to neutral
        [TestCase(FactionPersonality.Isolationist, -10, -9)]
        [TestCase(FactionPersonality.Aggressive, -40, -45)]   // hostility feeds itself
        [TestCase(FactionPersonality.Manipulative, -1, -4)]   // spreads rumours
        public void ProcessYearlyFactionAI_MovesRelationsByPersonality(FactionPersonality personality, int start, int expected)
        {
            var (w, f) = WithFaction(personality, start);
            w.Factions.ProcessYearlyFactionAI();
            Assert.AreEqual(expected, f.RelationWithPlayer);
        }

        [Test]
        public void ProcessYearlyFactionAI_GrowsExpansionistPower()
        {
            var (w, f) = WithFaction(FactionPersonality.Expansionist, 0, power: 1000);
            w.Factions.ProcessYearlyFactionAI();
            Assert.AreEqual(1100, f.PowerLevel);
        }

        // ---- Neighbours weigh more (L5b: the powers live on the map) ----

        [TestCase(FactionPersonality.Aggressive, -40, "heshan")]        // the Ruan's prefecture borders the lake
        [TestCase(FactionPersonality.Merchant, 10, "jingshui-lake")]   // on the lake itself
        public void ProcessYearlyFactionAI_ANeighbourActsMoreStrongly(FactionPersonality personality, int start, string region)
        {
            var (far, distant) = WithFaction(personality, start);
            var (near, neighbour) = WithFaction(personality, start);
            neighbour.RegionId = region;

            far.Factions.ProcessYearlyFactionAI();
            near.Factions.ProcessYearlyFactionAI();

            int factor = Fixtures.Content.Balance.Diplomacy.NeighbourIntensity;
            Assert.AreEqual(start + factor * (distant.RelationWithPlayer - start), neighbour.RelationWithPlayer);
        }

        [Test]
        public void IsNeighbour_OnlyForTheHomeAndThePlacesBorderingIt()
        {
            var w = new TestWorld();
            Assert.IsTrue(w.Factions.IsNeighbour(new FactionData { RegionId = "jingshui-lake" }));
            Assert.IsTrue(w.Factions.IsNeighbour(new FactionData { RegionId = "heshan" }));
            Assert.IsFalse(w.Factions.IsNeighbour(new FactionData { RegionId = "mount-yunfeng" }));
            Assert.IsFalse(w.Factions.IsNeighbour(new FactionData()));
        }
    }
}
