using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// Who knows the mirror exists (balance.json « mirrorLore », user decision 2026-09-27; figures are interpretations):
    /// only beings of the realms listed, each with a chance growing with their age — the base, plus so much per century
    /// — up to a ceiling. An unnamed elder of a power is taken to be this old. An ordinary power whose suspicion is
    /// complete can only suspect a treasure: its suspicion of the clan grows, and each year it may sell the rumour to the
    /// strongest power where someone knows.
    /// </summary>
    public sealed record MirrorLoreSettings
    {
        public Dictionary<CultivationRealm, double> KnowChance { get; init; } = new Dictionary<CultivationRealm, double>();
        public Dictionary<CultivationRealm, double> PerCentury { get; init; } = new Dictionary<CultivationRealm, double>();
        public double MaxChance { get; init; } = 1;
        public int UnknownAge { get; init; }
        public double RumourRiseChance { get; init; }
        public int TreasureSuspicion { get; init; }
    }
}
