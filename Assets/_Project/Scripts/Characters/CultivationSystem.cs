using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Handles the yearly cultivation progress of characters.
    /// </summary>
    public class CultivationSystem : MonoBehaviour
    {
        public static CultivationSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Processes cultivation XP gain for a single year.
        /// Called during the Management phase resolution.
        /// </summary>
        public void ProcessYearlyCultivation(CharacterData character)
        {
            if (!character.IsAlive || character.CurrentTask != TaskType.Cultivation)
                return;

            // Base XP gain depends heavily on Spiritual Root
            int baseGain = 10 + (character.SpiritualRoot / 2);

            // Modifiers (e.g., Training Room, Mentor, etc. will be added here in later phases)
            float multiplier = 1.0f;

            // Apply Mental Stability penalty if too low
            if (character.MentalStability < 50)
            {
                multiplier *= 0.8f;
            }

            int finalGain = Mathf.RoundToInt(baseGain * multiplier);
            character.CultivationXP += finalGain;

            Debug.Log($"[CultivationSystem] {character.FullName} gained {finalGain} XP. Total: {character.CultivationXP}");

            CheckBreakthroughEligibility(character);
        }

        /// <summary>
        /// Checks if the character has enough XP to attempt a breakthrough to the next realm.
        /// </summary>
        private void CheckBreakthroughEligibility(CharacterData character)
        {
            int requiredXP = GetRequiredXPForNextRealm(character.Realm);
            
            if (requiredXP > 0 && character.CultivationXP >= requiredXP)
            {
                // Character is ready for a breakthrough. 
                // In a full game, this would notify the UI to allow the player to trigger it during Phase 3.
                Debug.Log($"[CultivationSystem] {character.FullName} is ready to breakthrough from {character.Realm}!");
            }
        }

        private int GetRequiredXPForNextRealm(CultivationRealm currentRealm)
        {
            switch (currentRealm)
            {
                case CultivationRealm.Embryonic: return 100;
                case CultivationRealm.QiRefinement: return 500;
                case CultivationRealm.Foundation: return 2000;
                case CultivationRealm.PurpleMansion: return 10000;
                case CultivationRealm.GoldenCore: return 50000;
                case CultivationRealm.DaoEmbryo: return -1; // Max realm
                default: return -1;
            }
        }
    }
}
