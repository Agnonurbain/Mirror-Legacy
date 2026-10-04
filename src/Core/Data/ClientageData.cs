namespace MirrorChronicles.Data
{
    /// <summary>
    /// What the clientage costs (balance.json « clientage », AUDIT_LORE.md §4.1, §5.1 — 🔎 the amounts): a share of the stones;
    /// each year, at <see cref="SelectionChance"/>, the most gifted child born with an orifice is taken as the sect's disciple
    /// for <see cref="DiscipleYears"/>; at <see cref="LevyChance"/> a cultivator serves a year at the frontier, dying there at
    /// <see cref="LevyDeathChance"/>; a seeded cultivator in the open gives the sect <see cref="SeededClues"/> clues at
    /// <see cref="SeededClueChance"/>.
    /// </summary>
    public sealed record ClientageSettings
    {
        public double TributeShare { get; init; } = 0.03;
        public double SelectionChance { get; init; } = 0.2;
        public int SelectionMinAge { get; init; } = 6;
        public int SelectionMaxAge { get; init; } = 16;
        public int DiscipleYears { get; init; } = 30;
        public double LevyChance { get; init; } = 0.15;
        public double LevyDeathChance { get; init; } = 0.1;
        public double SeededClueChance { get; init; } = 0.1;
        public int SeededClues { get; init; } = 5;
    }
}
