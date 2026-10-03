using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The Qi of the places (L5b; LORE.md §2.5, §5.8). A place offers the Qi its sources name, or else those of the
    /// elements its kind carries, with the Twelve Qi found everywhere and the common breath. Every Qi depends on a
    /// lineage: rich where the lineage's authority flows unhindered (held), poor where it is broken — so a Qi's
    /// abundance is the place's density times its lineage's fortune. An atmosphere weighs on the place's cultivators and
    /// favours some. The clan cultivates at home: a member whose Qi the place does not offer goes slower.
    /// </summary>
    public sealed class RegionalQi
    {
        private readonly GameContext ctx;
        private readonly FruitionRegistry fruitions;
        private readonly TechniqueLibrary techniques;
        private readonly Dictionary<string, IReadOnlyList<QiDefinition>> qiByRegion = new Dictionary<string, IReadOnlyList<QiDefinition>>(); // content never changes
        private Dictionary<string, RegionDefinition> regions;

        public RegionalQi(GameContext ctx, FruitionRegistry fruitions, TechniqueLibrary techniques)
        {
            this.ctx = ctx;
            this.fruitions = fruitions;
            this.techniques = techniques;
        }

        private RegionalQiSettings Settings => ctx.Content.Balance.RegionalQi;

        /// <summary>The phenomena over the regions (L4c); set by the session.</summary>
        public RegionalPhenomena Phenomena { get; set; }

        private RegionDefinition Region(string regionId)
        {
            regions ??= ctx.Content.Regions.ToDictionary(r => r.Id);
            return regionId != null && regions.TryGetValue(regionId, out var region) ? region : null;
        }

        /// <summary>The Qi a place offers (none for an unknown place); vanished Qi are nowhere.</summary>
        public IReadOnlyList<QiDefinition> QiOf(string regionId)
        {
            var region = Region(regionId);
            if (region == null) return new List<QiDefinition>();
            if (!qiByRegion.TryGetValue(region.Id, out var qi))
                qiByRegion[region.Id] = qi = ctx.Content.Qi.Where(q => Offers(region, q)).ToList();
            return qi;
        }

        private bool Offers(RegionDefinition region, QiDefinition qi)
        {
            if (qi.Vanished) return false;
            if (qi.Ubiquitous) return true;
            if (region.Qi != null) return region.Qi.Contains(qi.Id);
            return (Settings.KindElements.TryGetValue(region.Kind, out var elements) && elements.Contains(qi.Element))
                || Settings.EverywhereFamilies.Contains(qi.Family);
        }

        public double Density(string regionId)
        {
            var region = Region(regionId);
            if (region == null) return 0;
            return region.QiDensity ?? (Settings.KindDensity.TryGetValue(region.Kind, out var d) ? d : 1.0);
        }

        /// <summary>How abundant a Qi is in a place: its density times its lineage's fortune; 0 where it is not offered.</summary>
        public double Abundance(string regionId, QiDefinition qi)
        {
            var region = Region(regionId);
            if (qi == null || region == null || !Offers(region, qi)) return 0;
            if (qi.Ubiquitous) return Density(regionId);
            var lineage = FoundationRef.Parse(qi.Foundation).FruitionId;
            var status = lineage == null ? FruitionStatus.Unspecified : fruitions.State(lineage)?.Status ?? FruitionStatus.Unspecified;
            return Density(regionId) * (Settings.LineageStatusFactors.TryGetValue(status, out var f) ? f : 1.0);
        }

        public AtmosphereDefinition AtmosphereOf(string regionId)
        {
            var id = Region(regionId)?.AtmosphereId;
            return id == null ? null : ctx.Content.Atmospheres.FirstOrDefault(a => a.Id == id);
        }

        /// <summary>A member's cultivation pace at home: the abundance of their method's Qi (slower when absent), under the atmosphere.</summary>
        public double SpeedFactor(CharacterData member)
        {
            string home = ctx.Content.Clan.HomeRegion;
            var qi = techniques.FindQi(techniques.MethodOf(member)?.RequiredQiId);
            double abundance = qi == null ? 1.0 : Abundance(home, qi);
            if (qi != null && abundance <= 0) abundance = Settings.AbsentQiFactor; // a Qi brought from afar, and scarce
            var (lineage, element) = Affinity(member, qi);
            return abundance * RegionalQiRules.AtmosphereSpeed(AtmosphereOf(home), lineage, element, member.Path)
                * (Phenomena?.SpeedFactor(home, lineage) ?? 1.0);
        }

        /// <summary>The points of breakthrough chance the home atmosphere gives the member.</summary>
        public int BreakthroughBonus(CharacterData member)
        {
            var qi = techniques.FindQi(techniques.MethodOf(member)?.RequiredQiId);
            var (lineage, element) = Affinity(member, qi);
            return RegionalQiRules.AtmosphereBreakthrough(AtmosphereOf(ctx.Content.Clan.HomeRegion), lineage, element, member.Path);
        }

        /// <summary>The lineage a member stands on (their foundation's, else their Qi's) and their Qi's element.</summary>
        private static (string Lineage, Element Element) Affinity(CharacterData member, QiDefinition qi) =>
            (FoundationRef.Parse(member.FoundationId).FruitionId ?? FoundationRef.Parse(qi?.Foundation).FruitionId, qi?.Element ?? Element.None);
    }
}
