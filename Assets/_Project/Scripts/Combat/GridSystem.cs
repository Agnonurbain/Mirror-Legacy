using System.Collections.Generic;
using UnityEngine;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Manages the 2D tactical grid (usually 10x10 or 12x12).
    /// Handles cell retrieval and basic distance calculations.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        public int Width { get; private set; } = 10;
        public int Height { get; private set; } = 10;
        
        private GridCell[,] _grid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Initializes the grid with procedural terrain generation.
        /// </summary>
        public void InitializeGrid(int width = 10, int height = 10,
            float forestPercent = 0.15f, float mountainPercent = 0.10f,
            float waterPercent = 0.08f, int qiSpots = 2)
        {
            Width = width;
            Height = height;
            _grid = new GridCell[Width, Height];

            int totalCells = Width * Height;
            int forestCount = Mathf.RoundToInt(totalCells * forestPercent);
            int mountainCount = Mathf.RoundToInt(totalCells * mountainPercent);
            int waterCount = Mathf.RoundToInt(totalCells * waterPercent);
            int qiCount = Mathf.Clamp(qiSpots, 1, 3);

            // Start with all plain
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _grid[x, y] = new GridCell(x, y, TerrainType.Plain);

            // Place terrain clusters using random seeds with neighbor spread
            PlaceTerrainCluster(TerrainType.Forest, forestCount);
            PlaceTerrainCluster(TerrainType.Mountain, mountainCount);
            PlaceTerrainCluster(TerrainType.Water, waterCount);

            // Place ConcentratedQi spots away from edges
            int placed = 0;
            int attempts = 0;
            while (placed < qiCount && attempts < 100)
            {
                int x = Random.Range(2, Width - 2);
                int y = Random.Range(2, Height - 2);
                if (_grid[x, y].Terrain == TerrainType.Plain)
                {
                    _grid[x, y] = new GridCell(x, y, TerrainType.ConcentratedQi);
                    placed++;
                }
                attempts++;
            }

            Debug.Log($"[GridSystem] Initialized {Width}x{Height} grid with procedural terrain.");
        }

        private void PlaceTerrainCluster(TerrainType terrain, int count)
        {
            if (count <= 0) return;

            // Pick a random seed point
            int seedX = Random.Range(1, Width - 1);
            int seedY = Random.Range(1, Height - 1);
            _grid[seedX, seedY] = new GridCell(seedX, seedY, terrain);
            int placed = 1;

            // Grow outward from seed
            var frontier = new List<Vector2Int> { new Vector2Int(seedX, seedY) };

            while (placed < count && frontier.Count > 0)
            {
                int idx = Random.Range(0, frontier.Count);
                var current = frontier[idx];
                frontier.RemoveAt(idx);

                int[][] dirs = { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };
                foreach (var d in dirs)
                {
                    if (placed >= count) break;
                    int nx = current.x + d[0];
                    int ny = current.y + d[1];
                    if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;
                    if (_grid[nx, ny].Terrain != TerrainType.Plain) continue;

                    if (Random.value < 0.6f)
                    {
                        _grid[nx, ny] = new GridCell(nx, ny, terrain);
                        placed++;
                        frontier.Add(new Vector2Int(nx, ny));
                    }
                }

                if (placed < count && frontier.Count == 0)
                {
                    // Fallback: pick a new random seed
                    int fx = Random.Range(0, Width);
                    int fy = Random.Range(0, Height);
                    if (_grid[fx, fy].Terrain == TerrainType.Plain)
                    {
                        _grid[fx, fy] = new GridCell(fx, fy, terrain);
                        placed++;
                        frontier.Add(new Vector2Int(fx, fy));
                    }
                }
            }
        }

        public GridCell GetCellAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            
            return _grid[x, y];
        }

        /// <summary>
        /// Calculates Manhattan distance between two cells (standard for grid movement without diagonals).
        /// </summary>
        public int GetDistance(GridCell a, GridCell b)
        {
            if (a == null || b == null) return 999;
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// Retrieves all adjacent cells (Up, Down, Left, Right).
        /// </summary>
        public List<GridCell> GetAdjacentCells(GridCell center)
        {
            List<GridCell> neighbors = new List<GridCell>();
            if (center == null) return neighbors;

            GridCell up = GetCellAt(center.X, center.Y + 1);
            GridCell down = GetCellAt(center.X, center.Y - 1);
            GridCell left = GetCellAt(center.X - 1, center.Y);
            GridCell right = GetCellAt(center.X + 1, center.Y);

            if (up != null) neighbors.Add(up);
            if (down != null) neighbors.Add(down);
            if (left != null) neighbors.Add(left);
            if (right != null) neighbors.Add(right);

            return neighbors;
        }

        /// <summary>
        /// A* pathfinding from start to goal, respecting terrain movement costs and occupied cells.
        /// Returns the path as a list of cells (excluding start, including goal), or empty if no path.
        /// </summary>
        public List<GridCell> FindPath(GridCell start, GridCell goal)
        {
            if (start == null || goal == null) return new List<GridCell>();

            var openSet = new List<GridCell> { start };
            var cameFrom = new Dictionary<GridCell, GridCell>();
            var gScore = new Dictionary<GridCell, int> { [start] = 0 };
            var fScore = new Dictionary<GridCell, int> { [start] = GetDistance(start, goal) };

            while (openSet.Count > 0)
            {
                // Find node in openSet with lowest fScore
                GridCell current = openSet[0];
                int currentF = fScore.ContainsKey(current) ? fScore[current] : int.MaxValue;
                for (int i = 1; i < openSet.Count; i++)
                {
                    int f = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : int.MaxValue;
                    if (f < currentF)
                    {
                        current = openSet[i];
                        currentF = f;
                    }
                }

                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                foreach (var neighbor in GetAdjacentCells(current))
                {
                    // Skip occupied cells (unless it's the goal itself)
                    if (neighbor.IsOccupied && neighbor != goal) continue;

                    int tentativeG = gScore[current] + neighbor.GetMovementCost();

                    int neighborG = gScore.ContainsKey(neighbor) ? gScore[neighbor] : int.MaxValue;
                    if (tentativeG < neighborG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + GetDistance(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return new List<GridCell>();
        }

        private List<GridCell> ReconstructPath(Dictionary<GridCell, GridCell> cameFrom, GridCell current)
        {
            var path = new List<GridCell> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            // Remove start cell from path
            if (path.Count > 0) path.RemoveAt(0);
            return path;
        }
    }
}
