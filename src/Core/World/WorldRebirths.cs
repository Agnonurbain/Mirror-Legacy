using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>A power's True Monarch awaiting its return: its power, its name, the Realization it held, the year it was reborn.</summary>
    public sealed record WorldRebirth(string Power, string Name, string FruitionId, int BornYear);

    /// <summary>
    /// The powers' ancestors come back (the user's rule, 2026-10-03: what befalls the clan befalls the world; the Ancestor's
    /// Return, R9). A True Monarch of a power whose essence is intact — no demon — is reborn in its power by the clan's
    /// odds; until its power can hide it (the clan's Foundation age), it may be harvested; grown to the clan's own pace, it
    /// stands again at the Golden Core, its Realization its own again if free or still in its name.
    /// </summary>
    public sealed class WorldRebirths
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly FruitionRegistry registry;
        private readonly List<WorldRebirth> pending = new List<WorldRebirth>();

        public WorldRebirths(GameContext ctx, FactionManager factions, FruitionRegistry registry)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.registry = registry;
            ctx.Events.OnElderDied += Died;
        }

        private AncestorSettings Settings => ctx.Content.Balance.Ancestors;

        public IReadOnlyList<WorldRebirth> Pending => pending;

        /// <summary>The clan, to harvest a power's Chosen (set by the session).</summary>
        public Clan.ClanManager Clan { get; set; }

        /// <summary>Who remembers the clan's deeds (set by the session).</summary>
        public SuspicionLedger Suspicion { get; set; }

        /// <summary>A member who may go this year: alive, free, not in retreat, no operation yet.</summary>
        private bool Free(CharacterData m) =>
            m != null && m.IsAlive && m.CaptorFaction == null && m.Retreat == Retreat.None && m.LastOperationYear != ctx.Clock.Year;

        /// <summary>
        /// The powers' young Chosen the clan senses: only a free Purple Mansion of the clan reads fate (LORE.md §5.4; audit
        /// §1.8, the user's decision 2026-10-03), and only those it can reach.
        /// </summary>
        public IReadOnlyList<WorldRebirth> SensedByClan()
        {
            var seers = Clan?.LivingMembers.Where(m => Free(m) && m.Realm >= CultivationRealm.PurpleMansion).ToList() ?? new List<CharacterData>();
            if (seers.Count == 0) return new List<WorldRebirth>();
            int year = ctx.Clock.Year;
            return pending.Where(r => year - r.BornYear < HiddenAge && factions.GetFactionByName(r.Power) is { } p
                && seers.Any(m => TravelRules.CanReach(m.Realm, ctx.Content.Clan.HomeRegion, p.RegionId, ctx.Content.Regions, ctx.Content.Balance.Travel)))
                .ToList();
        }

        /// <summary>The clan's odds of harvesting this power's Chosen with this Purple Mansion: its strength against the power's guard.</summary>
        public double HarvestChance(string power, CharacterData harvester)
        {
            var p = factions.GetFactionByName(power);
            if (p == null || harvester == null) return 0;
            var s = Settings;
            var gap = ctx.Content.Balance.RealmGap;
            if (!RealmGap.Reaches(new[] { harvester }, p.HighestRealm, gap)) return 0;
            double strength = RealmGap.TeamStrength(new[] { harvester }, p.HighestRealm, 1.0, gap);
            return System.Math.Clamp(s.ClanHarvestBase + (strength - Mirror.HuntRules.Power(p.HighestRealm, 5)) * s.ClanHarvestPerPower,
                s.ClanHarvestMin, s.ClanHarvestMax);
        }

        /// <summary>
        /// The clan's Purple Mansion harvests a power's young Chosen: its True Monarch will not come back. Seen, the power
        /// distrusts the clan and holds it a grudge. Null when done; else why not, or « la récolte échoue » (French).
        /// </summary>
        public string Harvest(string power, string harvesterId)
        {
            var harvester = Clan?.FindById(harvesterId);
            if (harvester == null || !Free(harvester)) return "ce membre ne peut pas partir";
            if (harvester.Realm < CultivationRealm.PurpleMansion) return "seul un Manoir Pourpre manipule le destin";
            var chosen = SensedByClan().FirstOrDefault(r => r.Power == power);
            if (chosen == null) return "le clan ne sent aucun Élu de cette puissance";
            var p = factions.GetFactionByName(power);
            double chance = HarvestChance(power, harvester);
            if (chance <= 0) return $"{power} est d'un royaume hors de sa portée";
            harvester.LastOperationYear = ctx.Clock.Year;
            var s = Settings;
            bool seen = ctx.Rng.Chance(s.ClanHarvestSeenChance);
            if (seen)
            {
                Suspicion?.AddToClan(power, s.ClanHarvestDistrust);
                factions.ChangeRelation(p.ID, s.ClanHarvestRelation);
            }
            if (!ctx.Rng.Chance(chance))
            {
                ctx.Log.Info($"[Rebirths] The clan fails to harvest {chosen.Name} of {power}{(seen ? ", and is seen" : "")}.");
                return "la récolte échoue" + (seen ? ", et le clan est vu" : "");
            }
            pending.Remove(chosen);
            ctx.Log.Warning($"[Rebirths] {harvester.FullName} harvests {chosen.Name}, the Chosen of {power}{(seen ? ", and is seen" : "")}.");
            return null;
        }

        public void Restore(IEnumerable<WorldRebirth> saved)
        {
            pending.Clear();
            if (saved != null) pending.AddRange(saved);
        }

        private int HiddenAge => TaskRules.CultivationAge + Settings.FoundationYears;
        private int ReturnAge => HiddenAge + Settings.PurpleMansionYears + Settings.GoldenCoreYears;

        private void Died(FactionData power, FactionElder elder, bool demon)
        {
            if (demon || elder.Realm < CultivationRealm.GoldenCore || power == null || !ctx.Rng.Chance(Settings.RebirthChance)) return;
            pending.Add(new WorldRebirth(power.Name, elder.Name, elder.FruitionId, ctx.Clock.Year));
            ctx.Log.Info($"[Rebirths] {elder.Name} of {power.Name} is reborn in its power.");
        }

        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            foreach (var r in pending.ToList())
            {
                var power = factions.GetFactionByName(r.Power);
                int age = year - r.BornYear;
                if (power == null || (age < HiddenAge && AHarvesterReaches(power) && ctx.Rng.Chance(Settings.HarvestChance)))
                {
                    pending.Remove(r); // its power is gone, or a stronger one harvested the Chosen
                    continue;
                }
                if (age < ReturnAge) continue;
                pending.Remove(r);
                Return(power, r, year);
            }
        }

        /// <summary>Only another power's Purple Mansion, able to reach it, can harvest its Chosen (LORE.md §5.4; 2026-10-03).</summary>
        private bool AHarvesterReaches(FactionData power) =>
            factions.Factions.Any(f => f != power && f.HighestRealm >= CultivationRealm.PurpleMansion && TravelRules.PowerReaches(f, power.RegionId, ctx.Content));

        private void Return(FactionData power, WorldRebirth r, int year)
        {
            var elder = new FactionElder
            {
                Id = ctx.Rng.NextId(), Name = r.Name, Realm = CultivationRealm.GoldenCore, Stage = 1, BornYear = year - ReturnAge,
                MaxLifespan = PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1), RealmSinceYear = year, Perfected = true,
            };
            var state = r.FruitionId == null ? null : registry.State(r.FruitionId);
            if (state?.Status == FruitionStatus.Free && registry.Claim(r.FruitionId, r.Name)) elder.FruitionId = r.FruitionId;
            else if (state?.Status == FruitionStatus.Occupied && state.Holder == r.Name) elder.FruitionId = r.FruitionId;
            power.Elders.Add(elder);
            ElderSystem.Sync(power);
            ctx.Log.Info($"[Rebirths] {r.Name} of {power.Name} stands again at the Golden Core.");
            if (elder.FruitionId != null) ctx.Events.TriggerFruitionTaken(elder.FruitionId, elder.Name);
        }
    }
}
