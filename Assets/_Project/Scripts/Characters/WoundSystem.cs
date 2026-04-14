using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Evaluates vitality loss after combat and applies long-term wounds or Dao Wounds.
    /// </summary>
    public class WoundSystem : MonoBehaviour
    {
        public static WoundSystem Instance { get; private set; }

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
        /// Called at the end of combat for each surviving character to evaluate their injuries.
        /// </summary>
        /// <param name="character">The base character data</param>
        /// <param name="maxVitality">The max HP they had in combat</param>
        /// <param name="currentVitality">The remaining HP they had at the end of combat</param>
        public void EvaluatePostCombatWounds(CharacterData character, int maxVitality, int currentVitality)
        {
            if (!character.IsAlive || maxVitality <= 0) return;

            float healthPercentage = (float)currentVitality / maxVitality;
            float damageTakenPercentage = 1f - healthPercentage;

            if (damageTakenPercentage <= 0f)
            {
                Debug.Log($"[WoundSystem] {character.FullName} emerged unscathed.");
                return;
            }

            if (damageTakenPercentage > 0.9f)
            {
                ApplyCriticalWound(character);
            }
            else if (damageTakenPercentage > 0.6f)
            {
                ApplySevereWound(character);
            }
            else if (damageTakenPercentage > 0.3f)
            {
                ApplyModerateWound(character);
            }
            else
            {
                ApplyLightWound(character);
            }
        }

        private void ApplyLightWound(CharacterData character)
        {
            Debug.Log($"[WoundSystem] {character.FullName} suffered a Light Wound. Will heal in 1-3 months.");
            // In a full implementation, we would add a "Wound" object to the character's status list
            // with a duration of 1-3 "ticks" (months).
        }

        private void ApplyModerateWound(CharacterData character)
        {
            Debug.Log($"[WoundSystem] {character.FullName} suffered a Moderate Wound. Stats reduced by 10% during healing.");
            MentalStabilitySystem.Instance.ApplyModifier(character, -5);
        }

        private void ApplySevereWound(CharacterData character)
        {
            Debug.LogWarning($"[WoundSystem] {character.FullName} suffered a Severe Wound! Healing will take years.");
            MentalStabilitySystem.Instance.ApplyModifier(character, -15);

            // 20% chance of a permanent Dao Wound
            if (Random.value <= 0.2f)
            {
                ApplyDaoWound(character);
            }
        }

        private void ApplyCriticalWound(CharacterData character)
        {
            Debug.LogError($"[WoundSystem] {character.FullName} suffered a CRITICAL Wound! Barely survived.");
            MentalStabilitySystem.Instance.ApplyModifier(character, -30);

            // 80% chance of a permanent Dao Wound
            if (Random.value <= 0.8f)
            {
                ApplyDaoWound(character);
            }
        }

        /// <summary>
        /// A permanent, unhealable injury that reduces max lifespan and stats.
        /// </summary>
        public void ApplyDaoWound(CharacterData character)
        {
            Debug.LogError($"[WoundSystem] TRAGEDY! {character.FullName} has suffered a permanent DAO WOUND.");
            
            // Reduce max lifespan by 20%
            int lifespanPenalty = Mathf.RoundToInt(character.MaxLifespan * 0.2f);
            character.MaxLifespan = Mathf.Max(character.Age + 1, character.MaxLifespan - lifespanPenalty);
            
            // In a full implementation, we add a "DaoWound" flag to CharacterData that permanently debuffs them.
            
            // Huge mental stability hit
            MentalStabilitySystem.Instance.ApplyModifier(character, -20);
        }
    }
}
