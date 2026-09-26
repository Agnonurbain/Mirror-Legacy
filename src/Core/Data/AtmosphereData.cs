using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A regional atmosphere (atmospheres.json, LORE.md §5.8): the abundance of a Qi, or a lasting celestial phenomenon
    /// (§5.7), that favours some lineages, elements and paths — faster cultivation, safer breakthroughs — and may weigh
    /// on every cultivator of the place. Only Purple Mansions and above can sway one; the great sects fight over them.
    /// </summary>
    public sealed class AtmosphereDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public IReadOnlyList<string> FavouredFruitions { get; init; } = Array.Empty<string>();
        public IReadOnlyList<Element> FavouredElements { get; init; } = Array.Empty<Element>();
        public IReadOnlyList<CultivationPath> FavouredPaths { get; init; } = Array.Empty<CultivationPath>();

        /// <summary>The cultivation speed every cultivator of the place gains (or loses, when negative).</summary>
        public double GeneralSpeed { get; init; }

        /// <summary>What the favoured gain besides: cultivation speed, and points of breakthrough chance.</summary>
        public double FavouredSpeed { get; init; }
        public int FavouredBreakthrough { get; init; }

        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// The Qi of the places (balance.json « regionalQi », L5b; LORE.md §2.5; interpretations): the elements each kind of
    /// place carries, the Qi families found everywhere (the Twelve Qi), each kind's density, how a lineage's state sways
    /// its Qi's abundance, and the pace of a member whose Qi the place does not offer.
    /// </summary>
    public sealed record RegionalQiSettings
    {
        public Dictionary<RegionKind, List<Element>> KindElements { get; init; }
        public List<QiFamily> EverywhereFamilies { get; init; } = new List<QiFamily>();
        public Dictionary<RegionKind, double> KindDensity { get; init; }
        public Dictionary<FruitionStatus, double> LineageStatusFactors { get; init; }
        public double AbsentQiFactor { get; init; } = 1;
    }
}
