namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Demonic Dao of the clan's bound beasts (balance.json « demonicDao »; LORE.md §3.4, R16; the user's choices of
    /// 2026-10-12): a captured beast bound to the clan rather than offered to the mirror absorbs the Qi around it every year,
    /// slower than a human cultivates but for a far longer life, and its lineage awakens its realms on its own — up to the
    /// Purple Mansion for now.
    /// </summary>
    public sealed class DemonicDaoSettings
    {
        /// <summary>The share of a human's yearly cultivation a bound beast absorbs, whatever its task.</summary>
        public double AbsorptionSpeed { get; init; }

        /// <summary>A beast's lifespan against a human's of the same realm.</summary>
        public double LifespanFactor { get; init; }

        /// <summary>The age of a bound beast, drawn between these bounds.</summary>
        public int MinBoundAge { get; init; }
        public int MaxBoundAge { get; init; }

        /// <summary>Its spiritual root, drawn between these bounds.</summary>
        public int MinRoot { get; init; }
        public int MaxRoot { get; init; }
    }
}
