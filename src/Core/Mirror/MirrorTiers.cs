using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Mirror
{
    /// <summary>The mirror's restoration tiers, pure (AUDIT_LORE.md §3, the user's decision 2026-10-04).</summary>
    public static class MirrorTiers
    {
        /// <summary>The tier of a mirror with so many shards; the Great Void's once it is open.</summary>
        public static MirrorTier Of(int restored, bool voidOpen, MirrorTierSettings s) =>
            voidOpen ? s.GreatVoid : s.Tiers.Where(t => t.MinShards <= restored).OrderByDescending(t => t.MinShards).FirstOrDefault() ?? s.Tiers[0];

        /// <summary>True when a mirror of this reach, at home, perceives the place (an unknown place: only from everywhere).</summary>
        public static bool Perceives(MirrorReach reach, string home, string place, IReadOnlyList<RegionDefinition> regions)
        {
            if (reach == MirrorReach.Everywhere || place == home) return true;
            var byId = regions.ToDictionary(r => r.Id);
            if (place == null || home == null || !byId.ContainsKey(place) || !byId.ContainsKey(home)) return false;
            if (reach == MirrorReach.Lake) return byId[home].Neighbours.Contains(place);
            if (reach == MirrorReach.State) return (byId[home].ParentId ?? home) == (byId[place].ParentId ?? place);
            return false;
        }

        /// <summary>The Light's effect on one of this realm: « tue », « blesse », or null when beyond it.</summary>
        public static string Effect(MirrorTier tier, CultivationRealm realm) =>
            realm <= tier.LightKills ? "tue" : realm <= tier.LightWounds ? "blesse" : null;
    }
}
