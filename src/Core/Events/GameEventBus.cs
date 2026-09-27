using System;
using System.Collections.Generic;
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
        public event Action<CharacterData> OnMetalEssenceDemon;
        public event Action<string> OnMirrorSeized;                          // a power seized the mirror: the game is lost (§11.9)
        public event Action<CharacterData, string> OnMemberCaptured;         // a power holds a member (L6a)
        public event Action<CharacterData> OnMemberFreed;
        public event Action<string> OnAgentCaught;
        public event Action<string> OnClanAbsorbed;                          // the suzerain's grip complete: the clan is no more (defeat)
        public event Action<string, string> OnPowerAbsorbed;                 // a vassal power absorbed by its suzerain
        public event Action<IReadOnlyList<string>> OnCoalitionFormed;        // powers banded against the clan
        public event Action<string, string> OnCallToArms;                    // an ally of the clan attacked, and by whom
        public event Action<string, string> OnTheft;
        public event Action<string, string> OnDeed;                          // a deed of the clan that leaves a secret (kind, subject)
        public event Action<string> OnProbeSpotted;                          // a power caught probing the clan                         // what was stolen, and the thief when caught (else null)                           // an agent of a power in the clan's hands

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
        public void TriggerMirrorSeized(string faction) => OnMirrorSeized?.Invoke(faction);
        public void TriggerMemberCaptured(CharacterData member, string faction) => OnMemberCaptured?.Invoke(member, faction);
        public void TriggerMemberFreed(CharacterData member) => OnMemberFreed?.Invoke(member);
        public void TriggerAgentCaught(string faction) => OnAgentCaught?.Invoke(faction);
        public void TriggerClanAbsorbed(string suzerain) => OnClanAbsorbed?.Invoke(suzerain);
        public void TriggerPowerAbsorbed(string vassal, string suzerain) => OnPowerAbsorbed?.Invoke(vassal, suzerain);
        public void TriggerCoalitionFormed(IReadOnlyList<string> members) => OnCoalitionFormed?.Invoke(members);
        public void TriggerCallToArms(string ally, string attacker) => OnCallToArms?.Invoke(ally, attacker);
        public void TriggerTheft(string what, string thief) => OnTheft?.Invoke(what, thief);
        public void TriggerDeed(string kind, string subject) => OnDeed?.Invoke(kind, subject);
        public void TriggerProbeSpotted(string prober) => OnProbeSpotted?.Invoke(prober);
        public void TriggerBreakthroughFailed(CharacterData character) => OnBreakthroughFailed?.Invoke(character);
        public void TriggerSpiritStonesChanged(int total) => OnSpiritStonesChanged?.Invoke(total);
        public void TriggerRandomEventOccurred(RandomEventData evt) => OnRandomEventOccurred?.Invoke(evt);
        public void TriggerStoryEventRaised(StoryEventData evt) => OnStoryEventRaised?.Invoke(evt);
        public void TriggerGameOver(bool victory) => OnGameOver?.Invoke(victory);
    }
}
