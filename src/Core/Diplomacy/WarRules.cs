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
            members.Where(m => m.CaptorFaction == null).Select(m => HuntRules.Power(m.Realm, m.RealmStage)).DefaultIfEmpty(0).Max();

        /// <summary>A power's war strength: its strongest realm, and its size.</summary>
        public static double Strength(FactionData power, WarSettings s) =>
            HuntRules.Power(power.HighestRealm, 5) + System.Math.Max(0, power.PowerLevel) * s.StrengthPerPowerLevel;
    }
}
