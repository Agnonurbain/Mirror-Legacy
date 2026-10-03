namespace MirrorChronicles.Data
{
    /// <summary>A Rank Designation (LORE.md §6.9): a Dharma Treasure mortgaged on its Fruition; its first master, and since when.</summary>
    public sealed record RankDesignation(string Id, string Lineage, string MasterId, string MasterName, int Year);

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
