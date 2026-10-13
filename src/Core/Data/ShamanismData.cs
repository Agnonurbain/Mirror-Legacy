namespace MirrorChronicles.Data
{
    /// <summary>A shaman's method (R17; LORE.md §3.5): Transformation (Threshold Owl), Concealment (First Jade), Sacrifice (Grand Shaman).</summary>
    public enum ShamanMethod { None, Transformation, Concealment, Sacrifice }

    /// <summary>
    /// The Shamanic Talisman Dao (balance.json « shamanism »; LORE.md §3.5, R17; the user's choices of 2026-10-13): a member
    /// whose foundation is of the Grand Shaman's lineage may pass to the shamanic Dao keeping its realm (back to its first
    /// stage, its abilities turned talismans) — at any time, or as a refuge when its Golden Core fails, escaping the Emissaries
    /// of Death. It walks one method: Transformation (it weighs a realm higher), Concealment (the powers' ambushes, captures and
    /// Dao hunts no longer find it), or Sacrifice (it burns blood essence to cultivate far faster). At the Purple Mansion's
    /// Grand Perfection a rite of blood may give it a shamanic Golden Core.
    /// </summary>
    public sealed class ShamanismSettings
    {
        /// <summary>The Fruition whose foundation opens the way.</summary>
        public string GateFruition { get; init; }

        /// <summary>The realms a Transformation shaman weighs above its own.</summary>
        public int TransformLeap { get; init; }

        /// <summary>The share of a human's yearly cultivation a shaman gains; a Sacrifice shaman's blood each year and its multiplier.</summary>
        public double Speed { get; init; }
        public int SacrificeBlood { get; init; }
        public double SacrificeSpeed { get; init; }

        /// <summary>The blood and the chance of the shamanic Golden Core's rite.</summary>
        public int CoreBlood { get; init; }
        public double CoreChance { get; init; }
    }
}
