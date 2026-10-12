using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// An ambush fought on the grid (G6; the user's choices of 2026-10-11): when a power's ambush is not foiled, the members
    /// away from the domain with the one it struck — the free cultivators among them — face its agents, a realm or so below
    /// the power's strongest and never below the member struck. Beaten off, one agent is taken; beaten, the member struck is
    /// taken for ransom; fled, they escape with their wounds; nobody dies, the agents want a hostage. The clan may also let
    /// chance settle it, as before the battles. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class AmbushSystem
    {
        private const int AgentMinRoot = 30;
        private const int AgentMaxRoot = 80;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SchemeSystem schemes;
        private readonly CaptiveSystem captives;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;
        private Ambush fought; // the ambush whose battle is under way

        public AmbushSystem(GameContext ctx, ClanManager clan, FactionManager factions, SchemeSystem schemes, CaptiveSystem captives,
            TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.schemes = schemes;
            this.captives = captives;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        private SchemeSettings Settings => ctx.Content.Balance.Schemes;

        public Ambush Pending { get; private set; }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        /// <summary>The clan's alchemy (set by the session): its fighters draw on its Autumn Convergence Pills, as in a challenge.</summary>
        public AlchemySystem Alchemy { get; set; }

        public void Restore(Ambush pending) => Pending = pending;

        /// <summary>
        /// The ambush waits for the clan; false when the member struck cannot fight, or stands out of the power's reach (chance
        /// settles it then, as before).
        /// </summary>
        public bool Spring(FactionData power, CharacterData target)
        {
            if (power == null || FighterRules.Refusal(target) != null || Pending != null || Current != null) return false;
            if (SchemeRules.CaptureChance(power, target, Settings, ctx.Content.Balance.RealmGap) <= 0) return false; // beyond its agents
            var realm = (CultivationRealm)Math.Max((int)target.Realm, (int)power.HighestRealm - Settings.AgentRealmsBelowPower);
            int stage = realm == target.Realm ? target.RealmStage : 1;
            Pending = new Ambush(power.Name, ctx.Clock.Year, target.ID, realm, stage, ctx.Rng.Next(1, Settings.AmbushMaxAgents + 1), ctx.Rng.Next());
            ctx.Log.Warning($"[Ambush] {power.Name}'s agents waylay {target.FullName}: {Pending.Agents} agent(s).");
            return true;
        }

        /// <summary>The member struck first, then the others away who can fight, at most the settings' number.</summary>
        public IReadOnlyList<CharacterData> Fighters(Ambush ambush)
        {
            if (ambush == null) return Array.Empty<CharacterData>();
            var target = clan.FindById(ambush.TargetId);
            var companions = clan.LivingMembers.Where(m => m != target && schemes.IsAway(m) && FighterRules.Refusal(m) == null);
            var first = FighterRules.Refusal(target) == null ? new[] { target } : Array.Empty<CharacterData>();
            return first.Concat(companions).Take(Settings.AmbushMaxFighters).ToList();
        }

        /// <summary>The agents of an ambush, the same each time (drawn from its seed).</summary>
        public IReadOnlyList<CharacterData> Agents(Ambush ambush)
        {
            var rng = new Random(ambush.Seed);
            var names = ctx.Content.Names;
            return Enumerable.Range(0, ambush.Agents).Select(i =>
            {
                bool isMale = rng.Next(2) == 0;
                var pool = isMale ? names.Male : names.Female;
                return new CharacterData
                {
                    ID = $"agent-{ambush.Seed}-{i}",
                    FirstName = pool.Count == 0 ? "Agent" : pool[rng.Next(pool.Count)],
                    LastName = names.OutsiderFamilies.Count == 0 ? ambush.Faction : names.OutsiderFamilies[rng.Next(names.OutsiderFamilies.Count)],
                    IsMale = isMale,
                    Age = 20 + rng.Next(40),
                    Realm = ambush.Realm,
                    RealmStage = ambush.Stage,
                    SpiritualRoot = rng.Next(AgentMinRoot, AgentMaxRoot + 1),
                    HasSpiritualOrifice = true,
                    OrificeKnown = true,
                    FromFaction = ambush.Faction
                };
            }).ToList();
        }

        /// <summary>The members away fight: the battle opens on the grid.</summary>
        public string Fight()
        {
            if (Pending == null) return "aucune embuscade en attente";
            var fighters = Fighters(Pending);
            if (fighters.Count == 0) return "personne ne peut combattre : laissez faire le sort";

            fought = Pending; // Equip reads it while the battle is set up
            Current = Battle.Start(fighters, Agents(fought), ctx.Rng, ctx.Log, findTechnique: techniques.Find, gap: ctx.Content.Balance.RealmGap,
                convergence: ctx.Content.Balance.Arts.Convergence, equip: Equip);
            Pending = null; // only once the battle stands
            return null;
        }

        /// <summary>The clan lets chance settle the ambush, as before the battles.</summary>
        public string LetBe()
        {
            if (Pending == null) return "aucune embuscade en attente";
            var power = factions.GetFactionByName(Pending.Faction);
            var target = clan.FindById(Pending.TargetId);
            Pending = null;
            if (power != null && target is { IsAlive: true, CaptorFaction: null }) schemes.ResolveByChance(power, target);
            return null;
        }

        /// <summary>Once the battle is over: an agent taken, the member taken, or a flight; the wounds, and the chronicle.</summary>
        public string Conclude()
        {
            if (Current == null) return "aucune bataille en cours";
            if (!Current.IsOver) return "la bataille n'est pas finie";
            Current.ResolveAftermath(clan, wounds, fallenDies: () => false); // the agents want a hostage, not a corpse
            var outcome = Current.State switch
            {
                CombatState.Victory => AmbushOutcome.Repelled,
                CombatState.Defeat => AmbushOutcome.Taken,
                _ => AmbushOutcome.Escaped
            };
            var target = clan.FindById(fought.TargetId);
            if (outcome == AmbushOutcome.Taken && (target is not { IsAlive: true, CaptorFaction: null }
                || Current.Allies.Any(a => a.BaseData == target && a.HasFled)))
                outcome = AmbushOutcome.Escaped; // nobody left to take, or the member struck got away
            if (outcome == AmbushOutcome.Repelled)
                captives.Imprison(new Prisoner($"agent-{fought.Seed}", fought.Faction, fought.Realm, ctx.Clock.Year));
            else if (outcome == AmbushOutcome.Taken) captives.Take(target, fought.Faction);
            else ctx.Log.Info($"[Ambush] The clan's members escape {fought.Faction}'s agents.");
            ctx.Events.TriggerAmbushSettled(fought.Faction, outcome);
            Current = null;
            fought = null;
            return null;
        }

        /// <summary>What each fighter carries: the clan's draw on its store; an agent of a power that knows alchemy, one pill.</summary>
        private void Equip(CombatUnit unit)
        {
            if (unit.IsAlly)
            {
                if (Alchemy == null) return;
                unit.TakeConvergencePill = () => Alchemy.TakePill(PillKind.AutumnConvergence);
                unit.ConvergencePillsLeft = () => Alchemy.PillsOf(PillKind.AutumnConvergence);
                return;
            }
            if (!PowerArts.Knows(factions.GetFactionByName(fought?.Faction), ImmortalArt.Alchemy, ctx.Content)) return;
            int left = 1;
            unit.TakeConvergencePill = () => left-- > 0;
            unit.ConvergencePillsLeft = () => left;
        }
    }
}
