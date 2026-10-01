using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The powers' elders (balance.json « elders », the living world, step A, 2026-10-01 — interpretations: the lore gives
    /// no such rates). How many cadets each kind of power keeps beside its strongest; the yearly chance a cadet rises one
    /// realm (Embryonic, Qi Cultivation, Foundation) once it has spent its years there; the chance a power raises a new
    /// cadet when below its ranks; the years at the Purple Mansion before its peak, and the range of its own odds of the
    /// Golden Core, which grow as it prepares at its peak; a precious Purple Mansion dares only from <see cref="RiseOdds"/> or in
    /// its <see cref="LastYears"/>. At the world's start no elder holds such odds: it would have risen long before.
    /// </summary>
    public sealed record ElderSettings
    {
        public Dictionary<FactionKind, int> CadetsByKind { get; init; } = new Dictionary<FactionKind, int>();
        public double[] RiseChance { get; init; } = { 0.05, 0.03, 0.01 };
        public int[] MinYearsInRealm { get; init; } = { 10, 30, 60, 150 };
        public double NewElderChance { get; init; }
        public double MinGoldenCoreOdds { get; init; }
        public double MaxGoldenCoreOdds { get; init; }
        public double RiseOdds { get; init; } = 0.6;
        public double OddsGainPerYear { get; init; }   // a Purple Mansion at its peak prepares: its odds grow a little each year
        public int LastYears { get; init; } = 20;
    }
}
