using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>When a patron's design falls due (LORE.md §11.10, C).</summary>
    public enum DesignDue { PurpleMansion, GoldenCore, Years, HeirBorn }

    /// <summary>
    /// A patron's hidden design (designs.json; LORE.md §11.10, C; the user's decisions of 2026-09-30): what a power that offers
    /// an ascent method truly wants, when it falls due, and whether the mirror can cleanse it from the manual.
    /// </summary>
    public sealed class PatronDesign
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public DesignDue Due { get; init; }
        public int Years { get; init; }           // for a design due after years
        public bool Cleansable { get; init; }     // a mark or a flaw the mirror can wash out of the manual
        public int Weight { get; init; } = 1;     // how often a patron harbours it
        public string Source { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }
}
