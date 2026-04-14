using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Represents a single cell on the tactical grid.
    /// </summary>
    public class GridCell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public TerrainType Terrain { get; private set; }
        public CombatUnit Occupant { get; set; }

        public bool IsOccupied => Occupant != null;

        public GridCell(int x, int y, TerrainType terrain = TerrainType.Plain)
        {
            X = x;
            Y = y;
            Terrain = terrain;
            Occupant = null;
        }

        /// <summary>
        /// Calculates the movement cost to enter this cell based on terrain.
        /// </summary>
        public int GetMovementCost()
        {
            switch (Terrain)
            {
                case TerrainType.Mountain: return 2;
                case TerrainType.Water: return 2; // Represents the -30% speed conceptually in grid terms
                default: return 1;
            }
        }
    }
}
