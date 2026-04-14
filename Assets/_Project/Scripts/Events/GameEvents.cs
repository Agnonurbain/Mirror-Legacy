using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// Global Event Bus for decoupled communication between systems.
    /// Uses standard C# Actions. Systems subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // Time & Flow
        public static event Action<int> OnYearStarted;
        public static event Action<GamePhase> OnPhaseChanged;

        // Character Lifecycle
        public static event Action<CharacterData> OnCharacterBorn;
        public static event Action<CharacterData, DeathCause> OnCharacterDied;

        // Cultivation
        public static event Action<CharacterData, CultivationRealm> OnBreakthroughSuccess;
        public static event Action<CharacterData> OnBreakthroughFailed;

        // Economy
        public static event Action<int> OnSpiritStonesChanged;

        // Trigger Methods (Wrappers to safely invoke events)
        public static void TriggerYearStarted(int year) => OnYearStarted?.Invoke(year);
        public static void TriggerPhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public static void TriggerCharacterBorn(CharacterData character) => OnCharacterBorn?.Invoke(character);
        public static void TriggerCharacterDied(CharacterData character, DeathCause cause) => OnCharacterDied?.Invoke(character, cause);
        public static void TriggerBreakthroughSuccess(CharacterData character, CultivationRealm newRealm) => OnBreakthroughSuccess?.Invoke(character, newRealm);
        public static void TriggerBreakthroughFailed(CharacterData character) => OnBreakthroughFailed?.Invoke(character);
        public static void TriggerSpiritStonesChanged(int amount) => OnSpiritStonesChanged?.Invoke(amount);
    }
}
