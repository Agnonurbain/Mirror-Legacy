using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A power's elder held by another power, for a ransom.</summary>
    public sealed record PowerCaptive(string Captor, string Victim, FactionElder Elder, int Year);

    /// <summary>
    /// The powers' schemes against each other (balance.json « powerSchemes », the user's rule 2026-10-03 — interpretations):
    /// a year's odds a power schemes (by its personality), how it chooses a theft, an ambush or a harvest, what a theft takes,
    /// the odds it is caught, a captive's ransom and the years before it is put to death, what a harvest gives.
    /// </summary>
    public sealed record PowerSchemeSettings
    {
        public double SchemeChance { get; init; } = 0.03;
        public Dictionary<FactionPersonality, double> PersonalityFactor { get; init; } = new Dictionary<FactionPersonality, double>();
        public double TheftWeight { get; init; } = 0.5;
        public double AmbushWeight { get; init; } = 0.35;
        public double TheftWealthShare { get; init; } = 0.1;
        public double CaughtChance { get; init; } = 0.3;
        public int RansomPerRealm { get; init; } = 500;
        public int CaptiveYears { get; init; } = 3;
        public int HarvestPowerGain { get; init; } = 20;
    }
}
