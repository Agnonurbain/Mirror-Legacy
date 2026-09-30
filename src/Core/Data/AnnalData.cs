namespace MirrorChronicles.Data
{
    /// <summary>The kinds of milestone the clan's Annals keep (LORE.md §11.9). Saved by name: never rename a member.</summary>
    public enum AnnalKind
    {
        RealmReached,   // Value: the realm first reached by a member of the clan
        PositionTaken,  // Value: the Golden Core position (GoldenCoreState) first taken
        Generation,     // Value: the generation that begins
        EndingReached,  // Ref: the dynastic ending (endings.json id); Subject: who reached it, or none for the clan as a whole
        ShardRecovered, // Ref: the shard (shards.json id); Value: the shards restored so far
        SectFounded     // the clan founds its sect (B3d)
    }

    /// <summary>A milestone of the clan's Annals: what, when, who, and what it refers to (saved since 2.21).</summary>
    public sealed record AnnalEntry(AnnalKind Kind, int Year, int Value, string Subject, string Ref = null);
}
