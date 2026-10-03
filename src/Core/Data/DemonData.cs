using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The hierarchy of the Metal Essence Demons (LORE.md §6.9): born of a Realization's holder, catastrophes that rival a
    /// True Monarch; born of a failed ascent, not necessarily weak (Heaven had recognised the essence); born of the old
    /// paths without position, far lesser. Read by name from the data: never rename a member.
    /// </summary>
    public enum DemonTier { Lesser, Ascent, Realization }

    /// <summary>A demon of the clan: awaiting the clan's choice, or let be and ravaging until <see cref="UntilYear"/>.</summary>
    public sealed record MetalEssenceDemon(string Id, string Name, DemonTier Tier, int Year, int UntilYear);

    /// <summary>What a demon let be does each year, by its rank: how long it ravages, whom it kills, what it ruins.</summary>
    public sealed record DemonTierSettings
    {
        public int RavageYears { get; init; }
        public double KillChance { get; init; }
        public int StonesLost { get; init; }
        public int PowerLoss { get; init; } // each power of the region
    }

    /// <summary>
    /// The demons (balance.json « demons », L4e, user decisions 2026-10-03 — interpretations): their ravages by rank, and
    /// the years the Underworld's grudge lasts once the clan takes an essence against the custom.
    /// </summary>
    public sealed record DemonSettings
    {
        public Dictionary<DemonTier, DemonTierSettings> Tiers { get; init; } = new Dictionary<DemonTier, DemonTierSettings>();
        public int GrudgeYears { get; init; } = 50;
    }
}
