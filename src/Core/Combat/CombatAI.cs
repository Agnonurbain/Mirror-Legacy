using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>An AI behaviour for one unit's turn (strategy pattern).</summary>
    public interface IAIStrategy
    {
        void PlayTurn(CombatUnit unit, BattleField field);
    }

    /// <summary>
    /// Plays a unit's turn with its <see cref="CombatUnit.Strategy"/>. Moves use the unit's whole
    /// movement range; ties between cells break by grid order, so turns are reproducible.
    /// </summary>
    public static class CombatAI
    {
        private static readonly Dictionary<AIStrategyType, IAIStrategy> Strategies = new Dictionary<AIStrategyType, IAIStrategy>
        {
            [AIStrategyType.Aggressive] = new AggressiveStrategy(),
            [AIStrategyType.Defensive] = new DefensiveStrategy(),
            [AIStrategyType.Strategic] = new StrategicStrategy(),
            [AIStrategyType.Berserker] = new BerserkerStrategy(),
            [AIStrategyType.Cautious] = new CautiousStrategy()
        };

        public static void PlayTurn(CombatUnit unit, BattleField field)
        {
            if (unit.IsActive) Strategies[unit.Strategy].PlayTurn(unit, field);
        }

        internal static int DistanceTo(CombatUnit unit, CombatUnit other) => CombatGrid.Distance(unit.CurrentCell, other.CurrentCell);

        /// <summary>Moves to the reachable cell closest to the goal, if it brings the unit closer.</summary>
        internal static void MoveTowards(CombatUnit unit, GridCell goal, BattleField field)
        {
            var move = new MoveAction();
            var best = field.Grid.Cells.Where(c => move.IsValid(unit, c, field))
                .OrderBy(c => CombatGrid.Distance(c, goal)).FirstOrDefault();
            if (best != null && CombatGrid.Distance(best, goal) < CombatGrid.Distance(unit.CurrentCell, goal))
                move.Execute(unit, best, field);
        }

        /// <summary>Moves to the reachable cell farthest from the threat, if it takes the unit farther.</summary>
        internal static void MoveAwayFrom(CombatUnit unit, GridCell threat, BattleField field)
        {
            var move = new MoveAction();
            var best = field.Grid.Cells.Where(c => move.IsValid(unit, c, field))
                .OrderByDescending(c => CombatGrid.Distance(c, threat)).FirstOrDefault();
            if (best != null && CombatGrid.Distance(best, threat) > CombatGrid.Distance(unit.CurrentCell, threat))
                move.Execute(unit, best, field);
        }

        internal static void StrikeIfAdjacent(CombatUnit unit, CombatUnit target, BattleField field)
        {
            var attack = new AttackAction();
            if (target != null && attack.IsValid(unit, target.CurrentCell, field))
                attack.Execute(unit, target.CurrentCell, field);
        }
    }

    /// <summary>Closes in on the nearest foe and strikes.</summary>
    internal sealed class AggressiveStrategy : IAIStrategy
    {
        public void PlayTurn(CombatUnit unit, BattleField field)
        {
            var target = field.NearestOpponent(unit);
            if (target == null) return;
            if (CombatAI.DistanceTo(unit, target) > 1) CombatAI.MoveTowards(unit, target.CurrentCell, field);
            CombatAI.StrikeIfAdjacent(unit, target, field);
        }
    }

    /// <summary>Holds its ground behind a guard.</summary>
    internal sealed class DefensiveStrategy : IAIStrategy
    {
        public void PlayTurn(CombatUnit unit, BattleField field) => new DefendAction().Execute(unit, unit.CurrentCell, field);
    }

    /// <summary>
    /// Hunts the foe with the least strength (a likely healer or support), approaching through cover,
    /// and never steps away from a target it already stands next to.
    /// </summary>
    internal sealed class StrategicStrategy : IAIStrategy
    {
        public void PlayTurn(CombatUnit unit, BattleField field)
        {
            var target = field.ActiveOpponentsOf(unit).OrderBy(o => o.Strength).FirstOrDefault();
            if (target == null) return;

            if (CombatAI.DistanceTo(unit, target) > 1)
            {
                var move = new MoveAction();
                var best = field.Grid.Cells.Where(c => move.IsValid(unit, c, field))
                    .OrderByDescending(c => CoverScore(c) * 10 + (10 - CombatGrid.Distance(c, target.CurrentCell)))
                    .FirstOrDefault();
                if (best != null) move.Execute(unit, best, field);
            }
            CombatAI.StrikeIfAdjacent(unit, target, field);
        }

        private static int CoverScore(GridCell cell) => cell.Terrain switch
        {
            TerrainType.Forest => 3,
            TerrainType.Mountain => 2,
            TerrainType.ConcentratedQi => 1,
            _ => 0
        };
    }

    /// <summary>Charges the nearest foe and spends its Qi on its strongest striking art in range.</summary>
    internal sealed class BerserkerStrategy : IAIStrategy
    {
        public void PlayTurn(CombatUnit unit, BattleField field)
        {
            var target = field.NearestOpponent(unit);
            if (target == null) return;
            if (CombatAI.DistanceTo(unit, target) > 1) CombatAI.MoveTowards(unit, target.CurrentCell, field);

            var strongest = unit.BaseData.KnownTechniqueIDs
                .Select(field.FindTechnique)
                .Where(t => t != null && t.Effect == TechniqueEffect.Strike)
                .OrderByDescending(t => t.PowerModifier)
                .Select(t => new TechniqueAction(t))
                .FirstOrDefault(a => a.IsValid(unit, target.CurrentCell, field));

            if (strongest != null) strongest.Execute(unit, target.CurrentCell, field);
            else CombatAI.StrikeIfAdjacent(unit, target, field);
        }
    }

    /// <summary>Flees below 40% vitality, guards when a foe is next to it, otherwise keeps its distance.</summary>
    internal sealed class CautiousStrategy : IAIStrategy
    {
        private const double FleeBelow = 0.4;

        public void PlayTurn(CombatUnit unit, BattleField field)
        {
            if (unit.CurrentVitality < unit.MaxVitality * FleeBelow)
            {
                new FleeAction().Execute(unit, null, field);
                return; // the attempt takes the whole turn
            }

            var nearest = field.NearestOpponent(unit);
            if (nearest == null) return;
            if (CombatAI.DistanceTo(unit, nearest) <= 1) new DefendAction().Execute(unit, unit.CurrentCell, field);
            else CombatAI.MoveAwayFrom(unit, nearest.CurrentCell, field);
        }
    }
}
