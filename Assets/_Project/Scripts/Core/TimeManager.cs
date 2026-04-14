using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Controls the yearly cycle and the 4 phases of gameplay.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        public int CurrentYear { get; private set; } = 1;
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Management;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartGameLoop()
        {
            CurrentYear = 1;
            CurrentPhase = GamePhase.Management;
            
            Debug.Log($"[TimeManager] --- Year {CurrentYear} Started ---");
            GameEvents.TriggerYearStarted(CurrentYear);
            GameEvents.TriggerPhaseChanged(CurrentPhase);
        }

        /// <summary>
        /// Advances to the next phase. If it's the last phase, advances the year.
        /// Can be called by UI buttons (e.g., "End Turn" button).
        /// </summary>
        public void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case GamePhase.Management:
                    CurrentPhase = GamePhase.Events;
                    break;
                case GamePhase.Events:
                    CurrentPhase = GamePhase.Breakthrough;
                    break;
                case GamePhase.Breakthrough:
                    CurrentPhase = GamePhase.Inheritance;
                    break;
                case GamePhase.Inheritance:
                    AdvanceYear();
                    return; // AdvanceYear handles the phase reset and event triggers
            }

            Debug.Log($"[TimeManager] Advanced to Phase: {CurrentPhase}");
            GameEvents.TriggerPhaseChanged(CurrentPhase);
        }

        private void AdvanceYear()
        {
            CurrentYear++;
            CurrentPhase = GamePhase.Management;
            
            Debug.Log($"[TimeManager] --- Happy New Year! Welcome to Year {CurrentYear} ---");
            
            GameEvents.TriggerYearStarted(CurrentYear);
            GameEvents.TriggerPhaseChanged(CurrentPhase);
        }
    }
}
