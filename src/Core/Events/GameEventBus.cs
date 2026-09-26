using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// Event bus of one game session. Handlers run in subscription order, so the session wires its
    /// systems in a fixed order and every reaction is deterministic. One bus per session: tests and
    /// reloaded games never share listeners.
    /// </summary>
    public sealed class GameEventBus
    {
        /// <summary>Someone harms someone else (devours or grafts their foundation…): oaths are checked (L4d).</summary>
        public event Action<CharacterData, CharacterData> OnHarm;

        public void TriggerHarm(CharacterData actor, CharacterData victim) => OnHarm?.Invoke(actor, victim);

        // Time & flow
        public event Action<int> OnYearStarted;
        public event Action<GamePhase> OnPhaseChanged;

        // Character lifecycle
        public event Action<CharacterData> OnCharacterBorn;               // newborns and members joining the clan
        public event Action<CharacterData, DeathCause> OnCharacterDied;
        public event Action<CharacterData> OnAncestorAscended;

        // Cultivation
        public event Action<CharacterData, CultivationRealm> OnBreakthroughSuccess;
        public event Action<CharacterData> OnBreakthroughFailed;
        public event Action<CharacterData> OnMetalEssenceDemon;             // a failed Golden Core comes alive (regional threat, L6)

        // Economy
        public event Action<int> OnSpiritStonesChanged;

        // Events & outcome
        public event Action<RandomEventData> OnRandomEventOccurred;
        public event Action<StoryEventData> OnStoryEventRaised;
        public event Action<bool> OnGameOver;                              // true: victory, false: defeat

        public void TriggerYearStarted(int year) => OnYearStarted?.Invoke(year);
        public void TriggerPhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public void TriggerCharacterBorn(CharacterData character) => OnCharacterBorn?.Invoke(character);
        public void TriggerCharacterDied(CharacterData character, DeathCause cause) => OnCharacterDied?.Invoke(character, cause);
        public void TriggerAncestorAscended(CharacterData character) => OnAncestorAscended?.Invoke(character);
        public void TriggerBreakthroughSuccess(CharacterData character, CultivationRealm newRealm) => OnBreakthroughSuccess?.Invoke(character, newRealm);
        public void TriggerMetalEssenceDemon(CharacterData character) => OnMetalEssenceDemon?.Invoke(character);
        public void TriggerBreakthroughFailed(CharacterData character) => OnBreakthroughFailed?.Invoke(character);
        public void TriggerSpiritStonesChanged(int total) => OnSpiritStonesChanged?.Invoke(total);
        public void TriggerRandomEventOccurred(RandomEventData evt) => OnRandomEventOccurred?.Invoke(evt);
        public void TriggerStoryEventRaised(StoryEventData evt) => OnStoryEventRaised?.Invoke(evt);
        public void TriggerGameOver(bool victory) => OnGameOver?.Invoke(victory);
    }
}
