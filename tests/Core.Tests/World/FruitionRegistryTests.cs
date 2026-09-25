using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The state of the Dao lineages in one game (LORE.md §6.8): the statuses the lore fixes, and those it
    /// leaves open, drawn from the world's seed (§11.7).
    /// </summary>
    [TestFixture]
    public class FruitionRegistryTests
    {
        private static string World(GameSession s) =>
            string.Join(",", s.Fruitions.States.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value.Status}/{kv.Value.Holder}"));

        [Test]
        public void NewGame_KeepsTheStatusesTheLoreGives()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            Assert.AreEqual(new FruitionState(FruitionStatus.Occupied, "Tan Qing"), s.Fruitions.State("mutable-water"));
            Assert.AreEqual(new FruitionState(FruitionStatus.Free, null), s.Fruitions.State("orthodox-water"));
            Assert.AreEqual(FruitionStatus.Broken, s.Fruitions.State("nourishing-fire").Status);
        }

        [Test]
        public void NewGame_LeavesNoFruitionUnspecified()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            Assert.IsTrue(s.Fruitions.States.Values.All(state => state.Status != FruitionStatus.Unspecified));
            Assert.AreEqual(Fixtures.Content.Fruitions.Count, s.Fruitions.States.Count);
        }

        [Test]
        public void NewGame_DrawsTheSameWorld_FromTheSameSeed_AndAnotherFromAnother()
        {
            string first = World(GameSession.NewGame(Fixtures.Setup(7)));
            Assert.AreEqual(first, World(GameSession.NewGame(Fixtures.Setup(7))));
            Assert.IsTrue(Enumerable.Range(1, 5).Any(seed => World(GameSession.NewGame(Fixtures.Setup(seed))) != first));
        }

        [TestCase(0.1, FruitionStatus.Free)]
        [TestCase(0.7, FruitionStatus.Occupied)]
        [TestCase(0.95, FruitionStatus.Broken)]
        public void DrawStatus_FollowsTheOddsOfTheBalance(double sample, FruitionStatus status)
        {
            // Decision of 2026-09-25: 60% free, 30% occupied, 10% broken
            Assert.AreEqual(status, FruitionRegistry.DrawStatus(new FixedRandom(sample), Fixtures.Content.Balance.UnspecifiedFruitionOdds));
        }

        [Test]
        public void DrawnOccupiedFruitions_AreHeldByAnAnonymousTrueMonarch()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var drawn = Fixtures.Content.Fruitions.Where(f => f.Status == FruitionStatus.Unspecified)
                .Select(f => s.Fruitions.State(f.Id)).Where(state => state.Status == FruitionStatus.Occupied).ToList();
            Assert.IsTrue(drawn.Count > 0 && drawn.All(state => state.Holder == Fixtures.Content.AnonymousHolder));
        }

        [Test]
        public void NewGame_NeverChangesTheSharedContent()
        {
            GameSession.NewGame(Fixtures.Setup(1));
            Assert.AreEqual(FruitionStatus.Unspecified, Fixtures.Content.Fruitions.Single(f => f.Id == "orthodox-fire").Status);
        }

        [Test]
        public void RoundTrip_KeepsTheWorld()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(World(s), World(reloaded));
        }

        [Test]
        public void OlderSave_DrawsItsWorldFromItsSeed()
        {
            var data = GameSession.NewGame(Fixtures.Setup(3)).ToSaveData();
            data.FruitionStates = null; // saved before phase L4

            var first = GameSession.FromSaveData(data, Fixtures.Setup());
            var second = GameSession.FromSaveData(data, Fixtures.Setup());

            Assert.IsTrue(first.Fruitions.States.Values.All(state => state.Status != FruitionStatus.Unspecified));
            Assert.AreEqual(World(first), World(second));
        }
    }
}
