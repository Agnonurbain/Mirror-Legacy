using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// A climb without trial, method nor Qi, for the ways that rise on their own: a bound beast's awakening lineage (R16), a
    /// monk's nature (R13-R14). The member's XP is spent on its next stages, then on the next realm at its apogee, up to a
    /// ceiling realm and stage.
    /// </summary>
    public static class TriallessClimb
    {
        /// <summary>Climbs while the XP allows; <paramref name="onNewRealm"/> is told each realm entered.</summary>
        public static void Climb(CharacterData member, CultivationRealm ceiling, int ceilingStage, Action<CharacterData> onNewRealm)
        {
            while (true)
            {
                int required = PowerLadder.XpForNextStage(member.Realm);
                bool apogee = member.RealmStage >= PowerLadder.StageCount(member.Realm);
                bool atCeiling = member.Realm > ceiling || (member.Realm == ceiling && (member.RealmStage >= ceilingStage || apogee));
                if (required <= 0 || member.CultivationXP < required || atCeiling) return;

                member.CultivationXP -= required;
                if (!apogee)
                {
                    member.RealmStage++;
                    continue;
                }
                member.Realm++;
                member.RealmStage = 1;
                onNewRealm?.Invoke(member);
            }
        }
    }
}
