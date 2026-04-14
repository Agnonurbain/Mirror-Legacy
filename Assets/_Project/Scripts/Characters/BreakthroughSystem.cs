using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Handles the logic, probabilities, and consequences of realm breakthroughs.
    /// </summary>
    public class BreakthroughSystem : MonoBehaviour
    {
        public static BreakthroughSystem Instance { get; private set; }

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
        /// Calculates the success rate (0 to 100) of a breakthrough attempt.
        /// </summary>
        public int CalculateSuccessRate(CharacterData character)
        {
            int baseRisk = GetBaseRiskForRealm(character.Realm);
            int successRate = 100 - baseRisk;

            // Modifier: Spiritual Root (Bonus if above minimum required for the next realm)
            int minRootRequired = GetMinRootForNextRealm(character.Realm);
            if (character.SpiritualRoot > minRootRequired)
            {
                successRate += (character.SpiritualRoot - minRootRequired) / 5; // +1% per 5 points above min
            }

            // Modifier: Mental Stability
            if (character.MentalStability < 50)
            {
                successRate -= 20;
            }

            // Modifier: Age (Penalty if old)
            float lifePercentage = (float)character.Age / character.MaxLifespan;
            if (lifePercentage > 0.8f)
            {
                successRate -= 10;
            }

            return Mathf.Clamp(successRate, 1, 99); // Always a 1% chance of failure or success
        }

        public void AttemptBreakthrough(CharacterData character)
        {
            int successRate = CalculateSuccessRate(character);
            int roll = Random.Range(1, 101); // 1 to 100

            Debug.Log($"[BreakthroughSystem] {character.FullName} attempting breakthrough. Success Rate: {successRate}%. Roll: {roll}");

            if (roll <= successRate)
            {
                // SUCCESS
                character.Realm = GetNextRealm(character.Realm);
                character.CultivationXP = 0; // Reset XP for the new realm
                
                Debug.Log($"[BreakthroughSystem] SUCCESS! {character.FullName} reached {character.Realm}!");
                GameEvents.TriggerBreakthroughSuccess(character, character.Realm);
            }
            else
            {
                // FAILURE
                int failureSeverity = Random.Range(1, 101);
                
                if (failureSeverity <= 70)
                {
                    // Minor Failure
                    Debug.Log($"[BreakthroughSystem] MINOR FAILURE for {character.FullName}.");
                }
                else if (failureSeverity <= 95)
                {
                    // Major Failure (Dao Wound in full implementation)
                    Debug.Log($"[BreakthroughSystem] MAJOR FAILURE for {character.FullName}. Suffered Dao Wound.");
                }
                else
                {
                    // Catastrophic Failure
                    Debug.Log($"[BreakthroughSystem] CATASTROPHIC FAILURE for {character.FullName}. Qi Deviation!");
                    if (MirrorChronicles.Characters.AgingAndDeathSystem.Instance != null)
                    {
                        MirrorChronicles.Characters.AgingAndDeathSystem.Instance.Die(character, DeathCause.QiDeviation);
                    }
                    else
                    {
                        GameEvents.TriggerCharacterDied(character, DeathCause.QiDeviation);
                    }
                }

                GameEvents.TriggerBreakthroughFailed(character);
            }
        }

        private int GetBaseRiskForRealm(CultivationRealm currentRealm)
        {
            switch (currentRealm)
            {
                case CultivationRealm.Embryonic: return 5;
                case CultivationRealm.QiRefinement: return 15;
                case CultivationRealm.Foundation: return 30;
                case CultivationRealm.PurpleMansion: return 50;
                case CultivationRealm.GoldenCore: return 70;
                default: return 100;
            }
        }

        private int GetMinRootForNextRealm(CultivationRealm currentRealm)
        {
            switch (currentRealm)
            {
                case CultivationRealm.Embryonic: return 10;
                case CultivationRealm.QiRefinement: return 30;
                case CultivationRealm.Foundation: return 50;
                case CultivationRealm.PurpleMansion: return 70;
                case CultivationRealm.GoldenCore: return 90;
                default: return 100;
            }
        }

        private CultivationRealm GetNextRealm(CultivationRealm current)
        {
            if (current == CultivationRealm.DaoEmbryo) return current;
            return current + 1;
        }
    }
}
