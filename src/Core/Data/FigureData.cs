using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A named cultivator of one of the world's powers (figures.json, from the wiki's character pages, renamed):
    /// the realm they reach (the wiki's, often their height), and when they were born counted from the mirror's
    /// finding (year 0; negative before, null when unknown). The powers' own cultivators arrive with P1 (L5b, L6).
    /// </summary>
    public sealed class FigureDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string FactionName { get; init; }
        public CultivationRealm Realm { get; init; }
        public CultivationPath Path { get; init; }
        public int? BornYear { get; init; }

        /// <summary>The chapter of the source where they first appear (a guide for the chronology, L6).</summary>
        public int? FirstChapter { get; init; }

        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }
}
