using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Oaths of the Dao (L4d, user request of 2026-09-26): two cultivators of any path swear clauses on their
    /// path. The oath-breaker's path is interrupted (progression sealed, perhaps a Dao wound) or slowed by a
    /// Heart Demon (speed, stability) — by the clause's severity. Nothing is perfect: loopholes (a third party
    /// acts, a false name, expiry, the mirror's veil, purification) come from oaths.json. Harm is reported
    /// through the event bus (OnHarm), so any system that harms someone is checked.
    /// </summary>
    public sealed class OathSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MirrorSystem mirror;
        private readonly KnowledgeBase knowledge;
        private readonly List<PactData> pacts = new List<PactData>();
        private readonly HashSet<string> veiled = new HashSet<string>();

        public OathSystem(GameContext ctx, ClanManager clan, ResourceManager resources, MirrorSystem mirror, KnowledgeBase knowledge)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.mirror = mirror;
            this.knowledge = knowledge;
            ctx.Events.OnHarm += (actor, victim) => Transgress(actor, victim, OathAct.Harm);
            ctx.Events.OnYearStarted += PassYear;
        }

        private OathSettings Settings => ctx.Content.Balance.Oaths;

        public IReadOnlyList<PactData> Pacts => pacts;

        public IEnumerable<PactData> PactsOf(CharacterData member) =>
            pacts.Where(p => p.PartyA == member.ID || p.PartyB == member.ID);

        public bool Allows(LoopholeKind kind) => ctx.Content.Oaths.Loopholes.Any(l => l.Kind == kind);

        /// <summary>Two cultivators swear clauses on their path; for life, or for some years (a loophole when allowed).</summary>
        public PactData Swear(CharacterData a, CharacterData b, IReadOnlyList<string> clauses, int? years = null,
            bool falseNameA = false, bool falseNameB = false)
        {
            var known = clauses.Where(id => ctx.Content.Oaths.Clauses.Any(c => c.Id == id)).ToList();
            if (a == null || b == null || a == b || known.Count == 0) return null;

            var pact = new PactData
            {
                Id = ctx.Rng.NextId(),
                PartyA = a.ID,
                PartyB = b.ID,
                Clauses = known,
                SwornYear = ctx.Clock.Year,
                ExpiresYear = years.HasValue ? ctx.Clock.Year + years.Value : (int?)null,
                FalseNameA = falseNameA && Allows(LoopholeKind.FalseName),
                FalseNameB = falseNameB && Allows(LoopholeKind.FalseName)
            };
            pacts.Add(pact);
            knowledge.Reveal(FactKind.Pact, pact.Id, KnowledgeSource.Learned);
            ctx.Log.Info($"[Oaths] {a.FullName} and {b.FullName} swear on their path ({string.Join(", ", known)}).");
            return pact;
        }

        /// <summary>
        /// The actor does what an oath towards the victim forbids: true when a sworn clause is broken. A third
        /// party acting keeps to the letter; a false name binds nothing; the mirror's veil hides the breach.
        /// </summary>
        public bool Transgress(CharacterData actor, CharacterData victim, OathAct act, bool throughThirdParty = false)
        {
            if (actor == null || victim == null) return false;
            var broken = pacts.Where(p => Binds(p, actor, victim)).SelectMany(p => p.Clauses).Select(Clause)
                .Where(c => c != null && c.Act == act).ToList();
            if (broken.Count == 0) return false;

            if (throughThirdParty && Allows(LoopholeKind.ThirdParty))
            {
                ctx.Log.Info($"[Oaths] {actor.FullName} keeps to the letter of the oath: someone else acts.");
                return false;
            }

            Punish(actor, broken.Max(c => c.Severity));
            return true;
        }

        /// <summary>A party renders the promised service.</summary>
        public void Fulfil(PactData pact, CharacterData party)
        {
            if (pact == null || party == null) return;
            if (pact.PartyA == party.ID) pact.FulfilledA = true;
            if (pact.PartyB == party.ID) pact.FulfilledB = true;
        }

        /// <summary>The mirror veils the member's next breach from Heaven (a loophole, at a cost of power).</summary>
        public bool VeilNextBreach(CharacterData member)
        {
            if (member == null || !Allows(LoopholeKind.MirrorVeil) || !mirror.ConsumePower(Settings.MirrorVeilCost)) return false;
            veiled.Add(member.ID);
            return true;
        }

        /// <summary>Pills and calm may lift a Heart Demon (a loophole): herbs spent, a chance.</summary>
        public bool Purify(CharacterData member)
        {
            if (member == null || member.HeartDemonYearsLeft <= 0 || !Allows(LoopholeKind.Purification)
                || !resources.ConsumeHerbs(Settings.PurificationHerbs)) return false;
            if (!ctx.Rng.Chance(Settings.PurificationChance)) return false;
            member.HeartDemonYearsLeft = 0;
            ctx.Log.Info($"[Oaths] {member.FullName}'s Heart Demon is lifted.");
            return true;
        }

        public void Restore(IEnumerable<PactData> saved)
        {
            pacts.Clear();
            pacts.AddRange(saved ?? Enumerable.Empty<PactData>());
        }

        private ClauseDefinition Clause(string id) => ctx.Content.Oaths.Clauses.FirstOrDefault(c => c.Id == id);

        /// <summary>The pact binds the actor towards the victim, unless the actor swore under a false name.</summary>
        private static bool Binds(PactData p, CharacterData actor, CharacterData victim) =>
            (p.PartyA == actor.ID && p.PartyB == victim.ID && !p.FalseNameA)
            || (p.PartyB == actor.ID && p.PartyA == victim.ID && !p.FalseNameB);

        private void Punish(CharacterData oathBreaker, int severity)
        {
            if (veiled.Remove(oathBreaker.ID))
            {
                ctx.Log.Info($"[Oaths] The mirror veils {oathBreaker.FullName}'s breach from Heaven.");
                return;
            }

            int index = Math.Clamp(severity, 1, Settings.InterruptChanceBySeverity.Count) - 1;
            if (ctx.Rng.NextDouble() < Settings.InterruptChanceBySeverity[index])
            {
                oathBreaker.ProgressionSealed = true;
                if (ctx.Rng.NextDouble() < Settings.DeviationChanceOnInterrupt)
                {
                    oathBreaker.DaoWounds++;
                    oathBreaker.MaxLifespan = PowerLadder.WoundedLifespan(oathBreaker.MaxLifespan, 1);
                }
                ctx.Log.Info($"[Oaths] {oathBreaker.FullName} broke an oath: their path is interrupted.");
                return;
            }

            oathBreaker.HeartDemonYearsLeft = Math.Max(oathBreaker.HeartDemonYearsLeft, Settings.HeartDemonYearsBySeverity[index]);
            oathBreaker.MentalStability = Math.Clamp(oathBreaker.MentalStability - Settings.HeartDemonStabilityLoss, 0, 100);
            ctx.Log.Info($"[Oaths] {oathBreaker.FullName} broke an oath: a Heart Demon haunts them.");
        }

        /// <summary>Heart Demons fade; a service unrendered by the deadline breaks the oath; pacts lapse at their end or a party's death.</summary>
        private void PassYear(int year)
        {
            foreach (var member in clan.LivingMembers.Where(m => m.HeartDemonYearsLeft > 0).ToList())
                member.HeartDemonYearsLeft--;

            foreach (var pact in pacts.ToList())
            {
                var a = clan.FindById(pact.PartyA);
                var b = clan.FindById(pact.PartyB);
                bool due = pact.ExpiresYear.HasValue && year > pact.ExpiresYear.Value;
                if (due && pact.Clauses.Select(Clause).Any(c => c?.Act == OathAct.Service))
                {
                    int severity = pact.Clauses.Select(Clause).Where(c => c?.Act == OathAct.Service).Max(c => c.Severity);
                    if (a != null && a.IsAlive && !pact.FulfilledA && !pact.FalseNameA) Punish(a, severity);
                    if (b != null && b.IsAlive && !pact.FulfilledB && !pact.FalseNameB) Punish(b, severity);
                }
                bool lapsed = (due && Allows(LoopholeKind.Expiry)) || a == null || b == null || !a.IsAlive || !b.IsAlive;
                if (lapsed) pacts.Remove(pact);
            }
        }
    }
}
