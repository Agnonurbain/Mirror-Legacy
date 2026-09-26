using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
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

        /// <summary>The technique with this ID, or null (also for no ID: a fighter without a method).</summary>
        public TechniqueData FindTechnique(string id) => id == null ? null : findTechnique(id);

        /// <summary>A fighter's reach: their agility, lengthened by the best movement art they know.</summary>
        public int MovementRangeOf(CombatUnit unit)
        {
            var best = unit.BaseData.KnownTechniqueIDs
                .Select(FindTechnique)
                .Where(t => t != null && t.Kind == TechniqueKind.Movement)
                .OrderByDescending(t => t.MovementSteps)
                .FirstOrDefault();
            return unit.MovementRange + (best?.MovementSteps ?? 0);
        }

        /// <summary>
        /// True when the attacker's method leaves them powerless against the defender's (LORE.md §2.4: the
        /// Veilleur du Sentier against the original sutra, whatever the realms).
        /// </summary>
        public bool IsPowerless(CombatUnit attacker, CombatUnit defender) =>
            TechniqueRules.IsPowerlessAgainst(FindTechnique(attacker.BaseData.CultivationMethodId), defender.BaseData.CultivationMethodId);

        public IEnumerable<CombatUnit> ActiveOpponentsOf(CombatUnit unit) => units.Where(u => u.IsActive && u.IsAlly != unit.IsAlly);

        public IEnumerable<CombatUnit> ActiveAlliesOf(CombatUnit unit) => units.Where(u => u.IsActive && u.IsAlly == unit.IsAlly);

        /// <summary>True while at least one fighter of that side still stands on the field.</summary>
        public bool SideStands(bool allies) => units.Any(u => u.IsAlly == allies && u.IsActive);

        public CombatUnit NearestOpponent(CombatUnit unit) =>
            ActiveOpponentsOf(unit).OrderBy(o => CombatGrid.Distance(unit.CurrentCell, o.CurrentCell)).FirstOrDefault();
    }
}
