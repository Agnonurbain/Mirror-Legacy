using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>What a place of the map is (LORE.md §7).</summary>
    public enum RegionKind { State, Sea, Prefecture, Mountain, Lake, River, Plain, Wilds, Desert, Island }

    /// <summary>
    /// A place of the world map (regions.json, LORE.md §7): a state or sea (no parent), or a region inside one,
    /// placed by its relative position (x west→east, y north→south, 0-1) with the places it borders. The lore
    /// gives relative geography only: positions and neighbours are interpretations until a map fixes them.
    /// </summary>
    public sealed class RegionDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public RegionKind Kind { get; init; }

        /// <summary>The state or sea the place belongs to; null for a state or sea.</summary>
        public string ParentId { get; init; }

        public double X { get; init; }
        public double Y { get; init; }
        public IReadOnlyList<string> Neighbours { get; init; } = Array.Empty<string>();
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }

        /// <summary>Fields filled by interpretation, to replace when a source speaks.</summary>
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }
}
