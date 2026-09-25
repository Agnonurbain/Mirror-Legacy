using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>The tactical grid: terrain, distances and paths.</summary>
    [TestFixture]
    public class CombatGridTests
    {
        [Test]
        public void NewGrid_IsAllPlain()
        {
            var grid = new CombatGrid(6, 4);
            Assert.IsTrue(grid.Cells.Count() == 24 && grid.Cells.All(c => c.Terrain == TerrainType.Plain));
        }

        [TestCase(-1, 0)]
        [TestCase(6, 0)]
        [TestCase(0, 4)]
        public void GetCellAt_ReturnsNull_OutsideTheGrid(int x, int y)
        {
            Assert.IsNull(new CombatGrid(6, 4).GetCellAt(x, y));
        }

        [Test]
        public void Distance_IsManhattan()
        {
            var grid = new CombatGrid(10, 10);
            Assert.AreEqual(7, CombatGrid.Distance(grid.GetCellAt(1, 2), grid.GetCellAt(4, 6)));
        }

        [Test]
        public void GetAdjacentCells_StaysInsideTheGrid()
        {
            var grid = new CombatGrid(10, 10);
            Assert.AreEqual(2, grid.GetAdjacentCells(grid.GetCellAt(0, 0)).Count);
        }

        [TestCase(TerrainType.Plain, 1)]
        [TestCase(TerrainType.Forest, 1)]
        [TestCase(TerrainType.Mountain, 2)]
        [TestCase(TerrainType.Water, 2)]
        public void MovementCost_DependsOnTerrain(TerrainType terrain, int expected)
        {
            var grid = new CombatGrid(3, 3);
            grid.SetTerrain(1, 1, terrain);
            Assert.AreEqual(expected, grid.GetCellAt(1, 1).GetMovementCost());
        }

        [Test]
        public void FindPath_WalksStraight_OnOpenGround()
        {
            var grid = new CombatGrid(10, 10);
            var path = grid.FindPath(grid.GetCellAt(0, 0), grid.GetCellAt(3, 0));
            Assert.IsTrue(path.Count == 3 && path.Last() == grid.GetCellAt(3, 0));
        }

        [Test]
        public void FindPath_WalksAroundAnOccupiedCell()
        {
            var field = CombatFixtures.Field();
            CombatFixtures.Place(field, 1, 0, isAlly: false);
            var path = field.Grid.FindPath(field.Grid.GetCellAt(0, 0), field.Grid.GetCellAt(2, 0));
            Assert.IsTrue(path.Count == 4 && !path.Contains(field.Grid.GetCellAt(1, 0)));
        }

        [Test]
        public void FindPath_IsEmpty_WhenTheGoalIsWalledOff()
        {
            var field = CombatFixtures.Field(width: 3, height: 3);
            CombatFixtures.Place(field, 1, 0);
            CombatFixtures.Place(field, 0, 1);
            Assert.IsEmpty(field.Grid.FindPath(field.Grid.GetCellAt(2, 2), field.Grid.GetCellAt(0, 0)));
        }

        [Test]
        public void Generate_PlacesQiSpotsAwayFromTheEdges()
        {
            var grid = CombatGrid.Generate(10, 10, new Random(3));
            var spots = grid.Cells.Where(c => c.Terrain == TerrainType.ConcentratedQi).ToList();
            Assert.IsTrue(spots.Count == 2 && spots.All(c => c.X >= 2 && c.X <= 7 && c.Y >= 2 && c.Y <= 7));
        }

        [Test]
        public void Generate_GrowsForestsMountainsAndWater()
        {
            var grid = CombatGrid.Generate(10, 10, new Random(3));
            var kinds = grid.Cells.Select(c => c.Terrain).Distinct().ToList();
            CollectionAssert.IsSubsetOf(new[] { TerrainType.Plain, TerrainType.Forest, TerrainType.Mountain, TerrainType.Water }, kinds);
        }

        [Test]
        public void Generate_IsReproducibleFromTheSeed()
        {
            string Terrain(int seed) => string.Concat(CombatGrid.Generate(10, 10, new Random(seed)).Cells.Select(c => (int)c.Terrain));
            Assert.AreEqual(Terrain(9), Terrain(9));
        }
    }
}
