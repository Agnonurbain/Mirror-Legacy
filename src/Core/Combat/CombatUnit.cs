using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A fighter on the grid, wrapping a character with its combat state: vitality and Qi derived from
    /// the realm and the spiritual root, and whether it is down or has fled.
    /// </summary>
    public sealed class CombatUnit
    {
        public const int BaseConstitution = 10;
        private const double ConcentratedQiRegen = 0.05;

        public CharacterData BaseData { get; }
        public bool IsAlly { get; }
        public AIStrategyType Strategy { get; }

        public int MaxVitality { get; }
        public int CurrentVitality { get; private set; }
        public int MaxQi { get; }
        public int CurrentQi { get; private set; }
        public int Agility { get; }
        public int Strength { get; }
        public int Defense { get; }

        public GridCell CurrentCell { get; private set; }
        public bool IsDefending { get; set; }
        public bool HasActedThisTurn { get; set; }
        public bool HasMovedThisTurn { get; set; }
        public bool HasFled { get; private set; }

        public bool IsDown => CurrentVitality <= 0;
        public bool IsActive => !IsDown && !HasFled;

        /// <summary>Cells it may move per turn: one per ten agility, at least one.</summary>
        public int MovementRange => Math.Max(1, Agility / 10);

        public CombatUnit(CharacterData data, bool isAlly, AIStrategyType strategy = AIStrategyType.Aggressive)
        {
            BaseData = data;
            IsAlly = isAlly;
            Strategy = strategy;

            int realm = (int)data.Realm;
            MaxVitality = (int)Math.Round(BaseConstitution * 10 * (1 + realm * 0.5));
            MaxQi = data.SpiritualRoot * (realm + 1);
            Agility = 10 + realm * 5;
            Strength = 10 + realm * 5;
            Defense = 5 + realm * 3;
            CurrentVitality = MaxVitality;
            CurrentQi = MaxQi;
        }

        public void SetCell(GridCell cell)
        {
            if (CurrentCell != null) CurrentCell.Occupant = null;
            CurrentCell = cell;
            if (cell != null) cell.Occupant = this;
        }

        public void Heal(int amount) => CurrentVitality = Math.Min(MaxVitality, CurrentVitality + Math.Max(0, amount));

        /// <summary>Defending halves the blow; a unit brought to zero falls and leaves the grid.</summary>
        public void TakeDamage(int amount)
        {
            if (IsDefending) amount /= 2;
            CurrentVitality = Math.Max(0, CurrentVitality - Math.Max(0, amount));
            if (IsDown) SetCell(null);
        }

        public bool ConsumeQi(int amount)
        {
            if (CurrentQi < amount) return false;
            CurrentQi -= amount;
            return true;
        }

        public void RestoreQi(int amount) => CurrentQi = Math.Min(MaxQi, CurrentQi + Math.Max(0, amount));

        /// <summary>Leaves the battle alive.</summary>
        public void Flee()
        {
            HasFled = true;
            SetCell(null);
        }

        /// <summary>Start of its turn: guard down, actions refreshed, Qi drawn from concentrated Qi terrain.</summary>
        public void ResetTurnState()
        {
            IsDefending = false;
            HasActedThisTurn = false;
            HasMovedThisTurn = false;
            if (CurrentCell != null && CurrentCell.Terrain == TerrainType.ConcentratedQi)
                RestoreQi((int)Math.Round(MaxQi * ConcentratedQiRegen));
        }
    }
}
