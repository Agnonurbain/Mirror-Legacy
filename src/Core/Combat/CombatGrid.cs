using System;
using System.Collections.Generic;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// The tactical grid: cells, Manhattan distances, neighbours and A* paths. Terrain is grown in
    /// clusters from the session's seed, so a battle can be replayed.
    /// </summary>
    public sealed class CombatGrid
    {
        public const int Unreachable = 999;
        private const double ClusterGrowthChance = 0.6;
        private const int QiSpotAttempts = 100;

        private readonly GridCell[,] cells;

        public int Width { get; }
        public int Height { get; }

        public CombatGrid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new GridCell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = new GridCell(x, y);
        }

        public IEnumerable<GridCell> Cells
        {
            get
            {
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        yield return cells[x, y];
            }
        }

        /// <summary>
        /// A battlefield: clusters of forest, mountain and water (15%, 10%, 8% of the cells), and one to
        /// three spots of concentrated Qi away from the edges.
        /// </summary>
        public static CombatGrid Generate(int width, int height, Random rng, double forest = 0.15, double mountain = 0.10,
            double water = 0.08, int qiSpots = 2)
        {
            var grid = new CombatGrid(width, height);
            int total = width * height;
            grid.GrowCluster(TerrainType.Forest, (int)Math.Round(total * forest), rng);
            grid.GrowCluster(TerrainType.Mountain, (int)Math.Round(total * mountain), rng);
            grid.GrowCluster(TerrainType.Water, (int)Math.Round(total * water), rng);

            int wanted = Math.Clamp(qiSpots, 1, 3);
            for (int placed = 0, attempts = 0; placed < wanted && attempts < QiSpotAttempts; attempts++)
            {
                var cell = grid.cells[rng.Next(2, width - 2), rng.Next(2, height - 2)];
                if (cell.Terrain != TerrainType.Plain) continue;
                cell.Terrain = TerrainType.ConcentratedQi;
                placed++;
            }
            return grid;
        }

        public GridCell GetCellAt(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? null : cells[x, y];

        public void SetTerrain(int x, int y, TerrainType terrain) => cells[x, y].Terrain = terrain;

        /// <summary>Manhattan distance (no diagonal moves); <see cref="Unreachable"/> when a cell is missing.</summary>
        public static int Distance(GridCell a, GridCell b) =>
            a == null || b == null ? Unreachable : Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        /// <summary>Up, down, left, right, within the grid.</summary>
        public List<GridCell> GetAdjacentCells(GridCell center)
        {
            var neighbours = new List<GridCell>();
            if (center == null) return neighbours;
            foreach (var cell in new[]
            {
                GetCellAt(center.X, center.Y + 1), GetCellAt(center.X, center.Y - 1),
                GetCellAt(center.X - 1, center.Y), GetCellAt(center.X + 1, center.Y)
            })
            {
                if (cell != null) neighbours.Add(cell);
            }
            return neighbours;
        }

        /// <summary>
        /// A* from start to goal, weighing terrain costs and avoiding occupied cells (except the goal).
        /// Returns the cells to walk (start excluded, goal included), or an empty list.
        /// </summary>
        public List<GridCell> FindPath(GridCell start, GridCell goal)
        {
            if (start == null || goal == null) return new List<GridCell>();

            var open = new List<GridCell> { start };
            var cameFrom = new Dictionary<GridCell, GridCell>();
            var gScore = new Dictionary<GridCell, int> { [start] = 0 };
            var fScore = new Dictionary<GridCell, int> { [start] = Distance(start, goal) };

            while (open.Count > 0)
            {
                var current = open[0];
                foreach (var candidate in open)
                    if (fScore[candidate] < fScore[current]) current = candidate;

                if (current == goal) return Reconstruct(cameFrom, current);
                open.Remove(current);

                foreach (var neighbour in GetAdjacentCells(current))
                {
                    if (neighbour.IsOccupied && neighbour != goal) continue;
                    int tentative = gScore[current] + neighbour.GetMovementCost();
                    if (gScore.TryGetValue(neighbour, out int known) && tentative >= known) continue;

                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentative;
                    fScore[neighbour] = tentative + Distance(neighbour, goal);
                    if (!open.Contains(neighbour)) open.Add(neighbour);
                }
            }
            return new List<GridCell>();
        }

        private static List<GridCell> Reconstruct(Dictionary<GridCell, GridCell> cameFrom, GridCell current)
        {
            var path = new List<GridCell> { current };
            while (cameFrom.TryGetValue(current, out var previous))
            {
                current = previous;
                path.Insert(0, current);
            }
            path.RemoveAt(0); // the start
            return path;
        }

        /// <summary>Grows a patch of terrain from a random seed, reseeding when the patch is walled in.</summary>
        private void GrowCluster(TerrainType terrain, int count, Random rng)
        {
            if (count <= 0) return;

            var seed = cells[rng.Next(1, Width - 1), rng.Next(1, Height - 1)];
            seed.Terrain = terrain;
            int placed = 1;
            var frontier = new List<GridCell> { seed };

            while (placed < count && frontier.Count > 0)
            {
                int index = rng.Next(frontier.Count);
                var current = frontier[index];
                frontier.RemoveAt(index);

                foreach (var neighbour in GetAdjacentCells(current))
                {
                    if (placed >= count) break;
                    if (neighbour.Terrain != TerrainType.Plain || rng.NextDouble() >= ClusterGrowthChance) continue;
                    neighbour.Terrain = terrain;
                    placed++;
                    frontier.Add(neighbour);
                }

                if (placed < count && frontier.Count == 0)
                {
                    var fresh = cells[rng.Next(Width), rng.Next(Height)];
                    if (fresh.Terrain != TerrainType.Plain) continue; // the loop ends: this patch stays smaller
                    fresh.Terrain = terrain;
                    placed++;
                    frontier.Add(fresh);
                }
            }
        }
    }
}
