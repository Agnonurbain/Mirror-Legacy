using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>The pure rules of keeping the cultivating line (2026-09-29).</summary>
    public static class LineageRules
    {
        /// <summary>The chance a search abroad finds a cultivator willing to wed this member: better by its realm.</summary>
        public static double SeekChance(CharacterData member, LineageSettings s) =>
            Math.Clamp(s.SeekChance + (int)member.Realm * s.SeekChancePerRealm, 0, 1);
    }
}
