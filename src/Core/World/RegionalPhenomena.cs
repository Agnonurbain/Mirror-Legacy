using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The phenomena over the regions (LORE.md §5.3.5, §5.4.5; L4c, 2026-10-03). A cultivator of the Foundation or above
    /// who dies leaves an unusual weather tied to its foundation — its lineage's cultivators go faster there for some years
    /// — and, a member of the clan, its body turns to spiritual things the clan gathers. A failed Golden Core leaves a
    /// lasting celestial phenomenon that slows every cultivator of the region. One exhausted before the Shenyang Mansion
    /// leaves nothing. The powers' elders leave theirs in their own regions.
    /// </summary>
    public sealed class RegionalPhenomena
    {
        private readonly GameContext ctx;
        private readonly ResourceManager resources;
        private readonly List<RegionalPhenomenon> active = new List<RegionalPhenomenon>();

        public RegionalPhenomena(GameContext ctx, ResourceManager resources)
        {
            this.ctx = ctx;
            this.resources = resources;
            ctx.Events.OnCharacterDied += MemberDied;
            ctx.Events.OnElderDied += (power, elder, demon) => Died(power.RegionId, elder.Realm, elder.FruitionId, elder.Name, demon, clan: false);
        }

        private PhenomenaSettings Settings => ctx.Content.Balance.Phenomena;

        public IReadOnlyList<RegionalPhenomenon> Active => active;

        public void Restore(IEnumerable<RegionalPhenomenon> saved)
        {
            active.Clear();
            if (saved != null) active.AddRange(saved);
        }

        public void ProcessYear() => active.RemoveAll(p => p.UntilYear < ctx.Clock.Year);

        /// <summary>How the region's phenomena weigh on a cultivator of this lineage (1: not at all).</summary>
        public double SpeedFactor(string regionId, string lineage)
        {
            var here = active.Where(p => p.RegionId == regionId).ToList();
            if (here.Count == 0) return 1.0;
            var s = Settings; // many deaths never pile up without end
            double general = System.Math.Max(s.MinGeneralSpeed, here.Sum(p => p.GeneralSpeed));
            double aligned = System.Math.Min(s.MaxAlignedSpeed, here.Where(p => lineage != null && p.Lineage == lineage).Sum(p => p.AlignedSpeed));
            return 1 + general + aligned;
        }

        private void MemberDied(CharacterData dead, DeathCause cause)
        {
            if (cause == DeathCause.AscentCollapse) return; // grandiose but fleeting, without spiritual trace
            string lineage = FoundationRef.Parse(dead.FoundationId).FruitionId ?? dead.FruitionId;
            Died(ctx.Content.Clan.HomeRegion, dead.Realm, lineage, dead.FullName, cause == DeathCause.MetalEssenceDemon, clan: true);
        }

        private void Died(string regionId, CultivationRealm realm, string lineage, string who, bool failed, bool clan)
        {
            if (regionId == null || realm < CultivationRealm.Foundation) return;
            var s = Settings;
            if (failed)
            {
                Add(new RegionalPhenomenon(PhenomenonKind.Failure, regionId, lineage, s.FailureSpeed, 0, ctx.Clock.Year + s.FailureYears, who));
                return;
            }
            var scale = s.Deaths.Where(d => d.Key <= realm).OrderByDescending(d => d.Key).Select(d => d.Value).FirstOrDefault();
            if (scale == null) return;
            if (clan) // its body turns to things associated with its foundation
            {
                resources.AddHerbs(scale.Herbs);
                resources.AddOres(scale.Ores);
            }
            if (lineage != null)
                Add(new RegionalPhenomenon(PhenomenonKind.Death, regionId, lineage, 0, scale.AlignedSpeed, ctx.Clock.Year + scale.Years, who));
        }

        private void Add(RegionalPhenomenon phenomenon)
        {
            active.Add(phenomenon);
            ctx.Log.Info($"[Phenomena] {phenomenon.Kind} over {phenomenon.RegionId} after {phenomenon.Source}, until {phenomenon.UntilYear}.");
            ctx.Events.TriggerPhenomenon(phenomenon);
        }
    }
}
