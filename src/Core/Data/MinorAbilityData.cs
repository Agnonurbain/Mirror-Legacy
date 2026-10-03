namespace MirrorChronicles.Data
{
    /// <summary>
    /// The minor abilities of former True Monarchs (balance.json « minorAbilities », R2; L4e, user decisions 2026-10-03 —
    /// interpretations): the odds a tomb or ruins reveal one, the mirror's price to deduce those of a well-known lineage,
    /// and what a power holding the lineage asks to teach one (in kind).
    /// </summary>
    public sealed record MinorAbilitySettings
    {
        public double TombRevealChance { get; init; } = 0.5;
        public double RuinsRevealChance { get; init; } = 0.2;
        public int MirrorCost { get; init; } = 40;
        public int Worth { get; init; } = 6000; // what it is worth in kind: never sold for stones
        public int BuyRelation { get; init; } = 20;
    }
}
