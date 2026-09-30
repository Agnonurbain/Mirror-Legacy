namespace MirrorChronicles.Data
{
    /// <summary>A power that learnt a member of the clan carries a ripe Dao (saved; hidden from the player).</summary>
    public sealed record DaoPrey(string Faction, string MemberId);

    /// <summary>
    /// The hunt of a ripe Dao as a plot (balance.json « daoHunts »; LORE.md §5.3.3; user decision 2026-09-29;
    /// interpretations): from what realm a power hunts, how often it learns a Dao is ripe — more when the member is away,
    /// hardly in seclusion — how often one who knows strikes, and how far each of the clan's defences lowers the blow
    /// (patrols, each level of the Protective Formation, a defensive ally, seclusion; a Purple Mansion guardian is
    /// <see cref="BalanceSettings.RipeDaoGuardedFactor"/>).
    /// </summary>
    public sealed record DaoHuntSettings
    {
        public CultivationRealm HunterMinRealm { get; init; } = CultivationRealm.PurpleMansion;
        public double CovetChance { get; init; }   // the chance a hunter covets a given lineage, drawn with the world (2026-09-30)
        public double LearnChance { get; init; }
        public double AwayExposure { get; init; } = 1;
        public double SecludedExposure { get; init; } = 1;
        public double StrikeChance { get; init; }
        public double StrikeSuccess { get; init; }
        public double PatrolGuard { get; init; }
        public double FormationGuard { get; init; }
        public double AllyGuard { get; init; } = 1;
        public double SecludedGuard { get; init; } = 1;
    }
}
