using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers scheme against the clan for profit (L6a; LORE.md D7 « everything is a plot »: the only true friend is
    /// profit). Each year a power may ambush a member away from the domain — on an errand, a hunt, a diversion — to hold
    /// them for ransom; greed, hostility and temper drive it, friendship only makes it rarer. A failed ambush may leave
    /// its agent in the clan's hands. A power confronting the clan over the mirror plays that game instead.
    /// </summary>
    public sealed class SchemeSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly CaptiveSystem captives;
        private readonly SecretSystem secrets;
        private readonly TreatySystem treaties;
        private readonly PowerPoliticsSystem politics;
        private readonly PatronSystem patrons;

        public SchemeSystem(GameContext ctx, ClanManager clan, FactionManager factions, CaptiveSystem captives, SecretSystem secrets,
            TreatySystem treaties = null, PowerPoliticsSystem politics = null, PatronSystem patrons = null)
        {
            this.patrons = patrons;
            this.politics = politics;
            this.treaties = treaties;
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.captives = captives;
            this.secrets = secrets;
        }

        private SchemeSettings Settings => ctx.Content.Balance.Schemes;

        public void ProcessYear()
        {
            int stones = captives.ClanStones;
            int ambushes = 0;
            foreach (var power in factions.Factions.OrderBy(_ => ctx.Rng.Next()).ToList()) // no power always first
            {
                if (ambushes >= Settings.MaxAmbushesPerYear) return;
                if (secrets.Confrontation?.Faction == power.Name) continue; // its move is the confrontation's
                if (treaties?.Spares(power.Name) == true) continue;           // spared by non-aggression, or as the clan's suzerain (betrayal is the treaty's own path)
                double factor = politics?.SchemeFactor(power) ?? 1.0; // a coalition's member schemes more
                if (ctx.Rng.Chance(Math.Min(1.0, SchemeRules.SchemeChance(power, stones, Settings) * factor)) && Ambush(power)) ambushes++;
            }
        }

        /// <summary>
        /// An ambush on a member away from the domain, for profit (the user's choice, 2026-10-01): one the clan's treasury
        /// could ransom, or — for a power hostile enough — a hostage against the quarrel; false when nobody worth it was within
        /// reach (a poor young clan was taken twelve times in sixty years, and died out).
        /// </summary>
        /// <summary>The chance the clan's Dharma Treasures and Rank Designations foil an ambush on this member (L4e; set by the session).</summary>
        public System.Func<CharacterData, double> Guard { get; set; }

        public bool Ambush(FactionData power)
        {
            var exposed = clan.LivingMembers.Where(IsAway).Where(m => WorthTaking(power, m)).ToList();
            if (exposed.Count == 0) return false;

            var target = ctx.Rng.Pick(exposed);
            ctx.Events.TriggerMemberImperilled(target); // whatever comes of it, a peril lived (the Mandate of Life)
            var guardian = treaties?.Guardians(against: power.Name).FirstOrDefault();
            if (guardian != null && ctx.Rng.Chance(ctx.Content.Balance.Treaties.DefenceGuardChance))
            {
                ctx.Log.Info($"[Schemes] {guardian}'s escort foils {power.Name}'s ambush on {target.FullName}.");
                return true;
            }
            if (Guard?.Invoke(target) is double treasure && treasure > 0 && ctx.Rng.Chance(treasure))
            {
                ctx.Log.Info($"[Schemes] A Dharma Treasure foils {power.Name}'s ambush on {target.FullName}.");
                return true;
            }
            if (patrons != null && patrons.GuardChance > 0 && ctx.Rng.Chance(patrons.GuardChance))
            {
                ctx.Log.Info($"[Schemes] A great partner's shadow foils {power.Name}'s ambush on {target.FullName}.");
                return true;
            }
            if (ctx.Rng.Chance(SchemeRules.CaptureChance(power, target, Settings)))
            {
                captives.Take(target, power.Name);
                return true;
            }
            if (ctx.Rng.Chance(Settings.AgentTakenChance))
                captives.Imprison(new Prisoner($"agent-{power.ID}-{ctx.Clock.Year}", power.Name, target.Realm, ctx.Clock.Year));
            ctx.Log.Info($"[Schemes] {target.FullName} escapes an ambush.");
            return true;
        }

        private bool WorthTaking(FactionData power, CharacterData member) =>
            captives.ClanStones >= SchemeRules.Ransom(member.Realm, Settings) || power.RelationWithPlayer <= Settings.HostageRelation;

        private bool IsAway(CharacterData member) =>
            member.CaptorFaction == null
            && (Settings.AwayTasks.Contains(member.CurrentTask) || member.LastOperationYear == ctx.Clock.Year);
    }
}
