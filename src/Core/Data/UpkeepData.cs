namespace MirrorChronicles.Data
{
    /// <summary>
    /// The clan's upkeep (balance.json « upkeep »; user decision 2026-09-29, after the long games; interpretations): what a
    /// member costs each year — a mortal, a cultivator, more by realm — what a poor year does (children few, members
    /// shaken), how many years of upkeep in reserve a clan needs to have all its children, and the veins of the mine: how
    /// many miners work them fully, how many more each level of the Mine building opens, and what share a miner beyond
    /// them still yields.
    /// </summary>
    public sealed record UpkeepSettings
    {
        public int MortalStones { get; init; }
        public int CultivatorStones { get; init; }
        public int StonesPerRealm { get; init; }
        public double PovertyBirthFactor { get; init; }
        public int ProsperityYears { get; init; } = 1;
        public int PovertyStability { get; init; }
        public int VeinMiners { get; init; }
        public int VeinMinersPerMineLevel { get; init; }
        public double ExtraMinerShare { get; init; }

        // The domain's materials (user decision 2026-10-01, interpretation): nothing produced them before
        public int OresPerVeinMiner { get; init; }     // spiritual ores each miner of the veins brings up in a year
        public int OresPerMineLevel { get; init; }     // and the Mine building's own, by level
        public int HerbsPerGardenLevel { get; init; }  // medicinal herbs the Herb Garden grows each year, by level
    }
}
