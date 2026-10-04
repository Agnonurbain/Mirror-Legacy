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
        public bool SoulLocks { get; init; }   // it locks souls and steals or blurs memories (🔎 from the first shard, 2026-10-04)
        public int MoonlightPerYear { get; init; } // the Supreme Yin Moonlight it condenses each year (🔎)
        public int MoonlightCap { get; init; }     // the most it holds (🔎)
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
            new MirrorTier { MinShards = 0, LightKills = CultivationRealm.Embryonic, LightWounds = CultivationRealm.QiRefinement, Perception = MirrorReach.Domain, MoonlightPerYear = 10, MoonlightCap = 100 },
            new MirrorTier { MinShards = 1, LightKills = CultivationRealm.QiRefinement, LightWounds = CultivationRealm.Foundation, Perception = MirrorReach.Lake, SoulLocks = true, MoonlightPerYear = 15, MoonlightCap = 150 },
            new MirrorTier { MinShards = 3, LightKills = CultivationRealm.Foundation, LightWounds = CultivationRealm.PurpleMansion, Perception = MirrorReach.State, Illusions = true, SoulLocks = true, MoonlightPerYear = 20, MoonlightCap = 200 },
        };

        /// <summary>Once the Great Void is open to it (the Jade Buckle).</summary>
        public MirrorTier GreatVoid { get; init; } = new MirrorTier
        {
            LightKills = CultivationRealm.PurpleMansion, LightWounds = CultivationRealm.GoldenCore, Perception = MirrorReach.Everywhere, Illusions = true, SoulLocks = true, MoonlightPerYear = 30, MoonlightCap = 300,
        };

        public int ElderWoundYears { get; init; } = 30;

        // The Supreme Yin Moonlight given to the clan (📚 « sealed and stored by the Li Family »; LORE.md §11.5; the user's
        // decision 2026-10-04): a portion of a Supreme Yin Qi sealed for the clan, or a member's cultivation nourished; the
        // spies in the clan notice so rare a Qi, and their power gains clues.
        public int GiftCost { get; init; } = 30;
        public string GiftQiId { get; init; } = "supreme-yin-moon-silk-qi";
        public double GiftXpShare { get; init; } = 0.5;
        public int GiftClues { get; init; } = 15;
        public int StrikeClues { get; init; } = 10;
    }
}
