namespace MirrorChronicles.Combat
{
    /// <summary>One cell of the tactical grid: its terrain and who stands on it.</summary>
    public sealed class GridCell
    {
        public int X { get; }
        public int Y { get; }
        public TerrainType Terrain { get; internal set; }
        public CombatUnit Occupant { get; internal set; }
        public bool IsOccupied => Occupant != null;

        public GridCell(int x, int y, TerrainType terrain = TerrainType.Plain)
        {
            X = x;
            Y = y;
            Terrain = terrain;
        }

        /// <summary>Mountains and water cost two movement points, everything else one.</summary>
        public int GetMovementCost() => Terrain == TerrainType.Mountain || Terrain == TerrainType.Water ? 2 : 1;
    }
}
