using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// Handles the random and scripted events during the Event Phase (Phase 2).
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

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

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Events)
            {
                TriggerYearlyEvent();
            }
        }

        /// <summary>
        /// Rolls for a random event based on weighted probabilities.
        /// </summary>
        private void TriggerYearlyEvent()
        {
            Debug.Log("[EventManager] --- Rolling Yearly Event ---");

            int roll = Random.Range(1, 101);

            // Simplified weighted table for prototype
            if (roll <= 20)
            {
                TriggerMonsterAttack();
            }
            else if (roll <= 35)
            {
                TriggerWanderingMerchant();
            }
            else if (roll <= 45)
            {
                TriggerRuinsDiscovery();
            }
            else if (roll <= 55)
            {
                TriggerInternalBetrayal();
            }
            else
            {
                Debug.Log("[EventManager] A peaceful year passes with no major events.");
            }

            // In a full game, the UI would wait for the player to resolve the event
            // before allowing TimeManager.Instance.AdvancePhase() to be called.
        }

        private void TriggerMonsterAttack()
        {
            Debug.LogWarning("[EventManager] EVENT: Monster Attack! The domain is under siege.");
            // Would transition to TacticalCombat scene
        }

        private void TriggerWanderingMerchant()
        {
            Debug.Log("[EventManager] EVENT: Wandering Merchant arrived. (UI opens)");
            // Would open a shop UI
        }

        private void TriggerRuinsDiscovery()
        {
            Debug.Log("[EventManager] EVENT: Ancient Ruins discovered nearby. Send an expedition?");
            // Would open a choice UI
        }

        private void TriggerInternalBetrayal()
        {
            Debug.LogWarning("[EventManager] EVENT: Internal Betrayal! A member with low stability is plotting.");
            // Would trigger a specific narrative event
        }
    }
}
