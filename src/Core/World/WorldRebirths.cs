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
