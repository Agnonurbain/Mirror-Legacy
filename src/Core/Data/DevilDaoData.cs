using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// The Heavenly Embryo Devil Dao (balance.json « devilDao »; LORE.md §3.2, R12; the wiki « Cultivation System »; the user's
    /// choices of 2026-10-13): after the six chakras a member may form a Diverse Mansion — merging the Juque Court, the Shenyang
    /// Mansion and the Qihai acupoint — instead of entering Qi Cultivation: no method nor Qi, but blood essence. Every realm it
    /// passes thirsts for blood (the Unified Furnace, the Purple Mansion), and its two ascents become one: a single trial to the
    /// Purple Mansion. Blood comes from captured beasts bled and from foes struck down on the grid; a practitioner found out
    /// earns the orthodox powers' distrust.
    /// </summary>
    public sealed class DevilDaoSettings
    {
        /// <summary>The blood essence to form the Diverse Mansion, the Unified Furnace, and to ascend to the Purple Mansion.</summary>
        public int FormBlood { get; init; }
        public int FurnaceBlood { get; init; }
        public int MansionBlood { get; init; }

        /// <summary>The share of a human's yearly cultivation a devil gains, cultivating.</summary>
        public double Speed { get; init; }

        /// <summary>The single ascent's chance to the Purple Mansion; a failure wounds the Dao.</summary>
        public double AscentChance { get; init; }

        /// <summary>The blood a bled beast gives by its realm, and each foe struck down on the grid beside a devil.</summary>
        public Dictionary<CultivationRealm, int> BloodByRealm { get; init; } = new Dictionary<CultivationRealm, int>();
        public int BloodPerKill { get; init; }

        /// <summary>Each practitioner's yearly chance to be found out, and the orthodox powers' distrust then.</summary>
        public double DiscoveryChance { get; init; }
        public int DiscoveryDistrust { get; init; }
    }
}
