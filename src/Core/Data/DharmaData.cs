namespace MirrorChronicles.Data
{
    /// <summary>A Rank Designation (LORE.md §6.9): a Dharma Treasure mortgaged on its Fruition; its first master, and since when.</summary>
    public sealed record RankDesignation(string Id, string Lineage, string MasterId, string MasterName, int Year);

    /// <summary>
    /// The powers' treasures and artifacts (balance.json « worldArsenal », the user's rule 2026-10-03 — interpretations):
    /// a year's odds a True Monarch condenses its treasure, a holder mortgages it, a masterless Designation strikes or is
    /// unsealed, and a power forges an artifact (a sect, a gate or a kingdom; a family less often).
    /// </summary>
    public sealed record WorldArsenalSettings
    {
        public double ElderCondenseChance { get; init; } = 0.1;
        public double ElderDesignationChance { get; init; } = 0.05;
        public double MasterlessStrikeChance { get; init; } = 0.05;
        public double UnsealChance { get; init; } = 0.2;
        public double ForgeChance { get; init; } = 0.15;
        public double FamilyForgeChance { get; init; } = 0.05;
        public double TreasureFindChance { get; init; } = 0.01;  // a power of the Purple Mansion finds a Spiritual Treasure (its tombs, its ruins)
        public double BondChance { get; init; } = 0.02;          // a peak Foundation binds itself to its power's Spiritual Treasure
        public double TransmuteChance { get; init; } = 0.001;    // a holder tries a Transmutation to a free Realization of its Virtue
    }

    /// <summary>
    /// The Dharma Treasures and the Rank Designations (balance.json « dharma », L4e, user decisions 2026-10-03 —
    /// interpretations): a treasure's price and years, its war strength and its guard of its bearer; a Designation's
    /// strength and its guard of the domain (halved when its master is not loved by the Fruition, or is gone); the yearly
    /// chance a masterless one strikes the clan.
    /// </summary>
    public sealed record DharmaSettings
    {
        public int CondenseOres { get; init; } = 300;
        public int CondenseYears { get; init; } = 5;
        public double TreasureStrength { get; init; } = 10;
        public double TreasureGuardChance { get; init; } = 0.5;
        public double DesignationStrength { get; init; } = 20;
        public double DesignationGuardChance { get; init; } = 0.3;
        public double MasterlessStrikeChance { get; init; } = 0.05;
    }
}
