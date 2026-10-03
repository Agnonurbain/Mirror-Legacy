using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A cultivator's fertility (balance.json « fertility », user decision 2026-10-03 — interpretations): the motherhood
    /// window lengthens with the mother's realm (the last age by realm; a mortal's is maxMotherAge), and the higher a
    /// parent's realm, the rarer a child each year (a factor on the yearly chance, by the higher realm of the two).
    /// </summary>
    public sealed record FertilitySettings
    {
        public Dictionary<CultivationRealm, int> LastMotherAge { get; init; } = new Dictionary<CultivationRealm, int>();
        public Dictionary<CultivationRealm, double> BirthFactor { get; init; } = new Dictionary<CultivationRealm, double>();
    }
}
