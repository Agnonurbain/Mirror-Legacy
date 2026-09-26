using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the powers' plots (L2c.4a).</summary>
    public static class PlotRules
    {
        /// <summary>A power's yearly chance of finding proof: its suspicion, and the reach of its strongest cultivator.</summary>
        public static double InvestigationChance(FactionData power, int suspicion, GameContent content)
        {
            var s = content.Balance.Plots;
            return Math.Clamp(suspicion * s.InvestigationChancePerPoint * (1 + (int)power.HighestRealm * s.InvestigationBonusPerRealm), 0, 1);
        }

        /// <summary>Without proof, a power dares only when it is clearly stronger than the clan (profit without risk).</summary>
        public static bool DaresWithoutProof(FactionData power, CultivationRealm clanStrongest, GameContent content) =>
            (int)power.HighestRealm >= (int)clanStrongest + content.Balance.Plots.BoldnessRealmMargin;
    }
}
