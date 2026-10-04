namespace MirrorChronicles.Data
{
    /// <summary>
    /// A reborn True Monarch bends lesser minds (balance.json « enthrallment », audit §1.8, the user's decision 2026-10-03;
    /// LORE.md §5.4: a Purple Mansion resists the mind of a True Monarch's reincarnation). Interpretations: each year, by
    /// <see cref="YearlyChance"/>, a power's returned True Monarch able to reach the clan bends a member below the Purple
    /// Mansion into its unwitting spy; the mirror, having sounded it, breaks the spell for <see cref="BreakMirrorCost"/>;
    /// rising to the Purple Mansion frees it. The clan's own returned ancestor bends a power's lesser elder at its odds
    /// (<see cref="BendBase"/> plus <see cref="BendPerPower"/> a point of power over the power's guard); seen by
    /// <see cref="SeenChance"/>, the power distrusts the clan by <see cref="SeenDistrust"/>.
    /// </summary>
    public sealed record EnthrallmentSettings
    {
        public double YearlyChance { get; init; } = 0.1;
        public int BreakMirrorCost { get; init; } = 30;
        public double BendBase { get; init; } = 0.5;
        public double BendPerPower { get; init; } = 0.02;
        public double SeenChance { get; init; } = 0.3;
        public int SeenDistrust { get; init; } = 30;
    }
}
