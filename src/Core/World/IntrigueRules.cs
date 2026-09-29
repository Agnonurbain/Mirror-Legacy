using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the powers' intrigues (2026-09-27).</summary>
    public static class IntrigueRules
    {
        /// <summary>A power's yearly chance of blackmailing the clan with the proof it holds.</summary>
        public static double BlackmailChance(FactionData power, IntrigueSettings s) =>
            Math.Clamp(s.BlackmailChance * (s.BlackmailTemper.TryGetValue(power.Personality, out var t) ? t : 1.0), 0, 1);

        /// <summary>A power's yearly chance of coveting the clan's hoard: its greed, and how rich the clan is.</summary>
        public static double GreedChance(FactionData power, int clanStones, IntrigueSettings s) =>
            Math.Clamp(s.GreedChance * (s.GreedTemper.TryGetValue(power.Personality, out var t) ? t : 0.0)
                * Math.Min(1.0, clanStones / (double)s.GreedStonesScale), 0, 1);

        /// <summary>A power's yearly chance of stealing from the clan: its temper, hindered by the clan's patrols.</summary>
        public static double TheftChance(FactionData power, int patrols, IntrigueSettings s) =>
            Math.Clamp(s.TheftChance * (s.TheftTemper.TryGetValue(power.Personality, out var t) ? t : 1.0) / (1 + patrols * s.PatrolGuard), 0, 1);

        /// <summary>The patrols' chance of catching a thief.</summary>
        public static double CatchChance(int patrols, IntrigueSettings s) => Math.Clamp(patrols * s.CatchPerPatrol, 0, s.MaxCatchChance);

        /// <summary>The chance the spouse a power sends is its spy.</summary>
        public static double SpyChance(FactionData power, IntrigueSettings s) =>
            s.SpyChance.TryGetValue(power.Personality, out var c) ? Math.Clamp(c, 0, 1) : 0;
    }
}
