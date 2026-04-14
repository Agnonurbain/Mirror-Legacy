using System;
using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A wrapper around CharacterData or EnemyData specifically for the tactical combat scene.
    /// Manages temporary combat stats like CurrentVitality and CurrentQi.
    /// </summary>
    public class CombatUnit : MonoBehaviour
    {
        public CharacterData BaseData { get; private set; }
        public bool IsAlly { get; private set; }
        
        public int MaxVitality { get; private set; }
        public int CurrentVitality { get; private set; }
        
        public int MaxQi { get; private set; }
        public int CurrentQi { get; private set; }

        public GridCell CurrentCell { get; private set; }
        
        // Temporary combat states
        public bool IsDefending { get; set; }
        public bool HasActedThisTurn { get; set; }
        public bool HasMovedThisTurn { get; set; }

        // Core stats (Derived from BaseData in a full implementation, hardcoded for prototype)
        public int Agility { get; private set; } = 10;
        public int Strength { get; private set; } = 10;
        public int Defense { get; private set; } = 5;

        /// <summary>
        /// Initializes the unit with its base data and calculates combat stats.
        /// </summary>
        public void Initialize(CharacterData data, bool isAlly)
        {
            BaseData = data;
            IsAlly = isAlly;

            // Calculate Vitality: Constitution * 10 * (1 + Realm * 0.5)
            // For prototype, we assume a base Constitution of 10.
            int constitution = 10; 
            float realmMultiplier = 1f + ((int)data.Realm * 0.5f);
            
            MaxVitality = Mathf.RoundToInt(constitution * 10 * realmMultiplier);
            CurrentVitality = MaxVitality;

            // Calculate Qi: Based on Spiritual Root and Realm
            MaxQi = data.SpiritualRoot * ((int)data.Realm + 1);
            CurrentQi = MaxQi;

            // Base stats scaling
            Agility = 10 + ((int)data.Realm * 5);
            Strength = 10 + ((int)data.Realm * 5);
            Defense = 5 + ((int)data.Realm * 3);

            Debug.Log($"[CombatUnit] Initialized {data.FullName} (Ally: {isAlly}) - HP: {MaxVitality}, Qi: {MaxQi}, Agi: {Agility}");
        }

        public void SetCell(GridCell newCell)
        {
            if (CurrentCell != null)
            {
                CurrentCell.Occupant = null;
            }

            CurrentCell = newCell;
            
            if (CurrentCell != null)
            {
                CurrentCell.Occupant = this;
                // Update physical position (assuming the cell size is 1x1 unit)
                transform.position = new Vector3(newCell.X, 0, newCell.Y);
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDefending)
            {
                amount /= 2; // 50% reduction
                Debug.Log($"[CombatUnit] {BaseData.FullName} defended! Damage reduced to {amount}.");
            }

            CurrentVitality = Mathf.Max(0, CurrentVitality - amount);
            Debug.Log($"[CombatUnit] {BaseData.FullName} took {amount} damage. HP: {CurrentVitality}/{MaxVitality}");

            if (CurrentVitality <= 0)
            {
                Die();
            }
        }

        public bool ConsumeQi(int amount)
        {
            if (CurrentQi >= amount)
            {
                CurrentQi -= amount;
                return true;
            }
            return false;
        }

        private void Die()
        {
            Debug.Log($"[CombatUnit] {BaseData.FullName} has fallen in combat!");
            SetCell(null);
            gameObject.SetActive(false);
            // TurnManager will handle removing them from the queue
        }

        public void ResetTurnState()
        {
            IsDefending = false;
            HasActedThisTurn = false;
            HasMovedThisTurn = false;

            // Handle Concentrated Qi terrain bonus
            if (CurrentCell != null && CurrentCell.Terrain == TerrainType.ConcentratedQi)
            {
                int regen = Mathf.RoundToInt(MaxQi * 0.05f); // +5% Qi
                CurrentQi = Mathf.Min(MaxQi, CurrentQi + regen);
                Debug.Log($"[CombatUnit] {BaseData.FullName} regenerated {regen} Qi from terrain.");
            }
        }
    }
}
