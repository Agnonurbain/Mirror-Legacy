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

        /// <summary>
        /// True when the defender's metallic essence leaves the attacker powerless: a True Monarch whose essence is forged
        /// in a lineage, before a cultivator whose foundation is of that lineage (LORE.md §5.5.2).
        /// </summary>
        public static bool Suppressed(CharacterData attacker, CharacterData defender)
        {
            if (attacker == null || defender == null || defender.Realm < CultivationRealm.GoldenCore || defender.FruitionId == null) return false;
            var (lineage, _) = FoundationRef.Parse(attacker.FoundationId);
            return lineage != null && lineage == defender.FruitionId;
        }

        /// <summary>True when one of the power's True Monarchs holds the lineage of the member's foundation: the member is powerless before it.</summary>
        public static bool SuppressedBy(CharacterData member, FactionData power)
        {
            if (member == null || power?.Elders == null) return false;
            var (lineage, _) = FoundationRef.Parse(member.FoundationId);
            return lineage != null && power.Elders.Any(e => e.Realm >= CultivationRealm.GoldenCore && e.FruitionId == lineage);
        }

        /// <summary>True when at least one of the team reaches a foe of this realm.</summary>
        public static bool Reaches(IEnumerable<CharacterData> team, CultivationRealm foe, RealmGapSettings gap) =>
            (team ?? Enumerable.Empty<CharacterData>()).Any(m => m != null && !OutOfReach(m.Realm, foe, gap));
    }
}
