using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>A talisman Qi's rank, set by the sacrifice's realm (LORE.md §11.5): grey for the Qi Cultivation, white for the Foundation.</summary>
    public enum TalismanRank { Grey, White }

    /// <summary>What a talisman Qi grants besides figures the game already measures (for combat and the arts, later).</summary>
    public enum TalismanTrait
    {
        Premonition, Strength, Longevity, Flight, Agility, Arts, QiPool, QiAbsorption, IceMastery,
        CalmHeart, FireRefinement, SpellInsight, OffspringTalent, FoundationStrength, CostOfLife
    }

    /// <summary>
    /// A talisman Qi the mirror can refine (talismans.json; names kept descriptive, D6). The figures the lore or
    /// the wiki give are kept; the others are interpretations named in InterpretedFields.
    /// </summary>
    public sealed class TalismanDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public TalismanRank Rank { get; init; }

        /// <summary>The tempers it suits: the mirror offers what fits the bearer's personality first.</summary>
        public IReadOnlyList<Temperament> Temperaments { get; init; } = Array.Empty<Temperament>();

        /// <summary>Years of life it adds.</summary>
        public int LifespanYears { get; init; }

        /// <summary>Multiplies the bearer's cultivation speed (1: none).</summary>
        public double CultivationSpeed { get; init; } = 1.0;

        /// <summary>Percent added to the Illusions trial of the Purple Mansion (a heart still as water).</summary>
        public int IllusionsBonus { get; init; }

        /// <summary>Points of spiritual root the bearer's children are born with besides.</summary>
        public int OffspringRootBonus { get; init; }

        public IReadOnlyList<TalismanTrait> Traits { get; init; } = Array.Empty<TalismanTrait>();
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>The ritual of the talisman Qi (balance.json; the ten thousand prayers are the lore's).</summary>
    public sealed record TalismanSettings
    {
        public int PrayersPerRitual { get; init; }

        /// <summary>Prayers the clan gathers each year: from each mortal member, and per point of prestige.</summary>
        public int PrayersPerMortalPerYear { get; init; }
        public int PrayersPerPrestigePerYear { get; init; }

        /// <summary>Spiritual roots from which the mirror offers two, then three talismans (one below the first).</summary>
        public IReadOnlyList<int> OfferRootThresholds { get; init; } = Array.Empty<int>();

        /// <summary>Sub-levels a new talisman Qi lifts its bearer, within their realm.</summary>
        public int GreyStageLeap { get; init; }
        public int WhiteStageLeap { get; init; }
    }

    /// <summary>The talismans the mirror offers a bearer after a ritual, awaiting the player's choice (saved).</summary>
    public sealed record TalismanOffer(string BeneficiaryId, List<string> Choices);
}
