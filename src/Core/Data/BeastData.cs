using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A kind of spirit beast (beasts.json; L2c.2). The lore names few beasts: the species are descriptive
    /// interpretations, each living in some kinds of places.
    /// </summary>
    public sealed class BeastSpecies
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public Element Element { get; init; }
        public IReadOnlyList<RegionKind> Habitats { get; init; } = Array.Empty<RegionKind>();
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// A spirit beast living in the world (drawn from the world's seed, saved): where it lives, how strong it is,
    /// and the power it belongs to — null for a solitary one (user decision, 2026-09-26).
    /// </summary>
    public sealed record WorldBeast(string Id, string SpeciesId, string RegionId, CultivationRealm Realm, int Stage, string OwnerFaction);

    /// <summary>How the world's beasts are drawn and found (balance.json, L2c.2; interpretations).</summary>
    public sealed record BestiarySettings
    {
        /// <summary>Beasts drawn on each place by its kind (1 for a kind not listed).</summary>
        public IReadOnlyDictionary<RegionKind, int> BeastsByKind { get; init; } = new Dictionary<RegionKind, int>();

        /// <summary>Where powers live, the share of beasts that belong to one of them (« most do »).</summary>
        public double OwnedShare { get; init; }

        /// <summary>The odds of a beast's realm: Qi Cultivation, Foundation, Purple Mansion (summing to 1).</summary>
        public IReadOnlyList<double> RealmOdds { get; init; } = Array.Empty<double>();

        /// <summary>Chance a year of scouting reveals a beast of the hunting ground.</summary>
        public double ScoutRevealChance { get; init; }
    }
}
