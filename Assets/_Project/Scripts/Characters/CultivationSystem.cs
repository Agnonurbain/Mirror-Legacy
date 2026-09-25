using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Handles the yearly cultivation progress of characters on the power ladder (LORE.md §5).
    /// Sub-levels without a trial advance automatically; trials wait for the Breakthrough phase.
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
            if (!SpiritualOrificeRules.CanCultivate(character))
                return; // a mortal without orifice or seed gathers no Qi

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

            AdvanceSubLevels(character);
        }

        /// <summary>
        /// Spends XP on every sub-level that needs no trial; stops before a trial or an unavailable step.
        /// </summary>
        public void AdvanceSubLevels(CharacterData character)
        {
            if (!SpiritualOrificeRules.CanCultivate(character)) return;

            while (true)
            {
                int required = PowerLadder.XpForNextStage(character.Realm);
                var step = PowerLadder.Next(character.Realm, character.RealmStage);
                if (required <= 0 || character.CultivationXP < required || !step.IsAvailable) return;

                if (step.Trial != TrialKind.None)
                {
                    Debug.Log($"[CultivationSystem] {character.FullName} is ready for the {step.Trial} trial.");
                    return;
                }

                character.CultivationXP -= required;
                ApplyStep(character, step);
            }
        }

        /// <summary>
        /// True when the character has the XP for a step gated by a trial that can be attempted now.
        /// </summary>
        public static bool IsReadyForTrial(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            return character.IsAlive
                && SpiritualOrificeRules.CanCultivate(character)
                && step.IsAvailable
                && step.Trial != TrialKind.None
                && character.CultivationXP >= PowerLadder.XpForNextStage(character.Realm);
        }

        /// <summary>
        /// Moves the character to the step's realm and stage and updates the lifespan. Only trials passed
        /// in the Breakthrough phase raise the breakthrough event (and its rewards).
        /// </summary>
        public static void ApplyStep(CharacterData character, AdvancementStep step)
        {
            character.Realm = step.TargetRealm;
            character.RealmStage = step.TargetStage;
            character.MaxLifespan = PowerLadder.MaxLifespan(character.Realm, character.RealmStage);

            Debug.Log($"[CultivationSystem] {character.FullName} reached {RankCatalog.DisplayName(character)}.");
        }
    }
}
