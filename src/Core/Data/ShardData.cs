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
        public CultivationRealm GuardRealm { get; init; } // what guards ruins (the realm an expedition measures itself against)
        public bool OpensGreatVoid { get; init; }          // 📚 the Jade Buckle gives the mirror back the Great Void
        public ShardMemory Memory { get; init; } = new ShardMemory();
        public string Notes { get; init; }
        public Provenance Provenance { get; init; }
        public IReadOnlyList<string> InterpretedFields { get; init; } = Array.Empty<string>();
    }

    /// <summary>What founding the clan's sect asks (balance.json « sect »; B3d; interpretations).</summary>
    public sealed record SectSettings
    {
        public CultivationRealm MinRealm { get; init; }  // a member able to hold a peak
        public int MinCultivators { get; init; }         // at the Qi Cultivation or above
        public int FoundingStones { get; init; }         // the peaks' price
    }

    /// <summary>How the shards are found (balance.json « shards »; interpretations).</summary>
    public sealed record ShardSettings
    {
        /// <summary>Each searcher's chance a year to dredge the lake's shard up (🔎).</summary>
        public double LakeSearchChance { get; init; }

        /// <summary>The chance a discovery of ruins reveals the next ruins holding a shard (🔎).</summary>
        public double RuinsRevealChance { get; init; }

        /// <summary>An expedition's odds (🔎): a base, and so much per point of power the team has over the guardian, bounded.</summary>
        public double ExpeditionBaseChance { get; init; }
        public double ExpeditionChancePerPower { get; init; }
        public double ExpeditionMinChance { get; init; }
        public double ExpeditionMaxChance { get; init; }

        /// <summary>The share of each member's power besides the strongest's that helps (🔎).</summary>
        public double ExpeditionHelpShare { get; init; }
        public int ExpeditionMaxTeam { get; init; }

        /// <summary>A failed expedition: the chance the weakest dies, and each other member's chance of a Dao wound (🔎).</summary>
        public double ExpeditionDeathChance { get; init; }
        public double ExpeditionWoundChance { get; init; }

        /// <summary>A theft's odds against the holder's guard, and on failure the chance the thief is caught and the proof left (🔎).</summary>
        public double TheftBaseChance { get; init; }
        public double TheftChancePerPower { get; init; }
        public double TheftMinChance { get; init; }
        public double TheftMaxChance { get; init; }
        public double TheftCaughtChance { get; init; }
        public int TheftEvidence { get; init; }

        /// <summary>The clues on the mirror each taking leaves its holder (🔎): a thief caught, loot of war, a vassal's due, a trade.</summary>
        public int CaughtClues { get; init; }
        public int WarClues { get; init; }
        public int VassalClues { get; init; }
        public int TradeClues { get; init; }

        /// <summary>A vassal made to hand a shard over resents it; a holder sells to a clan it likes enough, for a share of its wealth (🔎).</summary>
        public int VassalRelationLoss { get; init; }
        public int TradeMinRelation { get; init; }
        public double TradePriceShare { get; init; }
        public int TradeMinPrice { get; init; }

        /// <summary>A year's search of the Great Void for the last shard: the chance to find it, and to be lost among its demons (🔎).</summary>
        public double VoidSearchChance { get; init; }
        public double VoidDeathChance { get; init; }
    }
}
