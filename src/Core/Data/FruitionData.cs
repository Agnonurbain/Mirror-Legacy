using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>The families of Dao lineages (LORE.md §6.4); a lineage may belong to several.</summary>
    public enum FruitionGroup
    {
        TwoDualities,   // the three Yang and the three Yin
        FiveVirtues,    // element × manifestation
        TwelveQi,       // in the order of the Qi Inversion Cycle
        ThreeThunders,
        AncientFusion,  // from Beyond the Profundities: the Three Shamans among them
        VoidAttestation // attested from nothing by a figure of the Dao Embryo or above
    }

    /// <summary>How a lineage came to be (LORE.md §6.2).</summary>
    public enum FruitionGeneration { Unknown, Founding, Interaction, AncientFusion, Attestation }

    /// <summary>The tradition of the Three Profundities a lineage belongs to (LORE.md §6.2), when known.</summary>
    public enum Profundity { None, Qingxuan, Douxuan, Tongxuan }

    /// <summary>The five manifestations of an element (LORE.md §6.3).</summary>
    public enum Manifestation { None, Orthodox, Gathered, Nourishing, Mutable, Hidden }

    /// <summary>What a divine ability does (LORE.md §5.4.4); some have two types.</summary>
    public enum AbilityType { Body, Life, Eye, Magic }

    /// <summary>
    /// The state of a lineage's position (LORE.md §6.8): held by a True Monarch, free to claim, broken
    /// (it cannot be reached until restored), hidden from the world, suspected — or left open by the lore
    /// and drawn at the start of each game.
    /// </summary>
    public enum FruitionStatus { Unspecified, Occupied, Free, Broken, Hidden, Suspected }

    /// <summary>
    /// A foundation / divine ability of a lineage. Its name is null while the world has not revealed it
    /// (P3: knowledge is a resource); its types may be unknown too.
    /// </summary>
    public sealed class DivineAbilityDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public IReadOnlyList<AbilityType> Types { get; init; } = Array.Empty<AbilityType>();

        /// <summary>A substitute ability (Surplus, Intercalary), not one of the orthodox five.</summary>
        public bool Substitute { get; init; }
    }

    /// <summary>A Dao lineage (Fruition, LORE.md §6): what its holder embodies, its abilities, its state.</summary>
    public sealed class FruitionDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
        public IReadOnlyList<FruitionGroup> Groups { get; init; } = Array.Empty<FruitionGroup>();
        public FruitionGeneration Generation { get; init; }
        public Profundity Profundity { get; init; }

        /// <summary>The element of a Five Virtues lineage (the combat element otherwise, None when none fits).</summary>
        public Element Element { get; init; }
        public Manifestation Manifestation { get; init; }

        /// <summary>Rank in the Qi Inversion Cycle for the Twelve Qi (1-12), 0 otherwise.</summary>
        public int CycleIndex { get; init; }
        public bool IsShaman { get; init; }
        public bool IsRite { get; init; }

        /// <summary>What its holder's golden core becomes; null when unknown.</summary>
        public string MetalEssence { get; init; }
        public IReadOnlyList<DivineAbilityDefinition> Abilities { get; init; } = Array.Empty<DivineAbilityDefinition>();

        public FruitionStatus Status { get; init; }
        public string Holder { get; init; }
        public IReadOnlyList<string> FormerHolders { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Surplus { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Intercalary { get; init; } = Array.Empty<string>();
        public string Star { get; init; }
        public string Notes { get; init; }
    }

    /// <summary>fruitions.json: the lineages and the name given to a True Monarch the lore does not name.</summary>
    public sealed class FruitionCatalog
    {
        public string AnonymousHolder { get; init; }
        public IReadOnlyList<FruitionDefinition> Fruitions { get; init; } = Array.Empty<FruitionDefinition>();
    }

    /// <summary>How the statuses the lore leaves open are drawn at the start of a game (balance.json).</summary>
    public sealed class FruitionOdds
    {
        public double Free { get; init; }
        public double Occupied { get; init; }
        public double Broken { get; init; }
    }

    /// <summary>A lineage's state in one game.</summary>
    public sealed record FruitionState(FruitionStatus Status, string Holder);

    /// <summary>A foundation named as « fruition-id:ability-id » (qi.json).</summary>
    public static class FoundationRef
    {
        public static (string FruitionId, string AbilityId) Parse(string reference)
        {
            int colon = reference?.IndexOf(':') ?? -1;
            return colon <= 0 ? (null, null) : (reference.Substring(0, colon), reference.Substring(colon + 1));
        }
    }
}
