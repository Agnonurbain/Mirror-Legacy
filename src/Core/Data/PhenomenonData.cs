using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    public enum PhenomenonKind { Death, Failure }

    /// <summary>
    /// A phenomenon over a region (LORE.md §5.3.5, §5.4.5): after a cultivator's death, an unusual weather tied to its
    /// foundation (its lineage's cultivators go faster there); after a failed Golden Core, a lasting celestial phenomenon
    /// that slows every cultivator of the region. Until <see cref="UntilYear"/> included.
    /// </summary>
    public sealed record RegionalPhenomenon(PhenomenonKind Kind, string RegionId, string Lineage, double GeneralSpeed, double AlignedSpeed,
        int UntilYear, string Source);

    /// <summary>What a death of this realm leaves: how long its weather lasts, how much it favours its kin, the things its body turns to.</summary>
    public sealed record DeathPhenomenonSettings
    {
        public int Years { get; init; }
        public double AlignedSpeed { get; init; }
        public int Herbs { get; init; }
        public int Ores { get; init; }
    }

    /// <summary>
    /// The phenomena (balance.json « phenomena », L4c, 2026-10-03 — interpretations; the failure's 2.5 % is the lore's).
    /// </summary>
    public sealed record PhenomenaSettings
    {
        public Dictionary<CultivationRealm, DeathPhenomenonSettings> Deaths { get; init; } = new Dictionary<CultivationRealm, DeathPhenomenonSettings>();
        public double FailureSpeed { get; init; } = -0.025;
        public int FailureYears { get; init; } = 300;
        public double MaxAlignedSpeed { get; init; } = 0.3;  // the weathers of many deaths favour a lineage at most this much
        public double MinGeneralSpeed { get; init; } = -0.2; // the lasting phenomena slow a region at most this much

        public int FoundationDeathYears => Deaths.TryGetValue(CultivationRealm.Foundation, out var d) ? d.Years : 0;
    }
}
