using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A battle's shared state: the grid, the units on it, the random source, the log and the
    /// techniques the fighters may know. Actions and AI strategies read and change it.
    /// </summary>
    public sealed class BattleField
    {
        private readonly List<CombatUnit> units = new List<CombatUnit>();
        private readonly Func<string, TechniqueData> findTechnique;

        public CombatGrid Grid { get; }
        public Random Rng { get; }
        public IGameLog Log { get; }
        public IReadOnlyList<CombatUnit> Units => units;

        /// <param name="findTechnique">Looks up a known technique by ID (e.g. the clan's deduced techniques).</param>
        public BattleField(CombatGrid grid, Random rng, IGameLog log, Func<string, TechniqueData> findTechnique = null)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            this.findTechnique = findTechnique ?? (id => null);
        }

        /// <exception cref="ArgumentException">The cell is outside the grid or taken.</exception>
        public void Place(CombatUnit unit, int x, int y)
        {
            var cell = Grid.GetCellAt(x, y);
            if (cell == null || cell.IsOccupied)
                throw new ArgumentException($"Cell ({x}, {y}) is outside the grid or taken.");
            if (!units.Contains(unit)) units.Add(unit);
            unit.SetCell(cell);
        }

        public TechniqueData FindTechnique(string id) => findTechnique(id);

        public IEnumerable<CombatUnit> ActiveOpponentsOf(CombatUnit unit) => units.Where(u => u.IsActive && u.IsAlly != unit.IsAlly);

        public IEnumerable<CombatUnit> ActiveAlliesOf(CombatUnit unit) => units.Where(u => u.IsActive && u.IsAlly == unit.IsAlly);

        /// <summary>True while at least one fighter of that side still stands on the field.</summary>
        public bool SideStands(bool allies) => units.Any(u => u.IsAlly == allies && u.IsActive);

        public CombatUnit NearestOpponent(CombatUnit unit) =>
            ActiveOpponentsOf(unit).OrderBy(o => CombatGrid.Distance(unit.CurrentCell, o.CurrentCell)).FirstOrDefault();
    }
}
