namespace MirrorChronicles.Data
{
    /// <summary>
    /// Keeping the cultivating line (balance.json « lineage »; user decision 2026-09-29; interpretations): what seeking a
    /// cultivator spouse abroad costs, and its chance — better for a member of higher realm.
    /// </summary>
    public sealed record LineageSettings
    {
        public int SeekStones { get; init; }
        public double SeekChance { get; init; }
        public double SeekChancePerRealm { get; init; }
    }
}
