using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>Where a shard of the mirror lies (LORE.md §11.5; the user's decision of 2026-09-30).</summary>
    public enum ShardSource { Lake, Ruins, Power, GreatVoid }

    /// <summary>What a shard gives back (📚 each shard is a piece of jade that holds a memory): a technique, facts, a clue.</summary>
    public sealed class ShardMemory
    {
        public string TechniqueId { get; init; }                                   // taught to the clan
        public IReadOnlyList<string> Facts { get; init; } = Array.Empty<string>(); // « Kind:Subject » revealed to the clan
        public string Clue { get; init; }                                          // what the mirror remembers of its origin
    }

    /// <summary>
    /// A shard of the mirror (shards.json; LORE.md §11.5, B3c): where it lies, the memory it holds, and the years the
    /// mirror's spirit sleeps to integrate it.
    /// </summary>
    public sealed class ShardDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public ShardSource Source { get; init; }
        public int SleepYears { get; init; }
        public ShardMemory Memory { get; init; } = new ShardMemory();
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>How the shards are found (balance.json « shards »; interpretations).</summary>
    public sealed record ShardSettings
    {
        /// <summary>Each searcher's chance a year to dredge the lake's shard up (🔎).</summary>
        public double LakeSearchChance { get; init; }
    }
}
