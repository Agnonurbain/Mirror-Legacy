using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>
    /// How far a realm goes (AUDIT_LORE.md §1.5, 2026-10-03), pure: the regions of a state are linked by their borders; a
    /// lesser cultivator flies a few of them, a Foundation crosses its state, a Purple Mansion goes through the Great Void.
    /// </summary>
    public static class TravelRules
    {
        /// <summary>True when one of this realm, from its home, can go to the place (an unknown place stops no one).</summary>
        public static bool CanReach(CultivationRealm realm, string from, string to, IReadOnlyList<RegionDefinition> regions, TravelSettings s)
        {
            if (from == null || to == null || from == to || realm >= s.AnywhereFrom) return true;
            var byId = regions.ToDictionary(r => r.Id);
            if (!byId.ContainsKey(from) || !byId.ContainsKey(to)) return true;
            bool sameState = StateOf(from, byId) == StateOf(to, byId);
            if (!sameState) return false;
            if (realm >= s.WholeStateFrom) return true;
            int max = s.HopsByRealm.TryGetValue(realm, out var h) ? h : 0;
            var hops = Hops(from, to, byId, max);
            return hops != null && hops <= max;
        }

        /// <summary>True when a power can send its own to the place: as far as its strongest carries them (world parity).</summary>
        public static bool PowerReaches(FactionData power, string to, GameContent content) =>
            power != null && CanReach(power.HighestRealm, power.RegionId, to, content.Regions, content.Balance.Travel);

        /// <summary>Why the team cannot go there (French), or null: its strongest carries the others.</summary>
        public static string Refusal(IEnumerable<CharacterData> team, string home, string to, IReadOnlyList<RegionDefinition> regions, TravelSettings s)
        {
            var top = (team ?? Enumerable.Empty<CharacterData>()).Where(m => m != null).Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (CanReach(top, home, to, regions, s)) return null;
            string place = regions.FirstOrDefault(r => r.Id == to)?.Name ?? to;
            string reach = top >= s.WholeStateFrom ? "ne quitte pas son État"
                : $"ne vole qu'à {(s.HopsByRealm.TryGetValue(top, out var h) ? h : 0)} région(s) du domaine";
            return $"{place} est trop loin : le plus fort de l'équipe ({RankCatalog.RealmName(top)}) {reach} — il faudrait le Manoir Pourpre pour traverser le Grand Vide";
        }

        private static string StateOf(string id, IReadOnlyDictionary<string, RegionDefinition> byId) =>
            byId.TryGetValue(id, out var r) && r.ParentId != null ? r.ParentId : id;

        /// <summary>The fewest borders crossed from one place to another, looking no further than <paramref name="max"/>; null beyond.</summary>
        private static int? Hops(string from, string to, IReadOnlyDictionary<string, RegionDefinition> byId, int max)
        {
            var seen = new HashSet<string> { from };
            var frontier = new List<string> { from };
            for (int d = 0; d <= max; d++)
            {
                if (frontier.Contains(to)) return d;
                frontier = frontier.SelectMany(x => byId.TryGetValue(x, out var r) ? r.Neighbours : Enumerable.Empty<string>())
                    .Where(seen.Add).ToList();
            }
            return null;
        }
    }
}
