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
    }
}
