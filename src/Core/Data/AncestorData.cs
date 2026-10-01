using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Ancestor's Return (balance.json « ancestors », LORE.md §5.5.2, R9; user decision 2026-10-01 — interpretations).
    /// A True Monarch of the clan whose essence is intact is reborn by <see cref="RebirthChance"/> in the clan's next
    /// child; it regains the Foundation <see cref="FoundationYears"/> after the age of cultivation, the Purple Mansion
    /// <see cref="PurpleMansionYears"/> later, the Golden Core <see cref="GoldenCoreYears"/> later still. Until the Purple
    /// Mansion, a power harvests the Chosen of Destiny each year by <see cref="HarvestChance"/>, unless it is in seclusion.
    /// </summary>
    public sealed record AncestorSettings
    {
        public double RebirthChance { get; init; }
        public double HarvestChance { get; init; }
        public int FoundationYears { get; init; } = 2;
        public int PurpleMansionYears { get; init; } = 10;
        public int GoldenCoreYears { get; init; } = 10;
    }

    /// <summary>
    /// What a True Monarch's intact essence carries into its next life: its name, its divine abilities (its foundation
    /// first), the Realization it held, its path and method.
    /// </summary>
    public sealed record AncestorEssence(string Name, IReadOnlyList<string> Abilities, string FruitionId, GoldenCoreState Position,
        string MethodId, string QiId);
}
