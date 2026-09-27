using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The pure rules of open war (2026-09-27).</summary>
    public static class WarRules
    {
        /// <summary>A power's war strength: its strongest realm, and its size.</summary>
        public static double Strength(FactionData power, WarSettings s) =>
            HuntRules.Power(power.HighestRealm, 5) + System.Math.Max(0, power.PowerLevel) * s.StrengthPerPowerLevel;
    }
}
