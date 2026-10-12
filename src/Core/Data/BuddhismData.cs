namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Buddhist conversion and its two schools (balance.json « buddhism »; LORE.md §3.3, §3.7, R13-R14; the user's choices
    /// of 2026-10-12): a dying cultivator may convert at a temple to escape death, giving up its immortal way — a realm lower,
    /// its foundation, abilities and method lost. It then cultivates its nature and destiny without trial or method, at its
    /// temple's school's pace: the ancient school slow and stable up to the Dharma Master's apogee; the modern one faster but
    /// capped at the Dharma Master's beginning, its soul held by the temple, which may call it back.
    /// </summary>
    public sealed class BuddhismSettings
    {
        /// <summary>A cultivator with this many years left or fewer may convert.</summary>
        public int DyingYears { get; init; }

        /// <summary>The years the conversion gives beyond the convert's age.</summary>
        public int YearsGained { get; init; }

        /// <summary>The offering to the temple, in spirit stones.</summary>
        public int OfferingStones { get; init; }

        /// <summary>The temple's warmth after a conversion.</summary>
        public int ConversionRelation { get; init; }

        /// <summary>The share of a human's yearly cultivation each school gains, cultivating.</summary>
        public double AncientSpeed { get; init; }
        public double ModernSpeed { get; init; }

        /// <summary>The highest Golden Core stage each school reaches (the Dharma Master).</summary>
        public int AncientCeilingStage { get; init; }
        public int ModernCeilingStage { get; init; }

        /// <summary>Below this relation, a modern temple may call back a soul it holds — a chance each year.</summary>
        public int RecallBelow { get; init; }
        public double RecallChance { get; init; }
    }
}
