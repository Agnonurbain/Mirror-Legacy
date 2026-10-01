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
        public double OddsGainPerYear { get; init; }
        public double PerfectionChance { get; init; } // a Purple Mansion ever gathers its five abilities (most never do, as with the clan)   // a Purple Mansion at its peak prepares: its odds grow a little each year
        public int LastYears { get; init; } = 20;
    }

    /// <summary>
    /// The powers' economy and growth (balance.json « powerEconomy », the living world, step B, 2026-10-01 — interpretations).
    /// A yearly income and upkeep by size; the size tends toward what the elders can lead, weighed by their realms against
    /// the world as drawn; a temper speeds the income or the growth; a power in debt loses disciples.
    /// </summary>
    public sealed record PowerEconomySettings
    {
        public double IncomePerPower { get; init; }
        public double UpkeepPerPower { get; init; }
        public Dictionary<FactionPersonality, double> TemperIncome { get; init; } = new Dictionary<FactionPersonality, double>();
        public double GrowthRate { get; init; }
        public Dictionary<FactionPersonality, double> TemperGrowth { get; init; } = new Dictionary<FactionPersonality, double>();
        public double[] RealmWeight { get; init; } = { 1, 1, 3, 10, 40, 100, 200 };
        public double DebtDecline { get; init; }
    }

    /// <summary>
    /// The powers' births and falls (balance.json « powerLifecycle », the living world, step C, user decisions 2026-10-01 —
    /// interpretations). A power under <see cref="FallPower"/> or without elders disperses. A founding is strict, and harder
    /// at each rank: a gate asks a Purple Mansion founder and its followers, a sect a Golden Core and more, a kingdom — the
    /// hardest — a power with a Golden Core, vassals and the size of the greatest. A family rises while the world counts
    /// fewer powers than at first.
    /// </summary>
    public sealed record PowerLifecycleSettings
    {
        public int FallPower { get; init; }
        public double SecessionChance { get; init; }
        public int GateFollowers { get; init; } = 2;
        public int MaxExtraPowers { get; init; } = 3;  // no founding beyond the world's first count and these
        public int SectFollowers { get; init; } = 4;
        public double SecessionPowerShare { get; init; }
        public double SecessionWealthShare { get; init; }
        public double KingdomChance { get; init; }
        public int KingdomVassals { get; init; } = 3;
        public double KingdomPowerShare { get; init; } = 0.5; // its size against the greatest power's
        public double RiseChance { get; init; }
        public int RisenPower { get; init; }
        public int RisenWealth { get; init; }
    }
}
