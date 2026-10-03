using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The pure rules of open war (2026-09-27).</summary>
    public static class WarRules
    {
        /// <summary>What a power weighs the clan by: its strongest free member (a captive defends nothing).</summary>
        public static int ClanStrength(IEnumerable<CharacterData> members) =>
            members.Where(m => m.CaptorFaction == null).Select(m => HuntRules.Power(m)).DefaultIfEmpty(0).Max();

        /// <summary>The clan's own war strength, without its allies: its strongest free member, and the others' weight.</summary>
        public static double ClanWarStrength(IEnumerable<CharacterData> members, WarSettings s)
        {
            var fighters = members.Where(m => m.CaptorFaction == null).Select(m => (double)HuntRules.Power(m))
                .OrderByDescending(p => p).ToList();
            return fighters.Count == 0 ? 0 : fighters[0] + fighters.Skip(1).Sum() * s.ClanStrengthPerMember;
        }

        /// <summary>
        /// A side's chance of winning a battle: strength against strength, unless one side's strongest is out of the other's
        /// reach — a host of Foundations cannot touch a Purple Mansion (AUDIT_LORE.md §1).
        /// </summary>
        public static double WinChance(double ours, CultivationRealm ourTop, double theirs, CultivationRealm theirTop, RealmGapSettings gap)
        {
            if (Characters.RealmGap.OutOfReach(ourTop, theirTop, gap)) return 0;
            if (Characters.RealmGap.OutOfReach(theirTop, ourTop, gap)) return 1;
            return ours + theirs <= 0 ? 0.5 : ours / (ours + theirs);
        }

        /// <summary>A power's war strength: its strongest realm, and its size.</summary>
        public static double Strength(FactionData power, WarSettings s) =>
            HuntRules.Power(power.HighestRealm, 5) + System.Math.Max(0, power.PowerLevel) * s.StrengthPerPowerLevel + power.DomainStrength;
    }
}
