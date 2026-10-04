using System;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// A reborn True Monarch bends lesser minds (audit §1.8, the user's decision 2026-10-03; LORE.md §5.4). A power's True
    /// Monarch come back from rebirth, able to reach the clan, may bend a member below the Purple Mansion into its unwitting
    /// spy; the clan sees only an absent gaze, the mirror sounds and breaks it, and the Purple Mansion frees itself. What
    /// befalls the clan befalls the world: the clan's returned ancestor bends a power's lesser elder into its eyes there.
    /// </summary>
    public sealed class Enthrallment
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly MirrorSystem mirror;
        private readonly SuspicionLedger suspicion;

        public Enthrallment(GameContext ctx, ClanManager clan, FactionManager factions, MirrorSystem mirror, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.mirror = mirror;
            this.suspicion = suspicion;
        }

        private EnthrallmentSettings Settings => ctx.Content.Balance.Enthrallment;

        public void ProcessYear()
        {
            foreach (var freed in clan.LivingMembers.Where(m => m.Enthralled && m.Realm >= CultivationRealm.PurpleMansion).ToList())
            {
                Free(freed);
                ctx.Log.Info($"[Enthrallment] {freed.FullName}, risen to the Purple Mansion, shakes off the spell.");
            }
            foreach (var power in factions.Factions.Where(p => p.Elders.Any(e => e.Reborn && e.Realm >= CultivationRealm.GoldenCore)).ToList())
            {
                if (!TravelRules.PowerReaches(power, ctx.Content.Clan.HomeRegion, ctx.Content) || !ctx.Rng.Chance(Settings.YearlyChance)) continue;
                var prey = clan.LivingMembers.Where(m => m.Realm < CultivationRealm.PurpleMansion && m.CaptorFaction == null && m.DiscipleOf == null
                    && m.SpyFor == null && m.Age >= TaskRules.WorkingAge).ToList();
                if (prey.Count == 0) continue;
                var bent = ctx.Rng.Pick(prey);
                bent.Enthralled = true;
                bent.SpyFor = power.Name;
                ctx.Log.Info($"[Enthrallment] {power.Name}'s reborn True Monarch bends {bent.FullName}'s mind (hidden).");
            }
        }

        /// <summary>The mirror breaks the spell on a member it sounded. Null when done; else why not (French).</summary>
        public string Break(string memberId)
        {
            var member = clan.FindById(memberId);
            if (member == null || !member.IsAlive) return "membre introuvable";
            if (!member.Enthralled || !member.SpyUnmasked) return "le miroir n'a trouvé aucun envoûtement sur ce membre";
            if (mirror.PayRefusal(Settings.BreakMirrorCost) is { } refusal) return refusal;
            mirror.ConsumePower(Settings.BreakMirrorCost);
            ctx.Log.Info($"[Enthrallment] The mirror breaks {member.SpyFor}'s spell on {member.FullName}.");
            Free(member);
            return null;
        }

        private static void Free(CharacterData member)
        {
            member.Enthralled = false;
            member.SpyFor = null;
            member.SpyUnmasked = false;
            member.DoubleAgent = false;
        }

        // ---- The clan's returned ancestor bends a power's elder ----

        /// <summary>A returned ancestor of the clan: a True Monarch reborn and risen again, free to act this year.</summary>
        public bool IsReturnedAncestor(CharacterData m) =>
            m != null && m.IsAlive && m.RebornFrom != null && m.Realm >= CultivationRealm.GoldenCore && m.CaptorFaction == null
            && m.LastOperationYear != ctx.Clock.Year;

        /// <summary>The ancestor's odds of bending a lesser elder of the power: its strength against the power's guard.</summary>
        public double BendChance(CharacterData ancestor, FactionData power)
        {
            if (!IsReturnedAncestor(ancestor) || power == null || !power.Elders.Any(e => e.Realm < CultivationRealm.PurpleMansion && !e.ThrallOfClan)) return 0;
            if (!TravelRules.CanReach(ancestor.Realm, ctx.Content.Clan.HomeRegion, power.RegionId, ctx.Content.Regions, ctx.Content.Balance.Travel)) return 0;
            var s = Settings;
            return Math.Clamp(s.BendBase + (HuntRules.Power(ancestor) - HuntRules.Power(power.HighestRealm, 5)) * s.BendPerPower, 0.05, 0.95);
        }

        /// <summary>The clan's returned ancestor bends a lesser elder of the power: its eyes there. Null when done; else why not (French).</summary>
        public string Bend(string ancestorId, string powerName)
        {
            var ancestor = clan.FindById(ancestorId);
            if (!IsReturnedAncestor(ancestor)) return "seul un ancêtre revenu, Vrai Monarque libre, plie les esprits";
            var power = factions.GetFactionByName(powerName);
            double chance = BendChance(ancestor, power);
            if (chance <= 0) return "aucun esprit de cette puissance n'est à sa portée";
            ancestor.LastOperationYear = ctx.Clock.Year;
            if (ctx.Rng.Chance(Settings.SeenChance)) suspicion.AddToClan(power.Name, Settings.SeenDistrust);
            if (!ctx.Rng.Chance(chance)) return "l'esprit résiste";
            var elder = power.Elders.Where(e => e.Realm < CultivationRealm.PurpleMansion && !e.ThrallOfClan).OrderByDescending(e => e.Realm).First();
            elder.ThrallOfClan = true;
            ctx.Log.Info($"[Enthrallment] {ancestor.FullName} bends the mind of {elder.Name} of {power.Name}.");
            return null;
        }
    }
}
