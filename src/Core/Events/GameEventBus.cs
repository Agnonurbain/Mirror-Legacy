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

        // Economy
        public event Action<int> OnSpiritStonesChanged;

        public void TriggerYearStarted(int year) => OnYearStarted?.Invoke(year);
        public void TriggerPhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public void TriggerCharacterBorn(CharacterData character) => OnCharacterBorn?.Invoke(character);
        public void TriggerCharacterDied(CharacterData character, DeathCause cause) => OnCharacterDied?.Invoke(character, cause);
        public void TriggerAncestorAscended(CharacterData character) => OnAncestorAscended?.Invoke(character);
        public void TriggerBreakthroughSuccess(CharacterData character, CultivationRealm newRealm) => OnBreakthroughSuccess?.Invoke(character, newRealm);
        public void TriggerBreakthroughFailed(CharacterData character) => OnBreakthroughFailed?.Invoke(character);
        public void TriggerSpiritStonesChanged(int total) => OnSpiritStonesChanged?.Invoke(total);
    }
}
