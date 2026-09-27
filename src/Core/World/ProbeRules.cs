using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the probes (« sondages », user decision 2026-09-27).</summary>
    public static class ProbeRules
    {
        /// <summary>
        /// A probe's chance: the approach's base; the prober's strength against the target's guard; less for a graver secret
        /// (more so for a provocation, which reads only the surface), a watchful target, one that distrusts the prober;
        /// more for an insider, a neighbour, a bribe (a merchant sells); the target's temper against the approach.
        /// </summary>
        public static double SuccessChance(ProbeFactors f, SecretSettings s)
        {
            double percent = Value(s.BaseChance, f.Approach)
                + (f.ProberStrength - f.TargetGuard) * s.StrengthWeight
                - f.Rank * Value(s.RankPenalty, f.Approach)
                - f.Alertness * s.AlertnessWeight
                - f.TargetDistrust * s.DistrustWeight
                + (f.Insider ? s.InsiderBonus : 0)
                + (f.Neighbours ? s.NeighbourBonus : 0)
                + f.Bonus;
            if (f.Approach == ProbeApproach.Bribery)
                percent += f.Stones / (double)Math.Max(1, s.BribeUnit) * (s.BribeTemper.TryGetValue(f.TargetTemper, out var t) ? t : 1.0);
            if (s.TemperMod.TryGetValue(f.TargetTemper, out var mods) && mods.TryGetValue(f.Approach, out var mod)) percent += mod;
            return Math.Clamp(percent, s.MinPercent, s.MaxPercent) / 100.0;
        }

        /// <summary>The chance a probe is seen: the approach's own, more against a watchful target.</summary>
        public static double DetectChance(ProbeApproach approach, int alertness, SecretSettings s) =>
            approach == ProbeApproach.MirrorSight ? 0 : Math.Clamp(Value(s.DetectChance, approach) + alertness * s.AlertDetect, 0, 1);

        /// <summary>
        /// An ally's chance of coming to the target's aid in time: less when it distrusts the target, less when its temper
        /// finds profit in lingering — long enough for the secret to come out, and to learn it.
        /// </summary>
        public static double AllyPromptChance(FactionData ally, int distrust, SecretSettings s) =>
            Math.Clamp(s.AllyPromptBase - distrust / 100.0 * s.AllyDistrustWeight - (s.AllyLinger.TryGetValue(ally.Personality, out var l) ? l : 0), 0, 1);

        private static double Value<T>(System.Collections.Generic.IReadOnlyDictionary<ProbeApproach, T> table, ProbeApproach approach)
            where T : IConvertible => table.TryGetValue(approach, out var v) ? v.ToDouble(null) : 0;
    }
}
