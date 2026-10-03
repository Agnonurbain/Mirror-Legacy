using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// 📚 The classes of artifacts (the wiki's classifications): Dharma Artifacts for the Qi Refinement and the Foundation,
    /// Spiritual Artifacts for the Purple Mansion, and the rarer Spiritual Treasures; above them the Golden Core's Dharma
    /// Treasures and Rank Designations (<see cref="RankDesignation"/>). Read by name: never rename a member.
    /// </summary>
    public enum ArtifactClass { DharmaArtifact, SpiritualArtifact, SpiritualTreasure }

    /// <summary>What an artifact does for its bearer (the user's decisions, 2026-10-03).</summary>
    public enum ArtifactEffect { Combat, Protection, Cultivation }

    /// <summary>A form of artifact (artifacts.json): a weapon, a guard, a cultivation aid — its name and its effect.</summary>
    public sealed class ArtifactForm
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public ArtifactEffect Effect { get; init; }
        public string Source { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// An artifact of the clan: its form, its class and rank, its lineage (null: none), and what it gives — its strength,
    /// its guard, its help to cultivate — and how much more a bearer of its lineage draws from it.
    /// </summary>
    public sealed record ArtifactInstance(string Id, string FormId, string Name, ArtifactClass Class, CultivationRealm Rank, string Lineage,
        ArtifactEffect Effect, int Strength, double Protection, double Cultivation, double LineageFactor);

    /// <summary>What an artifact of a rank gives (by its effect).</summary>
    public sealed record ArtifactScale
    {
        public int Strength { get; init; }
        public double Protection { get; init; }
        public double Cultivation { get; init; }
    }

    /// <summary>The artifacts (balance.json « artifacts », L4f, user decisions 2026-10-03 — interpretations).</summary>
    public sealed record ArtifactSettings
    {
        public Dictionary<CultivationRealm, ArtifactScale> Ranks { get; init; } = new Dictionary<CultivationRealm, ArtifactScale>();
        public Dictionary<ArtifactClass, double> ClassFactor { get; init; } = new Dictionary<ArtifactClass, double>();
        public double LineageFactor { get; init; } = 1.5;
    }
}
