using System;
using System.Linq;
using UnityEngine;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Runs the trials of the power ladder (LORE.md §5): chakra trials and the Foundation wall.
    /// Every ready member attempts their trial during the Breakthrough phase.
    /// </summary>
    public class BreakthroughSystem : MonoBehaviour
    {
        public static BreakthroughSystem Instance { get; private set; }

        private const int AncestralShieldBonus = 30;

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
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Breakthrough)
                ProcessBreakthroughPhase();
        }

        /// <summary>
        /// Every living member ready for a trial attempts it. Before this, nothing in the game
        /// ever called AttemptBreakthrough.
        /// </summary>
        public void ProcessBreakthroughPhase()
        {
            var clan = ClanManager.Instance;
            if (clan == null) return;

            foreach (var member in clan.LivingMembers.ToList())
            {
                if (CultivationSystem.IsReadyForTrial(member))
                    AttemptBreakthrough(member);
            }
        }

        /// <summary>
        /// Calculates the success rate (1 to 99) of the trial gating the next step, 0 when none is due.
        /// </summary>
        public int CalculateSuccessRate(CharacterData character)
        {
            return BreakthroughRules.SuccessRate(character);
        }

        public bool AncestralShieldActive { get; set; }

        public void ActivateAncestralShield()
        {
            if (MirrorSystem.Instance != null && MirrorSystem.Instance.UseAncestralShield())
                AncestralShieldActive = true;
        }

        public void AttemptBreakthrough(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            if (!step.IsAvailable || step.Trial == TrialKind.None)
            {
                Debug.LogWarning($"[BreakthroughSystem] {character.FullName} has no trial to attempt ({RankCatalog.DisplayName(character)}).");
                return;
            }

            int successRate = CalculateSuccessRate(character);
            if (AncestralShieldActive)
            {
                successRate = Math.Min(99, successRate + AncestralShieldBonus);
                AncestralShieldActive = false;
                Debug.Log($"[BreakthroughSystem] Ancestral Shield grants +{AncestralShieldBonus}% success!");
            }

            int roll = UnityEngine.Random.Range(1, 101);
            int severityRoll = UnityEngine.Random.Range(1, 101);
            var outcome = BreakthroughRules.Resolve(step.Trial, successRate, character.Age, roll, severityRoll);
            int required = PowerLadder.XpForNextStage(character.Realm);

            Debug.Log($"[BreakthroughSystem] {character.FullName} attempts the {step.Trial} trial. Success Rate: {successRate}%. Roll: {roll}. Outcome: {outcome}");

            switch (outcome)
            {
                case BreakthroughOutcome.Success:
                    bool sameRealm = step.TargetRealm == character.Realm;
                    character.CultivationXP = Math.Max(0, character.CultivationXP - required);
                    CultivationSystem.ApplyStep(character, step); // raises the event on a new realm
                    if (sameRealm)
                        GameEvents.TriggerBreakthroughSuccess(character, character.Realm);
                    return;
                case BreakthroughOutcome.MinorFailure:
                    character.CultivationXP = Math.Max(0, character.CultivationXP - required / 2);
                    break;
                case BreakthroughOutcome.MajorFailure:
                    character.CultivationXP = 0;
                    break;
                case BreakthroughOutcome.QiDeviationDeath:
                    Die(character, DeathCause.QiDeviation);
                    break;
                case BreakthroughOutcome.SpiritualDissolution:
                    Die(character, DeathCause.SpiritualDissolution);
                    break;
            }

            GameEvents.TriggerBreakthroughFailed(character);
        }

        private static void Die(CharacterData character, DeathCause cause)
        {
            Debug.Log($"[BreakthroughSystem] {character.FullName} dies: {cause}.");
            if (AgingAndDeathSystem.Instance != null)
                AgingAndDeathSystem.Instance.Die(character, cause);
            else
                GameEvents.TriggerCharacterDied(character, cause);
        }
    }
}
