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
    /// The powers' intrigues (LORE.md D7; user decision 2026-09-27). Blackmail: a power holding enough proof may demand
    /// stones instead of striking; paid, it keeps quiet for a while; refused, or left unanswered to the next year, it
    /// spreads its proof among the others. Theft, at most once a year: a beast the clan captured (worst before the
    /// ritual), a copy of a manual, stones or a portion of Qi; patrols make it rarer and may catch the thief — an agent
    /// held, proof of the theft — otherwise the clan does not know who. Infiltration: a spy sent as a spouse feeds its
    /// power proof and clues each year; the mirror sounds a spouse at its price; an unmasked spy is turned into a double
    /// agent, who wears its power's proof away, or executed. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class IntrigueSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly TechniqueLibrary techniques;
        private readonly MirrorSystem mirror;
        private readonly CaptiveSystem captives;
        private readonly TreatySystem treaties;
        private readonly List<Demand> demands = new List<Demand>();
        private readonly Dictionary<string, int> quietUntil = new Dictionary<string, int>();

        public IntrigueSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            TechniqueLibrary techniques, MirrorSystem mirror, CaptiveSystem captives, TreatySystem treaties)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.techniques = techniques;
            this.mirror = mirror;
            this.captives = captives;
            this.treaties = treaties;
        }

        private IntrigueSettings Settings => ctx.Content.Balance.Intrigues;

        public IReadOnlyList<Demand> Demands => demands;

        /// <summary>The year until which a paid blackmailer keeps quiet.</summary>
        public IReadOnlyDictionary<string, int> QuietUntil => quietUntil;

        public void RestoreDemands(IEnumerable<Demand> saved, IReadOnlyDictionary<string, int> quiet)
        {
            demands.Clear();
            if (saved != null) demands.AddRange(saved.Where(d => d != null));
            quietUntil.Clear();
            foreach (var pair in quiet ?? new Dictionary<string, int>()) quietUntil[pair.Key] = pair.Value;
        }

        private int Patrols => clan.LivingMembers.Count(m => m.CurrentTask == TaskType.Patrol && m.CaptorFaction == null);

        public void ProcessYear()
        {
            foreach (var late in demands.Where(d => d.Year < ctx.Clock.Year).ToList()) Refuse(late.Faction); // unanswered: refused
            StealOnce();
            Blackmail();
            Spies();
        }

        // ---- Blackmail ----

        private void Blackmail()
        {
            var s = Settings;
            foreach (var power in factions.Factions.ToList())
            {
                if (suspicion.Evidence(power.Name) < s.BlackmailEvidence || demands.Any(d => d.Faction == power.Name)) continue;
                if (quietUntil.TryGetValue(power.Name, out int until) && until > ctx.Clock.Year) continue; // it was paid
                if (treaties.Spares(power.Name) || !ctx.Rng.Chance(IntrigueRules.BlackmailChance(power, s))) continue;
                demands.Add(new Demand(power.Name, (int)(resources.SpiritStones * s.BlackmailStonesShare), ctx.Clock.Year));
                ctx.Log.Warning($"[Intrigues] {power.Name} demands stones for its silence.");
                return; // one demand a year
            }
        }

        public string Pay(string faction)
        {
            var demand = demands.FirstOrDefault(d => d.Faction == faction);
            if (demand == null) return "aucune exigence de cette puissance";
            if (!resources.ConsumeSpiritStones(demand.Stones)) return $"il faut {demand.Stones} pierres spirituelles";
            var power = factions.GetFactionByName(faction);
            if (power != null) power.Wealth += demand.Stones;
            demands.Remove(demand);
            quietUntil[faction] = ctx.Clock.Year + Settings.QuietYears;
            ctx.Log.Info($"[Intrigues] The clan buys {faction}'s silence.");
            return null;
        }

        public string Refuse(string faction)
        {
            var demand = demands.FirstOrDefault(d => d.Faction == faction);
            if (demand == null) return "aucune exigence de cette puissance";
            demands.Remove(demand);
            var s = Settings;
            foreach (var other in factions.Factions.Where(f => f.Name != faction))
            {
                suspicion.AddEvidence(other.Name, s.RefusedSpreadEvidence); // it spreads what it holds
                suspicion.AddToClan(other.Name, s.RefusedSpreadEvidence);
            }
            var power = factions.GetFactionByName(faction);
            if (power != null) factions.ChangeRelation(power.ID, s.RefusedRelation);
            quietUntil[faction] = ctx.Clock.Year + s.QuietYears; // it has played its card
            ctx.Log.Warning($"[Intrigues] Refused, {faction} spreads its proof against the clan.");
            return null;
        }

        // ---- Theft ----

        private void StealOnce()
        {
            int patrols = Patrols;
            foreach (var thief in factions.Factions.OrderBy(_ => ctx.Rng.Next()).ToList())
            {
                if (treaties.Spares(thief.Name) || !ctx.Rng.Chance(IntrigueRules.TheftChance(thief, patrols, Settings))) continue;
                var targets = Available().ToList();
                if (targets.Count == 0) return;
                Steal(thief, ctx.Rng.Pick(targets));
                return; // one theft a year
            }
        }

        private IEnumerable<IntrigueTarget> Available()
        {
            if (resources.Beasts.Count > 0) yield return IntrigueTarget.Beast;
            if (techniques.Known.Any()) yield return IntrigueTarget.Manual;
            if (resources.SpiritStones > 0) yield return IntrigueTarget.Stones;
            if (resources.SpiritualQi.Any(q => q.Value > 0)) yield return IntrigueTarget.Qi;
        }

        /// <summary>A theft; patrols may catch the thief. Returns what was taken, or null when there was nothing to take.</summary>
        public string Steal(FactionData thief, IntrigueTarget target)
        {
            string taken = Take(thief, target);
            if (taken == null) return null;
            bool caught = ctx.Rng.Chance(IntrigueRules.CatchChance(Patrols, Settings));
            if (caught) captives.Imprison(new Prisoner($"thief-{thief.ID}-{ctx.Clock.Year}", thief.Name, thief.HighestRealm, ctx.Clock.Year));
            ctx.Log.Warning($"[Intrigues] Stolen from the clan: {taken}{(caught ? $" — the thief of {thief.Name} is caught" : "")}.");
            ctx.Events.TriggerTheft(taken, caught ? thief.Name : null);
            return taken;
        }

        private string Take(FactionData thief, IntrigueTarget target)
        {
            switch (target)
            {
                case IntrigueTarget.Beast:
                    var beast = resources.Beasts.OrderByDescending(b => (int)b.Realm).ThenByDescending(b => b.Stage).FirstOrDefault();
                    return beast != null && resources.ConsumeBeast(beast) ? "une bête capturée" : null;
                case IntrigueTarget.Manual:
                    var manual = techniques.Known.Where(t => !thief.Techniques.Contains(t.ID)).OrderByDescending(t => t.Grade)
                        .ThenBy(t => t.ID, StringComparer.Ordinal).FirstOrDefault();
                    if (manual == null) return null;
                    thief.Techniques.Add(manual.ID); // a copy: the clan keeps its own
                    return $"une copie de « {manual.Name} »";
                case IntrigueTarget.Stones:
                    int stones = (int)(resources.SpiritStones * Settings.TheftStonesShare);
                    if (stones <= 0 || !resources.ConsumeSpiritStones(stones)) return null;
                    thief.Wealth += stones;
                    return $"{stones} pierres spirituelles";
                case IntrigueTarget.Qi:
                    var qi = resources.SpiritualQi.Where(q => q.Value > 0).OrderBy(q => q.Key, StringComparer.Ordinal).Select(q => q.Key).FirstOrDefault();
                    return qi != null && resources.ConsumeQi(qi, 1) ? "une portion de Qi" : null;
                default:
                    return null;
            }
        }

        // ---- Spies ----

        private void Spies()
        {
            var s = Settings;
            foreach (var spy in clan.LivingMembers.Where(m => m.SpyFor != null && m.CaptorFaction == null))
            {
                if (factions.GetFactionByName(spy.SpyFor) == null) continue;
                if (spy.DoubleAgent)
                {
                    suspicion.AddEvidence(spy.SpyFor, -s.DoubleAgentRelief); // false reports wear its proof away
                    suspicion.AddMirrorClues(spy.SpyFor, -s.DoubleAgentRelief);
                    continue;
                }
                suspicion.AddEvidence(spy.SpyFor, s.SpyEvidence);
                suspicion.AddMirrorClues(spy.SpyFor, s.SpyClues);
            }
        }

        /// <summary>The mirror sounds a member: whom they spy for, or no one. Costs the mirror either way; null when it cannot.</summary>
        public string Unmask(string memberId)
        {
            var member = clan.FindById(memberId);
            if (member == null || !member.IsAlive) return null;
            if (!mirror.ConsumePower(Settings.UnmaskMirrorCost)) return null;
            if (member.SpyFor == null) return $"{member.FullName} n'espionne pour personne.";
            member.SpyUnmasked = true;
            return $"{member.FullName} espionne pour {member.SpyFor}.";
        }

        public string Turn(string memberId)
        {
            var spy = Unmasked(memberId);
            if (spy == null) return "ce membre n'est pas un espion démasqué";
            spy.DoubleAgent = true;
            ctx.Log.Info($"[Intrigues] {spy.FullName} now feeds {spy.SpyFor} false reports.");
            return null;
        }

        public string ExecuteSpy(string memberId)
        {
            var spy = Unmasked(memberId);
            if (spy == null) return "ce membre n'est pas un espion démasqué";
            var power = factions.GetFactionByName(spy.SpyFor);
            if (power != null) factions.ChangeRelation(power.ID, Settings.SpyExecutionRelation);
            clan.Kill(spy, DeathCause.ExecutedAsSpy);
            return null;
        }

        private CharacterData Unmasked(string memberId)
        {
            var member = clan.FindById(memberId);
            return member != null && member.IsAlive && member.SpyFor != null && member.SpyUnmasked ? member : null;
        }
    }
}
