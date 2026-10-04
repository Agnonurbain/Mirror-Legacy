using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.World
{
    /// <summary>The pure rules of the powers' schemes and of the captives (L6a).</summary>
    public static class SchemeRules
    {
        private const int MinPercent = 5;
        private const int MaxPercent = 95;

        /// <summary>A power's yearly chance of scheming against the clan: temper, hostility, friendship, greed.</summary>
        public static double SchemeChance(FactionData power, int clanStones, SchemeSettings s)
        {
            double temper = s.PersonalityFactors != null && s.PersonalityFactors.TryGetValue(power.Personality, out var f) ? f : 1.0;
            double hostility = 1 + Math.Max(0, -power.RelationWithPlayer) / 100.0 * s.HostilityWeight;
            double friendship = Math.Max(0, 1 - Math.Max(0, power.RelationWithPlayer) / 100.0 * s.FriendshipDamping);
            double greed = Math.Min(s.MaxGreed, 1 + Math.Max(0, clanStones) / (double)Math.Max(1, s.WealthReference));
            return Math.Clamp(s.BaseChance * temper * hostility * friendship * greed, 0, 1);
        }

        /// <summary>
        /// An ambush's chance of taking the member: the power's strongest against the member's realm; none when the member
        /// is out of its reach (a Purple Mansion before a lesser power, two realms above it — AUDIT_LORE.md §1).
        /// </summary>
        public static double CaptureChance(FactionData power, CharacterData member, SchemeSettings s, RealmGapSettings gap) =>
            Characters.RealmGap.OutOfReach(power.HighestRealm, member.Realm, gap) ? 0
                : Characters.RealmGap.SuppressedBy(member, power) ? MaxPercent / 100.0 // a True Monarch of its own foundation: it cannot resist (LORE.md §5.5.2)
                : Percent(s.CaptureBase + ((int)power.HighestRealm - (int)member.Realm) * s.CapturePerRealm);

        /// <summary>A captive's yearly chance of talking under interrogation.</summary>
        public static double InterrogationChance(CharacterData captive, bool sworn, GameContent content) =>
            Math.Clamp(PlotRules.LeakChance(captive, sworn, content) * content.Balance.Schemes.InterrogationFactor, 0, 1);

        /// <summary>The ransom of a captive of this realm, either side.</summary>
        public static int Ransom(CultivationRealm realm, SchemeSettings s) =>
            (int)(s.RansomBase * Math.Pow(s.RansomRealmFactor, (int)realm));

        /// <summary>
        /// A rescue's chance: the power of those of the team who reach the captor's strongest against it; none when no one
        /// reaches it — numbers do not cross a realm (AUDIT_LORE.md §1).
        /// </summary>
        public static double RescueChance(IEnumerable<CharacterData> team, FactionData captor, SchemeSettings s, RealmGapSettings gap)
        {
            team = team.Where(m => !Characters.RealmGap.SuppressedBy(m, captor)).ToList(); // none of its foundation raises a hand against it
            if (!Characters.RealmGap.Reaches(team, captor.HighestRealm, gap)) return 0;
            int teamPower = (int)Characters.RealmGap.TeamStrength(team, captor.HighestRealm, 1.0, gap);
            int captorPower = HuntRules.Power(captor.HighestRealm, s.CaptorStage);
            return Percent(s.RescueBase + (teamPower - captorPower) * s.RescuePerPowerPoint);
        }

        private static double Percent(int percent) => Math.Clamp(percent, MinPercent, MaxPercent) / 100.0;
    }
}
