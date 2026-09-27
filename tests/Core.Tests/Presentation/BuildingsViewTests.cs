using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The clan's buildings (G6): each of the eight with its level, what it gives now and at the next level, the cost of
    /// raising it, and why not — the same refusal the upgrade itself would give.
    /// </summary>
    [TestFixture]
    public class BuildingsViewTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void EveryBuilding_IsShown_WithItsLevelAndCost()
        {
            var s = Session();
            var lines = BuildingsView.Buildings(s);
            Assert.AreEqual(System.Enum.GetValues(typeof(BuildingType)).Length, lines.Count);
            var mine = lines.Single(b => b.Type == BuildingType.Mine);
            Assert.AreEqual("Mine de pierres spirituelles", mine.Name);
            Assert.AreEqual(0, mine.Level);
            Assert.AreEqual(BuildingData.GetUpgradeCost(1), mine.Cost);
            Assert.IsNull(mine.Effect, "nothing yet at level 0");
            StringAssert.Contains($"{BuildingSystem.MineStonesPerLevel} pierres", mine.NextEffect);
        }

        [Test]
        public void TheRefusal_IsTheUpgradesOwn()
        {
            var s = Session();
            foreach (var line in BuildingsView.Buildings(s))
                Assert.AreEqual(s.Buildings.UpgradeRefusal(line.Type), line.Refusal, line.Name);
        }

        [Test]
        public void ABuildingAtItsHeight_HasNoNextLevel()
        {
            var s = Session();
            s.Buildings.Restore(new[] { new BuildingData(BuildingType.Forge) { Level = BuildingSystem.MaxLevel } });
            var forge = BuildingsView.Buildings(s).Single(b => b.Type == BuildingType.Forge);
            Assert.IsNull(forge.Cost);
            Assert.IsNull(forge.NextEffect);
            StringAssert.Contains("25 %", forge.Effect);
        }
    }
}
