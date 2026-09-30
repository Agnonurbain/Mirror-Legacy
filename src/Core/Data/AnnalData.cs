namespace MirrorChronicles.Data
{
    /// <summary>The kinds of milestone the clan's Annals keep (LORE.md §11.9). Saved by name: never rename a member.</summary>
    public enum AnnalKind
    {
        RealmReached,   // Value: the realm first reached by a member of the clan
        PositionTaken,  // Value: the Golden Core position (GoldenCoreState) first taken
        Generation      // Value: the generation that begins
    }

    /// <summary>A milestone of the clan's Annals: what, when, and who (saved since 2.21).</summary>
    public sealed record AnnalEntry(AnnalKind Kind, int Year, int Value, string Subject);
}
