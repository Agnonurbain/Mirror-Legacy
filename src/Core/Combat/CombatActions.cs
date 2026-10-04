using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>A tactical action (command pattern): checked with <see cref="IsValid"/>, then executed.</summary>
    public interface ICombatAction
    {
        ActionType Type { get; }
        int QiCost { get; }
        bool IsValid(CombatUnit user, GridCell target, BattleField field);

        /// <summary>Does nothing when the action is not valid.</summary>
        void Execute(CombatUnit user, GridCell target, BattleField field);
    }

    /// <summary>Damage and healing shared by attacks and techniques.</summary>
    internal static class CombatMath
    {
        public const double AffinityBonus = 1.25;
        public const double WaterTerrainBonus = 1.2;

        public static double RealmFactor(CombatUnit unit, double perRealm) => 1.0 + (int)unit.BaseData.Realm * perRealm;

        public static bool IsOpponent(CombatUnit user, GridCell cell) =>
            cell?.Occupant != null && cell.Occupant.IsActive && cell.Occupant.IsAlly != user.IsAlly;

        public static bool IsFriend(CombatUnit user, GridCell cell) =>
            cell?.Occupant != null && cell.Occupant.IsActive && cell.Occupant.IsAlly == user.IsAlly;

        /// <summary>At least one point of damage always lands.</summary>
        public static int AfterDefence(int raw, CombatUnit target) => Math.Max(1, raw - target.Defense);

        /// <summary>The damage that lands: none when the user is powerless against the target (a countered technique).</summary>
        public static int Landing(int raw, CombatUnit user, CombatUnit target, BattleField field) =>
            field.IsPowerless(user, target) ? 0 : AfterDefence(raw, target);
    }

    /// <summary>
    /// Walks to a free cell along a free path (A*) whose terrain cost fits the unit's reach (its movement
    /// range and movement arts); a single step is always allowed, even onto a mountain or into water.
    /// </summary>
    public sealed class MoveAction : ICombatAction
    {
        public ActionType Type => ActionType.Move;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field)
        {
            if (!user.IsActive || user.HasMovedThisTurn || user.CurrentCell == null || target == null || target.IsOccupied)
                return false;
            int reach = field.MovementRangeOf(user);
            if (CombatGrid.Distance(user.CurrentCell, target) > reach) return false; // every step costs at least one

            var path = field.Grid.FindPath(user.CurrentCell, target);
            return path.Count == 1 || (path.Count > 1 && path.Sum(c => c.GetMovementCost()) <= reach);
        }

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;
            user.SetCell(target);
            user.HasMovedThisTurn = true;
        }

        /// <summary>
        /// Every cell <see cref="IsValid"/> accepts, from one search bounded by the reach (not one path per cell): the free
        /// neighbours (a single step is always allowed), and the free cells whose cheapest way costs no more than the reach.
        /// </summary>
        public static IReadOnlyList<GridCell> Targets(CombatUnit user, BattleField field)
        {
            var start = user.CurrentCell;
            if (!user.IsActive || user.HasMovedThisTurn || start == null) return Array.Empty<GridCell>();
            int reach = field.MovementRangeOf(user);
            var grid = field.Grid;
            var cost = new Dictionary<GridCell, int> { [start] = 0 };
            var frontier = new List<GridCell> { start };
            while (frontier.Count > 0)
            {
                var current = frontier.OrderBy(c => cost[c]).First();
                frontier.Remove(current);
                foreach (var next in grid.GetAdjacentCells(current).Where(c => !c.IsOccupied))
                {
                    int total = cost[current] + next.GetMovementCost();
                    if (total > reach || (cost.TryGetValue(next, out int known) && total >= known)) continue;
                    cost[next] = total;
                    frontier.Add(next);
                }
            }
            return cost.Keys.Where(c => c != start)
                .Union(grid.GetAdjacentCells(start).Where(c => !c.IsOccupied))
                .ToList();
        }
    }

    /// <summary>A bare-handed or weapon blow on an adjacent foe: strength × (1 + 0.2 per realm) − defence.</summary>
    public sealed class AttackAction : ICombatAction
    {
        public ActionType Type => ActionType.PhysicalAttack;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) =>
            user.IsActive && !user.HasActedThisTurn && CombatMath.IsOpponent(user, target)
            && CombatGrid.Distance(user.CurrentCell, target) == 1;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;
            var foe = target.Occupant;
            int damage = CombatMath.Landing((int)Math.Round(user.Strength * CombatMath.RealmFactor(user, 0.2)), user, foe, field);
            foe.TakeDamage(damage);
            user.HasActedThisTurn = true;
            field.Log.Info($"[Combat] {user.BaseData.FullName} strikes {foe.BaseData.FullName} ({damage}).");
        }
    }

    /// <summary>Guards until its next turn: incoming blows are halved.</summary>
    public sealed class DefendAction : ICombatAction
    {
        public ActionType Type => ActionType.Defend;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) => user.IsActive && !user.HasActedThisTurn;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;
            user.IsDefending = true;
            user.HasActedThisTurn = true;
        }
    }

    /// <summary>
    /// Tries to leave the battle: agility against the nearest foe's, agility / (agility + foe + 1).
    /// Win or lose, the attempt takes the whole turn.
    /// </summary>
    public sealed class FleeAction : ICombatAction
    {
        public ActionType Type => ActionType.Flee;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) => user.IsActive && !user.HasActedThisTurn;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;

            int foeAgility = field.NearestOpponent(user)?.Agility ?? 0;
            double chance = user.Agility / (double)(user.Agility + foeAgility + 1);
            if (field.Rng.Chance(chance))
            {
                user.Flee();
                field.Log.Info($"[Combat] {user.BaseData.FullName} escapes the battle.");
            }
            user.HasActedThisTurn = true;
            user.HasMovedThisTurn = true;
        }
    }

    /// <summary>A pill on oneself or an adjacent ally: healing or Qi, one of the stack used up.</summary>
    public sealed class ItemAction : ICombatAction
    {
        private readonly ItemData item;

        public ItemAction(ItemData item)
        {
            this.item = item;
        }

        public ActionType Type => ActionType.UseItem;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) =>
            user.IsActive && !user.HasActedThisTurn && item != null && item.Quantity > 0
            && CombatMath.IsFriend(user, target) && CombatGrid.Distance(user.CurrentCell, target) <= 1;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;
            var friend = target.Occupant;
            item.Quantity--;
            if (item.Type == ItemType.HealingPill) friend.Heal(item.Power);
            else friend.RestoreQi(item.Power);
            user.HasActedThisTurn = true;
        }
    }

    /// <summary>
    /// The Autumn Convergence Pill (audit §2.9, 📚 wiki): swallowed on one's own cell, it fills the Qi and strengthens the spell
    /// arts for the battle, for years of life; past the safe dose the foundation may collapse.
    /// </summary>
    public sealed class ConvergencePillAction : ICombatAction
    {
        public ActionType Type => ActionType.UseItem;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) =>
            user.IsActive && !user.HasActedThisTurn && user.TakeConvergencePill != null && (user.ConvergencePillsLeft?.Invoke() ?? 1) > 0
            && target != null && target == user.CurrentCell;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field) || !user.TakeConvergencePill()) return;
            var s = field.Convergence;
            user.ConvergenceDoses++;
            user.HasActedThisTurn = true;
            user.RestoreQi(user.MaxQi);
            user.SpellBoost = s.SpellBoost;
            user.BaseData.MaxLifespan -= s.LifespanCost;
            field.Log.Info($"[Combat] {user.BaseData.FullName} swallows an Autumn Convergence Pill (dose {user.ConvergenceDoses}).");
            if (user.ConvergenceDoses > s.SafeDoses && field.Rng.Chance(s.CollapseChance))
            {
                user.BaseData.MaxLifespan -= s.CollapseLifespan;
                user.Collapse();
                field.Log.Warning($"[Combat] {user.BaseData.FullName}'s foundation collapses under one pill too many.");
            }
        }
    }

    /// <summary>
    /// A known art: a striking art hits a foe in range for power × (1 + 0.2 per realm), a healing art
    /// heals an ally for power × (1 + 0.15 per realm); +25% when the element is the user's affinity, and
    /// Water arts strike 20% harder on water. Cultivation methods and arts without an effect are not for combat.
    /// </summary>
    public sealed class TechniqueAction : ICombatAction
    {
        private readonly TechniqueData technique;

        public TechniqueAction(TechniqueData technique)
        {
            this.technique = technique;
        }

        public ActionType Type => ActionType.Technique;
        public int QiCost => technique?.QiCost ?? 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field)
        {
            if (!user.IsActive || user.HasActedThisTurn || technique == null) return false;
            if (!user.BaseData.KnownTechniqueIDs.Contains(technique.ID)) return false;
            if (user.BaseData.Realm < technique.RequiredRealm || user.CurrentQi < technique.QiCost) return false;

            bool inRange = CombatGrid.Distance(user.CurrentCell, target) <= technique.Range;
            return technique.Effect switch
            {
                TechniqueEffect.Strike => CombatMath.IsOpponent(user, target) && inRange,
                TechniqueEffect.Heal => CombatMath.IsFriend(user, target) && inRange,
                _ => false
            };
        }

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;

            user.ConsumeQi(technique.QiCost);
            var other = target.Occupant;
            bool attuned = technique.DominantElement != Element.None && user.BaseData.Affinity == technique.DominantElement;

            if (technique.Effect == TechniqueEffect.Strike)
            {
                double raw = Math.Round(technique.PowerModifier * CombatMath.RealmFactor(user, 0.2)); // rounded before bonuses
                if (user.SpellBoost > 0 && technique.Kind == TechniqueKind.Spell) raw = Math.Round(raw * user.SpellFactor); // the Convergence Pill (audit §2.9)
                if (attuned) raw = Math.Round(raw * CombatMath.AffinityBonus);
                if (technique.DominantElement == Element.Water && target.Terrain == TerrainType.Water)
                    raw = Math.Round(raw * CombatMath.WaterTerrainBonus);
                int damage = CombatMath.Landing((int)Math.Round(raw), user, other, field);
                other.TakeDamage(damage);
                field.Log.Info($"[Combat] {user.BaseData.FullName} unleashes {technique.Name} on {other.BaseData.FullName} ({damage}).");
            }
            else
            {
                double heal = Math.Round(technique.PowerModifier * CombatMath.RealmFactor(user, 0.15));
                if (attuned) heal = Math.Round(heal * CombatMath.AffinityBonus);
                other.Heal((int)heal);
            }
            user.HasActedThisTurn = true;
        }
    }
}
