using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;
using MirrorChronicles.Clan;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Manages the Mental Stability (0-100) of characters.
    /// Listens to global events to apply modifiers.
    /// </summary>
    public class MentalStabilitySystem : MonoBehaviour
    {
        public static MentalStabilitySystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterDied += HandleCharacterDeath;
            GameEvents.OnBreakthroughSuccess += HandleBreakthroughSuccess;
            GameEvents.OnBreakthroughFailed += HandleBreakthroughFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterDied -= HandleCharacterDeath;
            GameEvents.OnBreakthroughSuccess -= HandleBreakthroughSuccess;
            GameEvents.OnBreakthroughFailed -= HandleBreakthroughFailed;
        }

        public void ApplyModifier(CharacterData character, int amount)
        {
            if (!character.IsAlive) return;

            character.MentalStability = Mathf.Clamp(character.MentalStability + amount, 0, 100);
            
            if (character.MentalStability <= 0)
            {
                Debug.LogWarning($"[MentalStabilitySystem] {character.FullName} has reached 0 Mental Stability! Qi Deviation imminent.");
                // In a full implementation, this triggers a specific event or death
            }
        }

        private void HandleCharacterDeath(CharacterData deceased, DeathCause cause)
        {
            // Apply grief penalty to close relatives
            foreach (var member in ClanManager.Instance.LivingMembers)
            {
                if (member.ID == deceased.ID) continue;

                bool isCloseRelative = 
                    member.FatherID == deceased.ID || 
                    member.MotherID == deceased.ID || 
                    member.SpouseID == deceased.ID ||
                    deceased.FatherID == member.ID ||
                    deceased.MotherID == member.ID;

                if (isCloseRelative)
                {
                    ApplyModifier(member, -15);
                    Debug.Log($"[MentalStabilitySystem] {member.FullName} lost 15 Stability due to the death of {deceased.FullName}.");
                }
            }
        }

        private void HandleBreakthroughSuccess(CharacterData character, CultivationRealm newRealm)
        {
            ApplyModifier(character, +10);
        }

        private void HandleBreakthroughFailed(CharacterData character)
        {
            ApplyModifier(character, -10);
        }
    }
}
