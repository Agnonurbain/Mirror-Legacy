using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Ancestor's Return (balance.json « ancestors », LORE.md §5.5.2, R9; user decisions 2026-10-01 and 2026-10-03 —
    /// interpretations). A True Monarch whose essence is intact is reborn by <see cref="RebirthChance"/> in the clan's next
    /// child — 1: « autant de fois qu'il le souhaite », only the lore's risks stand in the way (a demon, a soul taken back,
    /// a harvested Chosen, the Underworld's grudge); the powers' True Monarchs alike; it regains the Foundation <see cref="FoundationYears"/> after the age of cultivation, the Purple Mansion
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
