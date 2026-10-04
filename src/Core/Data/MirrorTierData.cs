using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>How far the mirror perceives: its domain, the lake and its shores, the whole state, everywhere.</summary>
    public enum MirrorReach { Domain, Lake, State, Everywhere }

    /// <summary>
    /// A tier of the mirror's restoration (📚 Lu_Jiangxian/Abilities; LORE.md §11.5; the user's decision 2026-10-04): from how
    /// many shards it holds, whom the Supreme Yin Profound Light kills and wounds, how far it perceives, whether it crafts
    /// grand illusions and locks souls.
    /// </summary>
    public sealed record MirrorTier
    {
        public int MinShards { get; init; }
        public CultivationRealm LightKills { get; init; }
        public CultivationRealm LightWounds { get; init; }
        public MirrorReach Perception { get; init; }
        public bool Illusions { get; init; }
        public bool SoulLocks { get; init; }
    }

    /// <summary>
    /// The mirror's restoration tiers (balance.json « mirrorTiers »). 📚 The Light: at first a peak Summit Eye strike; with a
    /// shard it kills the Qi Cultivation and wounds a realm above; with the Jade Buckle (the Great Void) it kills a Purple
    /// Mansion bar an exceptional foundation. Its divine sense: 66 m, the village, the whole lake, half the realm. Grand
    /// illusions and soul locks are late feats. Interpretations: the tier boundaries; a wounded elder loses
    /// <see cref="ElderWoundYears"/> years; a power whose elder the Light strikes gains <see cref="StrikeClues"/> clues.
    /// </summary>
    public sealed record MirrorTierSettings
    {
        public IReadOnlyList<MirrorTier> Tiers { get; init; } = new List<MirrorTier>
        {
            new MirrorTier { MinShards = 0, LightKills = CultivationRealm.Embryonic, LightWounds = CultivationRealm.QiRefinement, Perception = MirrorReach.Domain },
            new MirrorTier { MinShards = 1, LightKills = CultivationRealm.QiRefinement, LightWounds = CultivationRealm.Foundation, Perception = MirrorReach.Lake },
            new MirrorTier { MinShards = 3, LightKills = CultivationRealm.Foundation, LightWounds = CultivationRealm.PurpleMansion, Perception = MirrorReach.State, Illusions = true },
        };

        /// <summary>Once the Great Void is open to it (the Jade Buckle).</summary>
        public MirrorTier GreatVoid { get; init; } = new MirrorTier
        {
            LightKills = CultivationRealm.PurpleMansion, LightWounds = CultivationRealm.GoldenCore, Perception = MirrorReach.Everywhere, Illusions = true, SoulLocks = true,
        };

        public int ElderWoundYears { get; init; } = 30;
        public int StrikeClues { get; init; } = 10;
    }
}
