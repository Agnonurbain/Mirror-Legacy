using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>A rival of a patron that knows of its accord with the clan (saved).</summary>
    public sealed record AscentRival(string Power, string SponsorshipId);

    /// <summary>What those who hinder an ascent do (balance.json « ascentRivals »; LORE.md §11.10; 🔎).</summary>
    public sealed record AscentRivalSettings
    {
        public double LearnChance { get; init; }      // a rival of the patron learns of the accord, each year
        public double StrikeChance { get; init; }     // it strikes a practitioner in manifestation retreat
        public double DenounceChance { get; init; }   // it denounces the accord to all
        public double BuyChance { get; init; }        // it tries to buy the practitioner (less against a stable mind)
        public int OutbidStones { get; init; }        // what it pays the clan for resisting its patron
        public int FoiledDistrust { get; init; }      // the clan's distrust of a rival whose blow it foiled
        public int DenounceDistrust { get; init; }    // every power's distrust of an exposed patron
        public int DenounceRelationLoss { get; init; } // the exposed patron's grudge against the clan
    }

    /// <summary>
    /// Those who hinder an ascent (LORE.md §11.10; 2026-10-01): a rival of the patron — the power that distrusts it most, else a
    /// strong one — may learn of the accord. It then strikes a practitioner during the manifestation retreat (the clan's guards
    /// may foil it, as against a ripe Dao's hunters), denounces the accord to all (the exposed patron drops it, and resents the
    /// clan), or tries to buy the practitioner away; and it pays the clan to resist its patron.
    /// </summary>
    public sealed class AscentRivals
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly Sponsorships sponsorships;
        private readonly TreatySystem treaties;
        private readonly BuildingSystem buildings;
        private readonly ResourceManager resources;
        private readonly List<AscentRival> known = new List<AscentRival>();

        public IReadOnlyList<AscentRival> Known => known;

        public AscentRivals(GameContext ctx, ClanManager clan, FactionManager factions, SuspicionLedger suspicion, Sponsorships sponsorships,
            TreatySystem treaties, BuildingSystem buildings, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.suspicion = suspicion;
            this.sponsorships = sponsorships;
            this.treaties = treaties;
            this.buildings = buildings;
            this.resources = resources;
            ctx.Events.OnPatronDesignResisted += (s, d) =>
            {
                if (known.Any(k => k.SponsorshipId == s.Id)) resources.AddSpiritStones(Settings.OutbidStones); // the rival pays for it
            };
        }

        private AscentRivalSettings Settings => ctx.Content.Balance.AscentRivals;

        public void ProcessYear()
        {
            foreach (var rival in known.ToList()) Act(rival);
            foreach (var s in sponsorships.Active.Where(s => known.All(k => k.SponsorshipId != s.Id)).ToList()) Learn(s);
        }

        private void Learn(Sponsorship s)
        {
            var rival = factions.Factions.Where(f => f.Name != s.Power && f.HighestRealm >= CultivationRealm.PurpleMansion)
                .OrderByDescending(f => suspicion.Distrust(f.Name, s.Power)).ThenByDescending(f => (int)f.HighestRealm).ThenByDescending(f => f.PowerLevel)
                .FirstOrDefault();
            if (rival == null || !ctx.Rng.Chance(Settings.LearnChance)) return;
            known.Add(new AscentRival(rival.Name, s.Id));
            ctx.Log.Info($"[Rivals] {rival.Name} learns of {s.Power}'s accord with the clan.");
        }

        private void Act(AscentRival rival)
        {
            var s = sponsorships.Active.FirstOrDefault(x => x.Id == rival.SponsorshipId);
            var power = factions.GetFactionByName(rival.Power);
            if (s == null || power == null) { known.Remove(rival); return; }

            var practitioners = clan.LivingMembers.Where(m => m.CultivationMethodId == s.TechniqueId && m.CaptorFaction == null).ToList();
            var retreating = practitioners.FirstOrDefault(m => m.Retreat == Retreat.Manifestation);
            if (retreating != null && ctx.Rng.Chance(Settings.StrikeChance)) Sabotage(power, retreating);

            if (ctx.Rng.Chance(Settings.DenounceChance)) { Denounce(power, s); known.Remove(rival); return; }

            var target = practitioners.Where(m => m.IsAlive && m.ID != clan.PatriarchID && m.Retreat == Retreat.None)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).FirstOrDefault();
            if (target != null && ctx.Rng.Chance(Settings.BuyChance * (1 - target.MentalStability / 100.0)))
            {
                clan.Depart(target); // gone over to the rival
                ctx.Events.TriggerMemberLured(target, power.Name);
                ctx.Log.Warning($"[Rivals] {power.Name} buys {target.FullName} away from the clan.");
            }
        }

        /// <summary>A blow during the manifestation retreat; the clan's guards may foil it.</summary>
        private void Sabotage(FactionData power, CharacterData member)
        {
            var hunts = ctx.Content.Balance.DaoHunts;
            bool guardian = clan.LivingMembers.Any(m => m != member && m.CaptorFaction == null && m.Realm >= CultivationRealm.PurpleMansion);
            int patrols = clan.LivingMembers.Count(m => m.CaptorFaction == null && m.CurrentTask == TaskType.Patrol);
            bool ally = treaties.All.Any(t => t.Kind == TreatyKind.Defence);
            double success = DaoHuntRules.StrikeSuccess(guardian, patrols, buildings.FormationLevel, ally, false, hunts, ctx.Content.Balance.RipeDaoGuardedFactor);
            if (ctx.Rng.Chance(success))
            {
                ctx.Log.Warning($"[Rivals] {power.Name} strikes {member.FullName} during the manifestation: it breaks.");
                clan.Kill(member, DeathCause.ManifestationCollapse);
                return;
            }
            suspicion.AddClanDistrust(power.Name, Settings.FoiledDistrust);
            ctx.Log.Info($"[Rivals] {power.Name}'s blow at {member.FullName}'s manifestation is foiled.");
        }

        /// <summary>The accord exposed: every power distrusts the patron, which drops it and resents the clan.</summary>
        private void Denounce(FactionData rival, Sponsorship s)
        {
            foreach (var f in factions.Factions.Where(f => f.Name != s.Power)) suspicion.AddDistrust(f.Name, s.Power, Settings.DenounceDistrust);
            if (factions.GetFactionByName(s.Power) is { } patron) factions.ChangeRelation(patron.ID, -Settings.DenounceRelationLoss);
            sponsorships.Dissolve(s.Id);
            ctx.Events.TriggerAccordDenounced(rival.Name, s.Power);
            ctx.Log.Warning($"[Rivals] {rival.Name} denounces {s.Power}'s accord with the clan: {s.Power} drops it.");
        }

        public void Restore(IEnumerable<AscentRival> saved)
        {
            known.Clear();
            known.AddRange(saved ?? Enumerable.Empty<AscentRival>());
        }
    }
}
