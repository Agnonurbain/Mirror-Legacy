using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// Probes (« sondages », user decision 2026-09-27; D7): planned operations to pierce another's secrets — provocation,
    /// infiltration, bribery, theft of records, or (the clan only) the mirror's sight — alone or joined by other powers,
    /// once or again. The odds weigh the prober's strength (and its partners') against the target's guard, the secret's
    /// rank, the target's vigilance (it grows with each probe, fades with the years) and its distrust of the prober, an
    /// insider, being neighbours, a bribe and the target's temper. The target calls its allies: one that comes in time
    /// guards it; one that lingers — its own profit — lets the secret out and learns it too. A partner may talk. A probe
    /// may be seen — the target then knows who — or end in disaster: an agent taken. The powers probe each other and the
    /// clan; a probe of the clan also feeds a power's clues about the hidden treasure behind it.
    /// </summary>
    public sealed class ProbeSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly TreatySystem treaties;
        private readonly PowerPoliticsSystem politics;
        private readonly MirrorSystem mirror;
        private readonly MirrorLore lore;
        private readonly CaptiveSystem captives;
        private readonly SecretBook book;
        private readonly HuntOperations hunts;
        private readonly ResourceManager resources;
        private readonly Dictionary<string, int> alertness = new Dictionary<string, int>();

        public ProbeSystem(GameContext ctx, ClanManager clan, FactionManager factions, SuspicionLedger suspicion, TreatySystem treaties,
            PowerPoliticsSystem politics, MirrorSystem mirror, MirrorLore lore, CaptiveSystem captives, SecretBook book, HuntOperations hunts,
            ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.suspicion = suspicion;
            this.treaties = treaties;
            this.politics = politics;
            this.mirror = mirror;
            this.lore = lore;
            this.captives = captives;
            this.book = book;
            this.hunts = hunts;
            this.resources = resources;
        }

        private SecretSettings Settings => ctx.Content.Balance.Secrets;
        private static string Clan => SecretBook.ClanHolder;

        public int Alertness(string target) => alertness.TryGetValue(target, out int v) ? v : 0;
        public IReadOnlyDictionary<string, int> AllAlertness => alertness;

        public void RestoreAlertness(IReadOnlyDictionary<string, int> saved)
        {
            alertness.Clear();
            foreach (var pair in saved ?? new Dictionary<string, int>()) alertness[pair.Key] = pair.Value;
        }

        // ---- The clan probes ----

        public ProbeOutcome Probe(ProbePlan plan)
        {
            string refusal = Refusal(plan, out var team);
            if (refusal != null) return ProbeOutcome.Refused(refusal);
            foreach (var member in team) member.LastOperationYear = ctx.Clock.Year;
            var target = factions.GetFactionByName(plan.Target);
            if (plan.Approach == ProbeApproach.Bribery && resources.ConsumeSpiritStones(plan.Stones)) target.Wealth += plan.Stones;
            if (plan.Approach == ProbeApproach.MirrorSight) mirror.ConsumePower(MirrorSightCost(plan.Target));
            return Resolve(Clan, TeamStrength(team), plan.Target, plan.Approach, Partners(plan), plan.Stones, team);
        }

        /// <summary>The clan's odds against a target, before any roll (the allies either all prompt or all lingering).</summary>
        public double ChanceAgainst(ProbePlan plan, bool alliesPrompt = false)
        {
            var team = (plan.TeamIds ?? new List<string>()).Select(clan.FindById).Where(m => m != null).ToList();
            var allies = alliesPrompt ? AlliesOf(plan.Target).Where(a => a != Clan).ToList() : new List<string>();
            return SuccessProbability(Factors(Clan, TeamStrength(team), plan.Target, plan.Approach, Partners(plan), plan.Stones, allies), plan.Target);
        }

        /// <summary>The partners, each once.</summary>
        private static List<string> Partners(ProbePlan plan) => (plan.Partners ?? new List<string>()).Distinct().ToList();

        private string Refusal(ProbePlan plan, out List<CharacterData> team)
        {
            team = (plan.TeamIds ?? new List<string>()).Distinct().Select(clan.FindById).ToList();
            var target = factions.GetFactionByName(plan.Target);
            if (target == null) return "puissance inconnue";
            if (book.NextUnknown(Clan, plan.Target) == null) return "il n'y a rien à apprendre là, du moins que le clan sache chercher";
            if (team.Count == 0) return "il faut une équipe";
            var unfit = team.FirstOrDefault(m => !hunts.IsFree(m));
            if (unfit != null || team.Contains(null)) return $"{unfit?.FullName ?? "un membre"} ne peut pas partir";
            foreach (var partner in Partners(plan))
            {
                var power = factions.GetFactionByName(partner);
                if (power == null || partner == plan.Target) return $"{partner} ne peut se joindre à ce sondage";
                if (power.RelationWithPlayer < Settings.PartnerMinRelation) return $"{partner} ne s'y joindrait pas (relation trop froide)";
            }
            if (plan.Approach == ProbeApproach.Bribery && (plan.Stones <= 0 || resources.SpiritStones < plan.Stones))
                return "la corruption demande des pierres";
            if (plan.Approach == ProbeApproach.MirrorSight && mirror.MirrorPower < MirrorSightCost(plan.Target))
                return $"il faut {MirrorSightCost(plan.Target)} de puissance du miroir";
            return null;
        }

        private int MirrorSightCost(string target) => Settings.MirrorSightCostPerRank * (book.NextUnknown(Clan, target)?.Rank ?? 1);

        // ---- The powers probe ----

        public ProbeOutcome PowerProbe(FactionData prober, string target, ProbeApproach approach, List<string> partners) =>
            prober == null || target == prober.Name || approach == ProbeApproach.MirrorSight || book.NextUnknown(prober.Name, target) == null
                ? ProbeOutcome.Refused("sondage impossible")
                : Resolve(prober.Name, Strength(prober), target, approach, (partners ?? new List<string>()).Distinct().ToList(), 0, null);

        public void ProcessYear()
        {
            foreach (var key in alertness.Keys.ToList()) alertness[key] = Math.Max(0, alertness[key] - Settings.AlertnessDecay);
            int probes = 0;
            foreach (var power in factions.Factions.OrderBy(_ => ctx.Rng.Next()).ToList())
            {
                if (probes >= Settings.MaxAiProbesPerYear) return;
                string target = Motive(power);
                if (target == null || !ctx.Rng.Chance(Settings.AiProbeChance)) continue;
                var partners = politics.AlliesOf(power.Name).Where(a => a != target && ctx.Rng.Chance(Settings.AiPartnerChance)).ToList();
                PowerProbe(power, target, ApproachOf(power), partners);
                probes++;
            }
        }

        /// <summary>Whom a power would probe: the clan it suspects, else the power it distrusts most.</summary>
        private string Motive(FactionData power)
        {
            if (suspicion.OfClan(power.Name) >= Settings.AiProbeSuspicion && book.NextUnknown(power.Name, Clan) != null) return Clan;
            var distrusted = factions.Factions.Where(f => f != power && book.NextUnknown(power.Name, f.Name) != null)
                .Select(f => (f.Name, d: suspicion.Distrust(power.Name, f.Name)))
                .Where(p => p.d >= Settings.AiProbeDistrust)
                .OrderByDescending(p => p.d).ThenBy(p => p.Name, StringComparer.Ordinal).FirstOrDefault();
            return distrusted.Name;
        }

        private static ProbeApproach ApproachOf(FactionData power) => power.Personality switch
        {
            FactionPersonality.Aggressive => ProbeApproach.Provocation,
            FactionPersonality.Merchant => ProbeApproach.Bribery,
            FactionPersonality.Manipulative => ProbeApproach.Infiltration,
            _ => ProbeApproach.RecordTheft
        };

        // ---- The probe itself ----

        private ProbeOutcome Resolve(string prober, int strength, string target, ProbeApproach approach, List<string> partners, int stones,
            List<CharacterData> team)
        {
            var s = Settings;
            var (prompt, late) = CallAllies(prober, target, partners);
            bool leaked = partners.Aggregate(false, (seen, partner) =>
                ctx.Rng.Chance(Math.Clamp(s.PartnerLeakChance * (1 + suspicion.Distrust(partner, prober) / 100.0), 0, 1)) || seen);
            bool success = ctx.Rng.Chance(SuccessProbability(Factors(prober, strength, target, approach, partners, stones, prompt), target, approach));
            bool detected = ctx.Rng.Chance(ProbeRules.DetectChance(approach, Alertness(target), s)) || leaked;
            bool disaster = !success && detected && ctx.Rng.Chance(s.DisasterChance.TryGetValue(approach, out var d) ? d : 0);

            var revealed = success ? Spoils(prober, target, approach, partners, late) : new List<string>();
            alertness[target] = Math.Min(SuspicionLedger.Max, Alertness(target) + s.AlertnessPerProbe + (detected ? s.AlertnessDetectedExtra : 0));
            if (detected) Spotted(prober, target);
            if (disaster) Disaster(prober, target, team);
            ctx.Log.Info($"[Probes] {prober} probes {target}: {(success ? "success" : "failure")}{(detected ? ", seen" : "")}{(disaster ? ", disaster" : "")}.");
            return new ProbeOutcome(null, success, detected, disaster, revealed, late);
        }

        /// <summary>The target's allies: each comes in time, or lingers for its own profit.</summary>
        private (List<string> Prompt, List<string> Late) CallAllies(string prober, string target, List<string> partners)
        {
            var prompt = new List<string>();
            var late = new List<string>();
            foreach (var ally in AlliesOf(target).Where(a => a != prober && !partners.Contains(a)))
            {
                var power = factions.GetFactionByName(ally);
                if (power == null) continue;
                int distrust = target == Clan ? suspicion.OfClan(ally) : suspicion.Distrust(ally, target);
                (ctx.Rng.Chance(ProbeRules.AllyPromptChance(power, distrust, Settings)) ? prompt : late).Add(ally);
            }
            return (prompt, late);
        }

        private IEnumerable<string> AlliesOf(string target)
        {
            if (target != Clan) return politics.AlliesOf(target);
            return treaties.All.Where(t => t.Kind == TreatyKind.Defence || (t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain))
                .Select(t => t.Faction).Distinct();
        }

        private ProbeFactors Factors(string prober, int strength, string target, ProbeApproach approach, List<string> partners, int stones,
            List<string> promptAllies)
        {
            var s = Settings;
            int partnerStrength = partners.Select(factions.GetFactionByName).Where(p => p != null).Sum(Strength);
            int allyGuard = promptAllies.Select(factions.GetFactionByName).Where(p => p != null).Sum(Strength);
            var targetPower = factions.GetFactionByName(target);
            return new ProbeFactors
            {
                Approach = approach,
                ProberStrength = strength + (int)(partnerStrength * s.PartnerWeight),
                TargetGuard = Guard(target) + (int)(allyGuard * s.AllyGuardWeight),
                Rank = book.NextUnknown(prober, target)?.Rank ?? 1,
                Alertness = Alertness(target),
                Insider = Insider(prober, target),
                Neighbours = Neighbours(prober, target),
                TargetDistrust = target == Clan ? 0 : prober == Clan ? suspicion.OfClan(target) : suspicion.Distrust(target, prober),
                Stones = stones,
                TargetTemper = targetPower?.Personality ?? FactionPersonality.Isolationist
            };
        }

        /// <summary>A probe's odds of success (a probability; the roll is the caller's).</summary>
        private double SuccessProbability(ProbeFactors f, string target, ProbeApproach? approach = null)
        {
            if ((approach ?? f.Approach) != ProbeApproach.MirrorSight) return ProbeRules.SuccessChance(f, Settings);
            double sight = (Settings.BaseChance.TryGetValue(ProbeApproach.MirrorSight, out var b) ? b : 0) - (lore.Knows(target) ? Settings.KnowerSightPenalty : 0);
            return Math.Clamp(sight, Settings.MinPercent, Settings.MaxPercent) / 100.0; // the mirror sees; an elder who knows it veils himself
        }

        private List<string> Spoils(string prober, string target, ProbeApproach approach, List<string> partners, List<string> late)
        {
            var s = Settings;
            int gain = s.Gain.TryGetValue(approach, out var g) ? g : 0;
            var revealed = book.AddClues(prober, target, gain).Select(x => x.KindId).ToList();
            foreach (var partner in partners) book.AddClues(partner, target, gain);                           // the spoils are shared
            foreach (var ally in late) book.AddClues(ally, target, (int)(gain * s.LateWitnessShare));         // it watched the secret come out
            if (target == Clan && prober != Clan)
                foreach (var watcher in partners.Prepend(prober)) suspicion.AddMirrorClues(watcher, (int)(gain * s.MirrorShare)); // and wonders what else it hides
            return revealed;
        }

        private void Spotted(string prober, string target)
        {
            var s = Settings;
            if (prober == Clan)
            {
                suspicion.AddToClan(target, s.DetectedDistrust);
                var power = factions.GetFactionByName(target);
                if (power != null) factions.ChangeRelation(power.ID, s.DetectedRelation);
            }
            else if (target == Clan) ctx.Events.TriggerProbeSpotted(prober);
            else suspicion.AddDistrust(target, prober, s.DetectedDistrust);
        }

        private void Disaster(string prober, string target, List<CharacterData> team)
        {
            if (prober == Clan)
            {
                var taken = team?.FirstOrDefault(m => m.IsAlive && m.CaptorFaction == null);
                if (taken != null) captives.Take(taken, target); // an agent taken: the clan's own
            }
            else if (target == Clan)
            {
                var power = factions.GetFactionByName(prober);
                if (power != null) captives.Imprison(new Prisoner($"prober-{power.ID}-{ctx.Clock.Year}", power.Name, power.HighestRealm, ctx.Clock.Year));
            }
            else suspicion.AddDistrust(target, prober, Settings.DetectedDistrust);
        }

        // ---- Strength and guard ----

        private static int Strength(FactionData power) => HuntRules.Power(power.HighestRealm, 5);

        private static int TeamStrength(IEnumerable<CharacterData> team) => team.Where(m => m != null).Sum(m => HuntRules.Power(m.Realm, m.RealmStage));

        private int Guard(string target)
        {
            if (target != Clan) return factions.GetFactionByName(target) is { } power ? Strength(power) : 0;
            var free = clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            int strongest = free.Select(m => HuntRules.Power(m.Realm, m.RealmStage)).DefaultIfEmpty(0).Max();
            return strongest + free.Count(m => m.CurrentTask == TaskType.Patrol) * Settings.PatrolGuardPoints;
        }

        /// <summary>An insider: a spy of the prober inside the clan, a spy the clan turned inside the target, or a treaty with it.</summary>
        private bool Insider(string prober, string target)
        {
            if (target == Clan) return clan.LivingMembers.Any(m => m.SpyFor == prober && !m.DoubleAgent) || treaties.With(prober).Count > 0;
            if (prober == Clan) return clan.LivingMembers.Any(m => m.SpyFor == target && m.DoubleAgent) || treaties.With(target).Count > 0;
            return politics.AlliesOf(prober).Contains(target);
        }

        private bool Neighbours(string prober, string target)
        {
            var a = factions.GetFactionByName(prober);
            var b = factions.GetFactionByName(target);
            if (prober == Clan) return factions.IsNeighbour(b);
            if (target == Clan) return factions.IsNeighbour(a);
            return factions.AreNeighbours(a, b);
        }
    }
}
