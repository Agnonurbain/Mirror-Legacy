using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the hunt of a ripe Dao (LORE.md §5.3.3; 2026-09-29).</summary>
    public static class DaoHuntRules
    {
        /// <summary>A hunter's yearly chance of learning a member's Dao is ripe: more when away, hardly in seclusion.</summary>
        public static double LearnChance(CharacterData member, DaoHuntSettings s) =>
            Math.Clamp(s.LearnChance * Exposure(member.CurrentTask, s), 0, 1);

        private static double Exposure(TaskType task, DaoHuntSettings s) => task switch
        {
            TaskType.Seclusion => s.SecludedExposure,
            TaskType.Diplomacy or TaskType.Espionage or TaskType.GatherQi or TaskType.HuntBeast or TaskType.ScoutBeasts
                or TaskType.Diversion => s.AwayExposure,
            _ => 1.0
        };

        /// <summary>
        /// The chance a blow succeeds against the clan's defences: a Purple Mansion guardian, patrols, each level of the
        /// Protective Formation, a defensive ally, and seclusion each lower it.
        /// </summary>
        public static double StrikeSuccess(bool purpleMansionGuard, int patrols, int formationLevel, bool defensiveAlly, bool secluded,
            DaoHuntSettings s, double guardedFactor) =>
            Math.Clamp(s.StrikeSuccess
                * (purpleMansionGuard ? guardedFactor : 1.0)
                / (1 + patrols * s.PatrolGuard)
                * Math.Max(0, 1 - formationLevel * s.FormationGuard)
                * (defensiveAlly ? s.AllyGuard : 1.0)
                * (secluded ? s.SecludedGuard : 1.0), 0, 1);
    }
}
