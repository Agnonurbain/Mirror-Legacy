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
        /// Initializes the grid with a specific size and optional terrain generation.
        /// </summary>
        public void InitializeGrid(int width = 10, int height = 10)
        {
            Width = width;
            Height = height;
            _grid = new GridCell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // For Phase 2 prototype, we just make everything Plain.
                    // Later, we can add procedural terrain generation here.
                    _grid[x, y] = new GridCell(x, y, TerrainType.Plain);
                }
            }
            
            Debug.Log($"[GridSystem] Initialized {Width}x{Height} grid.");
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
    }
}
