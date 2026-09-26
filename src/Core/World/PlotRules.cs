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

        /// <summary>A keeper's yearly chance of talking: an unsteady mind talks more, an oath of secrecy holds the tongue.</summary>
        public static double LeakChance(CharacterData keeper, bool sworn, GameContent content)
        {
            var s = content.Balance.Plots;
            double mind = Math.Max(0.2, 1.0 + (s.LeakStabilityReference - keeper.MentalStability) / (double)s.LeakStabilityScale);
            return Math.Clamp(s.LeakBaseChance * mind * (sworn ? s.SwornLeakFactor : 1.0), 0, 1);
        }

        /// <summary>Without proof, a power dares only when it is clearly stronger than the clan (profit without risk).</summary>
        public static bool DaresWithoutProof(FactionData power, CultivationRealm clanStrongest, GameContent content) =>
            (int)power.HighestRealm >= (int)clanStrongest + content.Balance.Plots.BoldnessRealmMargin;
    }
}
