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

        // Cultivation
        public event Action<CharacterData, CultivationRealm> OnBreakthroughSuccess;
        public event Action<CharacterData> OnBreakthroughFailed;
        public event Action<CharacterData> OnPurpleMansionAscent;          // a member of the clan newly stands at the Purple Mansion
        public event Action<CharacterData> OnMetalEssenceDemon;
        public event Action<CharacterData, GoldenCoreState, GoldenCoreState> OnPositionTaken; // member, position, the standing it rose from
        public event Action<string> OnMirrorSeized;                          // a power seized the mirror: the game is lost (§11.9)
        public event Action<CharacterData, string> OnMemberCaptured;         // a power holds a member (L6a)
        public event Action<CharacterData> OnMemberFreed;
        public event Action<string> OnAgentCaught;                           // an agent of a power in the clan's hands
        public event Action<string> OnClanAbsorbed;                          // the suzerain's grip complete: the clan is no more (defeat)
        public event Action<string, string> OnPowerAbsorbed;                 // a vassal power absorbed by its suzerain
        public event Action<IReadOnlyList<string>> OnCoalitionFormed;        // powers banded against the clan
        public event Action<string, string> OnCallToArms;                    // an ally of the clan attacked, and by whom
        public event Action<string, string> OnTheft;                         // what was stolen, and the thief when caught (else null)
        public event Action<string, string> OnDeed;                          // a deed of the clan that leaves a secret (kind, subject)
        public event Action<string> OnProbeSpotted;                          // a power caught probing the clan
        public event Action<string> OnClanStruck;                            // a power strikes the clan (proof or none)
        public event Action<string> OnTreatyBetrayed;                        // a power betrays its treaty with the clan
        public event Action<string> OnBlackmail;                             // a power demands stones for its silence
        public event Action<string> OnSpyUnmasked;                           // the mirror unmasks a power's spy
        public event Action<string> OnPatronWrath;                           // a great partner turns on the clan
        public event Action<string, string> OnWarBegun;                      // attacker, defender (the clan as SecretBook.ClanHolder)
        public event Action<string, string> OnPeace;                         // victor or party, the other
        public event Action<string, ChallengeOutcome> OnChallengeSettled;    // a rival's challenge answered (or not)
        public event Action<string> OnExtortion;                             // a greedy power demands the clan pay for its « protection »
        public event Action<string> OnExtortionRefused;                      // refused (or unanswered): it makes war on the clan
        public event Action<string> OnDaoHuntFoiled;                         // a power struck at a ripe Dao of the clan, and was driven off
        public event Action<string, bool> OnHunt;                            // a hunt carried out: the beast, and whether it was taken

        // Economy
        public event Action<int> OnSpiritStonesChanged;

        // Events & outcome
        public event Action<RandomEventData> OnRandomEventOccurred;
        public event Action<StoryEventData> OnStoryEventRaised;
        public event Action<Diplomacy.SponsorOffer> OnPatronOffer;          // a patron offers an ascent method (§11.10)
        public event Action<Diplomacy.Sponsorship, PatronDesign> OnPatronDesignDue; // a patron's design falls due
        public event Action<Diplomacy.Sponsorship, PatronDesign> OnPatronDemand;   // a design awaits the clan's answer
        public event Action<Diplomacy.Sponsorship, PatronDesign> OnPatronDesignResisted;
        public event Action<string, string> OnAccordDenounced;              // rival, patron (§11.10)
        public event Action<CharacterData, string> OnMemberLured;           // a practitioner bought away by a rival
        public event Action<CharacterData> OnMemberImperilled;            // a member ambushed, or its ripe Dao struck and the blow driven off
        public event Action<CharacterData, string> OnMandateEmbodied;     // a life event embodies the ability a member condenses (§5.4.3)
        public event Action<string> OnMinorAbilityLearnt;                 // « lineage:ability », a former True Monarch's minor ability (R2)
        public event Action OnTombLooted;                                  // a tomb's guardian falls (C5)
        public event Action<ArtifactInstance, string> OnArtifactFound;      // an artifact found, and where (L4f)
        public event Action<CharacterData, ArtifactInstance> OnTreasureBound; // a Foundation binds itself to a Spiritual Treasure
        public event Action<ArtifactInstance, CharacterData> OnArtifactForged; // the clan's forge makes or raises an artifact (L4f)
        public event Action<CharacterData> OnDharmaTreasure;               // a True Monarch condenses its treasure (L4e)
        public event Action<RankDesignation> OnRankDesignation;            // a treasure mortgaged on its Fruition
        public event Action<MetalEssenceDemon> OnDemonBorn;                 // a demon of the clan, awaiting its choice (L4e, 2026-10-03)
        public event Action<MetalEssenceDemon> OnDemonSubdued;
        public event Action<MetalEssenceDemon> OnWorldDemon;
        public event Action<string, string, string> OnPowerScheme;          // a power schemes against another: what, the schemer, the victim              // a demon born of a power's elder ravages its region
        public event Action OnUnderworldProvoked;                           // the clan kept an essence against the custom
        public event Action<RegionalPhenomenon> OnPhenomenon;              // a phenomenon over a region (L4c, 2026-10-03)
        public event Action OnClanKingdomFounded;                          // the clan founds its kingdom (R20, 2026-10-03)
        public event Action OnSectFounded;                                 // the clan founds its sect (B3d)
        public event Action<string> OnClanWarWon;                          // an enemy of the clan yields (B3c3: its loot)
        public event Action<ShardDefinition> OnRuinsRevealed;               // ruins found to hold a shard (B3c2)
        public event Action<ShardDefinition> OnShardRecovered;              // a shard of the mirror comes back (§11.5)
        public event Action<string, string, bool> OnFruitionFreed;        // a lineage freed: its id, the holder gone, whether reborn (2026-10-01)
        public event Action<string, string> OnFruitionTaken;              // a lineage's Realization taken in the world: its id, its new holder
        public event Action<string> OnFruitionRevealed;                   // the mirror lays a hidden lineage's truth bare
        public event Action<CharacterData, string> OnAncestorReborn;      // a True Monarch of the clan reborn: the child, the ancestor's name
        public event Action<CharacterData> OnChosenHarvested;             // a power harvests the reborn while young
        public event Action<string, string> OnPowerFell;                  // a power disperses: its name, its heir's (2026-10-01)
        public event Action<FactionData, string> OnPowerRose;             // a power is born: it, and whence (a parent power, or null)
        public event Action<FactionData> OnKingdomFounded;                // a power founds a kingdom
        public event Action<FactionData, FactionElder, bool> OnElderDied;  // a power's elder dies (true: a demon of a failed Golden Core)
        public event Action<FactionData, FactionElder> OnElderRose;        // a power's elder reaches a new realm
        public event Action<ShardDefinition, string> OnShardSensed;        // the mirror senses a shard toward a region (2026-10-01)
        public event Action<string, IReadOnlyList<string>> OnClanProbe;    // the clan sends a probe: its target, its team
        public event Action<EndingDefinition, string> OnEndingReached;      // a dynastic ending (§11.9), and who reached it (null: the clan)
        public event Action OnGameOver;                                    // a defeat (§11.9): the dynastic endings never end the game

        public void TriggerYearStarted(int year) => OnYearStarted?.Invoke(year);
        public void TriggerPhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public void TriggerCharacterBorn(CharacterData character) => OnCharacterBorn?.Invoke(character);
        public void TriggerCharacterDied(CharacterData character, DeathCause cause) => OnCharacterDied?.Invoke(character, cause);
        public void TriggerBreakthroughSuccess(CharacterData character, CultivationRealm newRealm) => OnBreakthroughSuccess?.Invoke(character, newRealm);
        public void TriggerMetalEssenceDemon(CharacterData character) => OnMetalEssenceDemon?.Invoke(character);
        public void TriggerPositionTaken(CharacterData member, GoldenCoreState position, GoldenCoreState from) => OnPositionTaken?.Invoke(member, position, from);
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
        public void TriggerClanStruck(string power) => OnClanStruck?.Invoke(power);
        public void TriggerTreatyBetrayed(string power) => OnTreatyBetrayed?.Invoke(power);
        public void TriggerBlackmail(string power) => OnBlackmail?.Invoke(power);
        public void TriggerSpyUnmasked(string power) => OnSpyUnmasked?.Invoke(power);
        public void TriggerPatronWrath(string patron) => OnPatronWrath?.Invoke(patron);
        public void TriggerWarBegun(string attacker, string defender) => OnWarBegun?.Invoke(attacker, defender);
        public void TriggerPeace(string a, string b) => OnPeace?.Invoke(a, b);
        public void TriggerChallengeSettled(string faction, ChallengeOutcome outcome) => OnChallengeSettled?.Invoke(faction, outcome);
        public void TriggerExtortion(string power) => OnExtortion?.Invoke(power);
        public void TriggerExtortionRefused(string power) => OnExtortionRefused?.Invoke(power);
        public void TriggerDaoHuntFoiled(string power) => OnDaoHuntFoiled?.Invoke(power);
        public void TriggerHunt(string beastId, bool captured) => OnHunt?.Invoke(beastId, captured);
        public void TriggerPurpleMansionAscent(CharacterData member) => OnPurpleMansionAscent?.Invoke(member);

        public void TriggerBreakthroughFailed(CharacterData character) => OnBreakthroughFailed?.Invoke(character);
        public void TriggerSpiritStonesChanged(int total) => OnSpiritStonesChanged?.Invoke(total);
        public void TriggerRandomEventOccurred(RandomEventData evt) => OnRandomEventOccurred?.Invoke(evt);
        public void TriggerStoryEventRaised(StoryEventData evt) => OnStoryEventRaised?.Invoke(evt);
        public void TriggerPatronOffer(Diplomacy.SponsorOffer offer) => OnPatronOffer?.Invoke(offer);
        public void TriggerPatronDesignDue(Diplomacy.Sponsorship s, PatronDesign d) => OnPatronDesignDue?.Invoke(s, d);
        public void TriggerPatronDemand(Diplomacy.Sponsorship s, PatronDesign d) => OnPatronDemand?.Invoke(s, d);
        public void TriggerPatronDesignResisted(Diplomacy.Sponsorship s, PatronDesign d) => OnPatronDesignResisted?.Invoke(s, d);
        public void TriggerAccordDenounced(string rival, string patron) => OnAccordDenounced?.Invoke(rival, patron);
        public void TriggerMemberLured(CharacterData member, string rival) => OnMemberLured?.Invoke(member, rival);
        public void TriggerSectFounded() => OnSectFounded?.Invoke();
        public void TriggerClanKingdomFounded() => OnClanKingdomFounded?.Invoke();
        public void TriggerPhenomenon(RegionalPhenomenon phenomenon) => OnPhenomenon?.Invoke(phenomenon);
        public void TriggerDemonBorn(MetalEssenceDemon demon) => OnDemonBorn?.Invoke(demon);
        public void TriggerDharmaTreasure(CharacterData member) => OnDharmaTreasure?.Invoke(member);
        public void TriggerTombLooted() => OnTombLooted?.Invoke();
        public void TriggerMemberImperilled(CharacterData member) => OnMemberImperilled?.Invoke(member);
        public void TriggerMandateEmbodied(CharacterData member, string ability) => OnMandateEmbodied?.Invoke(member, ability);
        public void TriggerMinorAbilityLearnt(string ability) => OnMinorAbilityLearnt?.Invoke(ability);
        public void TriggerArtifactFound(ArtifactInstance a, string where) => OnArtifactFound?.Invoke(a, where);
        public void TriggerTreasureBound(CharacterData m, ArtifactInstance a) => OnTreasureBound?.Invoke(m, a);
        public void TriggerArtifactForged(ArtifactInstance artifact, CharacterData smith) => OnArtifactForged?.Invoke(artifact, smith);
        public void TriggerRankDesignation(RankDesignation d) => OnRankDesignation?.Invoke(d);
        public void TriggerDemonSubdued(MetalEssenceDemon demon) => OnDemonSubdued?.Invoke(demon);
        public void TriggerWorldDemon(MetalEssenceDemon demon) => OnWorldDemon?.Invoke(demon);
        public void TriggerPowerScheme(string what, string schemer, string victim) => OnPowerScheme?.Invoke(what, schemer, victim);
        public void TriggerUnderworldProvoked() => OnUnderworldProvoked?.Invoke();
        public void TriggerClanWarWon(string enemy) => OnClanWarWon?.Invoke(enemy);
        public void TriggerRuinsRevealed(ShardDefinition shard) => OnRuinsRevealed?.Invoke(shard);
        public void TriggerShardRecovered(ShardDefinition shard) => OnShardRecovered?.Invoke(shard);
        public void TriggerFruitionFreed(string fruitionId, string holder, bool reborn) => OnFruitionFreed?.Invoke(fruitionId, holder, reborn);
        public void TriggerFruitionRevealed(string fruitionId) => OnFruitionRevealed?.Invoke(fruitionId);
        public void TriggerAncestorReborn(CharacterData child, string ancestor) => OnAncestorReborn?.Invoke(child, ancestor);
        public void TriggerChosenHarvested(CharacterData chosen) => OnChosenHarvested?.Invoke(chosen);
        public void TriggerFruitionTaken(string fruitionId, string holder) => OnFruitionTaken?.Invoke(fruitionId, holder);
        public void TriggerPowerFell(string fallen, string heir) => OnPowerFell?.Invoke(fallen, heir);
        public void TriggerPowerRose(FactionData power, string parent) => OnPowerRose?.Invoke(power, parent);
        public void TriggerKingdomFounded(FactionData power) => OnKingdomFounded?.Invoke(power);
        public void TriggerElderDied(FactionData power, FactionElder elder, bool demon) => OnElderDied?.Invoke(power, elder, demon);
        public void TriggerElderRose(FactionData power, FactionElder elder) => OnElderRose?.Invoke(power, elder);
        public void TriggerShardSensed(ShardDefinition shard, string regionId) => OnShardSensed?.Invoke(shard, regionId);
        public void TriggerClanProbe(string target, IReadOnlyList<string> teamIds) => OnClanProbe?.Invoke(target, teamIds);
        public void TriggerEndingReached(EndingDefinition ending, string subject) => OnEndingReached?.Invoke(ending, subject);
        public void TriggerGameOver() => OnGameOver?.Invoke();
    }
}
