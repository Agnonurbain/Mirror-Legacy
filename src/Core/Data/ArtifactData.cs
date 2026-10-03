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
        ArtifactEffect Effect, int Strength, double Protection, double Cultivation, double LineageFactor)
    {
        public string LentBy { get; init; }  // the power that lent it to the clan (« clan »: the clan's own, lent to a power); null: owned
        public int DueYear { get; init; }    // the year a loan ends
    }

    /// <summary>What an artifact of a rank gives (by its effect).</summary>
    public sealed record ArtifactScale
    {
        public int Strength { get; init; }
        public double Protection { get; init; }
        public double Cultivation { get; init; }
    }

    /// <summary>What forging an artifact of a rank asks: the forge's level, ores and stones.</summary>
    public sealed record ArtifactForging
    {
        public int ForgeLevel { get; init; }
        public int Ores { get; init; }
        public int Stones { get; init; }
    }

    /// <summary>The artifacts between the clan and the powers: commissions, sales, loans, thefts.</summary>
    public sealed record ArtifactTradeSettings
    {
        public double CommissionMarkup { get; init; } = 2.0; // a sect's work, dearer than the clan's own forge
        public int OreValue { get; init; } = 5;              // stones an ore is worth in a price
        public int CommissionRelation { get; init; }         // a power works for the clan only from this relation
        public double SellShare { get; init; } = 0.5;        // what a power pays of an artifact's worth
        public int LoanRelation { get; init; } = 50;         // a power lends from this relation
        public int LoanYears { get; init; } = 10;
        public int LendRelationGain { get; init; } = 10;
        public int KeepRelation { get; init; }               // a borrower below this relation keeps the clan's loan
        public int CaughtEvidence { get; init; } = 20;
        public int CaughtRelation { get; init; } = -25;
        public double CaughtDeathChance { get; init; } = 0.1;
    }

    /// <summary>The odds of finding artifacts: in ruins, in a tomb, in a yielding enemy's halls, after a Purple Mansion's death.</summary>
    public sealed record ArtifactFinding
    {
        public double RuinsChance { get; init; }
        public double TombTreasureChance { get; init; }
        public double WarLootChance { get; init; }
        public double WarTreasureChance { get; init; }
        public double MansionDeathTreasureChance { get; init; }
    }

    /// <summary>The artifacts (balance.json « artifacts », L4f, user decisions 2026-10-03 — interpretations).</summary>
    public sealed record ArtifactSettings
    {
        public ArtifactFinding Finding { get; init; } = new ArtifactFinding();
        public ArtifactTradeSettings Trade { get; init; } = new ArtifactTradeSettings();
        public Dictionary<CultivationRealm, ArtifactForging> Forging { get; init; } = new Dictionary<CultivationRealm, ArtifactForging>();
        public double RaiseShare { get; init; } = 0.6; // raising one costs this share of forging it anew
        public Dictionary<CultivationRealm, ArtifactScale> Ranks { get; init; } = new Dictionary<CultivationRealm, ArtifactScale>();
        public Dictionary<ArtifactClass, double> ClassFactor { get; init; } = new Dictionary<ArtifactClass, double>();
        public double LineageFactor { get; init; } = 1.5;
    }
}
