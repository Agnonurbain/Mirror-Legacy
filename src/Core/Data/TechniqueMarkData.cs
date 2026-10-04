namespace MirrorChronicles.Data
{
    /// <summary>
    /// The mark an art keeps of the power it was taken from (balance.json « techniqueMarks », AUDIT_LORE.md §3.9, 2026-10-04;
    /// 📚 the mirror scrubs a sect's arts of their « personal flairs » so the clan uses them without exposing their origin).
    /// Interpretations: each year, by <see cref="RecognizeChance"/>, a power recognizes its style in an art stolen from it and
    /// gains <see cref="Evidence"/> proof; the mirror cleanses the mark for <see cref="CleanseCost"/> Moonlight.
    /// </summary>
    public sealed record TechniqueMarkSettings
    {
        public double RecognizeChance { get; init; } = 0.1;
        public int Evidence { get; init; } = 10;
        public int CleanseCost { get; init; } = 40;
    }
}
