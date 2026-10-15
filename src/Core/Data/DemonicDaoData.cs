using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Demonic Dao of the clan's bound beasts (balance.json « demonicDao »; LORE.md §3.4, R16; the user's choices of
    /// 2026-10-12): a captured beast bound to the clan rather than offered to the mirror absorbs the Qi around it every year,
    /// slower than a human cultivates but for a far longer life, and its lineage awakens its realms on its own — up to the
    /// Purple Mansion's Grand Perfection, where it may awaken a demonic Golden Core — on its own, rarely, or fed by the clan's
    /// offering (the user's choice of 2026-10-15). A serpent of the dragon's line may carry a hidden dragon blood, revealed at an
    /// awakening: it walks the True Serpent-Dragon Dao, and the Eastern Sea's dragons come to reclaim it (LORE.md §3.4, §12.1).
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

        /// <summary>At the Purple Mansion's Grand Perfection: the yearly chance its lineage awakens a demonic core on its own.</summary>
        public double NaturalCoreChance { get; init; }

        /// <summary>The clan's offering of spirit stones to feed the awakening, and its chance (a failure wounds its Dao).</summary>
        public int OfferingStones { get; init; }
        public double OfferingCoreChance { get; init; }

        /// <summary>The species of the dragon's line, and the chance one bound carries the dragon blood, hidden.</summary>
        public List<string> DragonBloodSpecies { get; init; } = new List<string>();
        public double DragonBloodChance { get; init; }

        /// <summary>A revealed dragon's absorption multiplier, and the odds it adds to both ways to its core.</summary>
        public double DragonSpeed { get; init; }
        public double DragonCoreBonus { get; init; }

        /// <summary>The yearly chance the Eastern Sea's dragons come to reclaim a revealed dragon of the clan.</summary>
        public double DragonReclaimChance { get; init; }
    }
}
