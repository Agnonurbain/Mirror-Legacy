using System;
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
    }

    /// <summary>Moves up to the unit's movement range (Manhattan distance), onto a free cell.</summary>
    public sealed class MoveAction : ICombatAction
    {
        public ActionType Type => ActionType.Move;
        public int QiCost => 0;

        public bool IsValid(CombatUnit user, GridCell target, BattleField field) =>
            user.IsActive && !user.HasMovedThisTurn && user.CurrentCell != null
            && target != null && !target.IsOccupied
            && CombatGrid.Distance(user.CurrentCell, target) <= user.MovementRange;

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;
            user.SetCell(target);
            user.HasMovedThisTurn = true;
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
            int damage = CombatMath.AfterDefence((int)Math.Round(user.Strength * CombatMath.RealmFactor(user, 0.2)), foe);
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
    /// A known technique: martial arts strike a foe in range for power × (1 + 0.2 per realm), support arts
    /// heal an ally for power × (1 + 0.15 per realm); +25% when the element is the user's affinity, and
    /// Water arts strike 20% harder on water. Cultivation methods are not for combat.
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
            return technique.Type switch
            {
                TechniqueType.MartialArt => CombatMath.IsOpponent(user, target) && inRange,
                TechniqueType.SupportArt => CombatMath.IsFriend(user, target) && inRange,
                _ => false
            };
        }

        public void Execute(CombatUnit user, GridCell target, BattleField field)
        {
            if (!IsValid(user, target, field)) return;

            user.ConsumeQi(technique.QiCost);
            var other = target.Occupant;
            bool attuned = technique.DominantElement != Element.None && user.BaseData.Affinity == technique.DominantElement;

            if (technique.Type == TechniqueType.MartialArt)
            {
                double raw = Math.Round(technique.PowerModifier * CombatMath.RealmFactor(user, 0.2)); // rounded before bonuses
                if (attuned) raw = Math.Round(raw * CombatMath.AffinityBonus);
                if (technique.DominantElement == Element.Water && target.Terrain == TerrainType.Water)
                    raw = Math.Round(raw * CombatMath.WaterTerrainBonus);
                int damage = CombatMath.AfterDefence((int)Math.Round(raw), other);
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
