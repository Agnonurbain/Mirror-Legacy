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

        /// <summary>A power's war strength: its strongest realm, and its size.</summary>
        public static double Strength(FactionData power, WarSettings s) =>
            HuntRules.Power(power.HighestRealm, 5) + System.Math.Max(0, power.PowerLevel) * s.StrengthPerPowerLevel + power.DomainStrength;
    }
}
