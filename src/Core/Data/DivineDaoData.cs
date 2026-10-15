namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Divine Dao (balance.json « divineDao »; LORE.md §3.6, R15; the wiki « Divine Dao »; the user's choices of
    /// 2026-10-13): « Profundity precedes power ». A cultivator from the Qi Cultivation to the Purple Mansion binds itself to
    /// what already holds Dao authority and becomes a Divine Attendant: it borrows a great partner's might (explosive, but it
    /// falls with the pact), unites with the clan's domain (undying while it stands, but bound to it and capped at the Purple
    /// Mansion), or owns a Profundity, refining it by rituals that spend the clan's standing toward a Divine Core.
    /// </summary>
    public sealed class DivineDaoSettings
    {
        /// <summary>The realms a borrower weighs above its own while its great partner's pact holds.</summary>
        public int BorrowLeap { get; init; }

        /// <summary>The share of a human's yearly cultivation a united or owning Attendant gains, cultivating.</summary>
        public double Speed { get; init; }

        /// <summary>The prestige an owner's rituals spend each year it cultivates.</summary>
        public int OwnPrestigePerYear { get; init; }

        /// <summary>The prestige and the chance to condense a Divine Core at the Purple Mansion's Grand Perfection.</summary>
        public int CorePrestige { get; init; }
        public double CoreChance { get; init; }

        /// <summary>
        /// The haunting (L7; the user's choice of 2026-10-15): the years a dead Attendant haunts the clan for each degree of its
        /// hold on a Profundity (borrowing or uniting 1, owning 2, a Divine Core 3), and the yearly odds, by degree, that it
        /// drives a member mad or brings a calamity costing a share of the clan's stones.
        /// </summary>
        public int HauntYearsPerStrength { get; init; }
        public double MadnessChancePerStrength { get; init; }
        public double CalamityChancePerStrength { get; init; }
        public double CalamityStoneShare { get; init; }

        /// <summary>Honoured, it lends prestige each year by degree, and commemoration multiplies its dangers.</summary>
        public int HonourPrestigePerStrength { get; init; }
        public double HonourRiskFactor { get; init; }

        /// <summary>The stones of an exorcism by degree, and their share when a monk of the clan performs it.</summary>
        public int ExorcismStonesPerStrength { get; init; }
        public double MonkExorcismFactor { get; init; }
    }

    /// <summary>
    /// A dead Divine Attendant haunting the clan (L7): its strength (its hold on a Profundity), the years left, and whether the
    /// clan honours it. Saved (2.55).
    /// </summary>
    public sealed class Haunting
    {
        public string Id { get; set; }          // the dead Attendant's ID
        public string Name { get; set; }
        public int Strength { get; set; }
        public int YearsLeft { get; set; }
        public bool Honoured { get; set; }
    }
}
