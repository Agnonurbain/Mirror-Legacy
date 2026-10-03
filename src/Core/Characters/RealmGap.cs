using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The gap between realms (AUDIT_LORE.md §1, 2026-10-03), pure: whom a realm cannot reach, and a team's strength
    /// against a foe — those out of reach add nothing, whatever their number.
    /// </summary>
    public static class RealmGap
    {
        /// <summary>True when one of the attacker's realm can neither see nor touch a foe of the defender's realm.</summary>
        public static bool OutOfReach(CultivationRealm attacker, CultivationRealm defender, RealmGapSettings gap)
        {
            if (attacker >= defender) return false;
            return defender >= gap.UnreachableFrom || (int)defender - (int)attacker > gap.MaxRealmsBehind;
        }

        /// <summary>
        /// A team's strength against a foe of this realm: the strongest who reaches it in full, each other who reaches it
        /// by <paramref name="helpShare"/>; 0 when none reaches it.
        /// </summary>
        public static double TeamStrength(IEnumerable<CharacterData> team, CultivationRealm foe, double helpShare, RealmGapSettings gap)
        {
            var powers = (team ?? Enumerable.Empty<CharacterData>()).Where(m => m != null && !OutOfReach(m.Realm, foe, gap))
                .Select(m => (double)HuntRules.Power(m)).OrderByDescending(p => p).ToList();
            return powers.Count == 0 ? 0 : powers[0] + powers.Skip(1).Sum() * helpShare;
        }

        /// <summary>True when at least one of the team reaches a foe of this realm.</summary>
        public static bool Reaches(IEnumerable<CharacterData> team, CultivationRealm foe, RealmGapSettings gap) =>
            (team ?? Enumerable.Empty<CharacterData>()).Any(m => m != null && !OutOfReach(m.Realm, foe, gap));
    }
}
