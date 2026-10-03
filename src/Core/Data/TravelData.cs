using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// How far a cultivator goes (balance.json « travel », AUDIT_LORE.md §1.5, 2026-10-03). Lore: a Qi Cultivator rides the
    /// wind and walks on air (📚 some two hundred li in a few hours); a Purple Mansion crosses the Great Void and is
    /// everywhere at once (LORE.md §5.2, §5.4, §5.8). Interpretations: below <see cref="WholeStateFrom"/>, one goes at most
    /// <see cref="HopsByRealm"/> regions from home, within its state; from it, anywhere in its state; from
    /// <see cref="AnywhereFrom"/>, anywhere in the world. A team goes as far as its strongest carries it.
    /// </summary>
    public sealed record TravelSettings
    {
        public IReadOnlyDictionary<CultivationRealm, int> HopsByRealm { get; init; } =
            new Dictionary<CultivationRealm, int> { [CultivationRealm.Embryonic] = 0, [CultivationRealm.QiRefinement] = 2 };
        public CultivationRealm WholeStateFrom { get; init; } = CultivationRealm.Foundation;
        public CultivationRealm AnywhereFrom { get; init; } = CultivationRealm.PurpleMansion;
    }
}
