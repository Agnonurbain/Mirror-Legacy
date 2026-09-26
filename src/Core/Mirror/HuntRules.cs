using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.Mirror
{
    /// <summary>The odds of a planned hunt (L2c.3), pure: the approach, the capture, the traces a clean hunt leaves.</summary>
    public static class HuntRules
    {
        /// <summary>A cultivator's or a beast's power: realm × 10 + stage.</summary>
        public static int Power(CultivationRealm realm, int stage) => (int)realm * 10 + stage;

        /// <summary>
        /// Getting close unseen: lookouts, the timing (a festival helps only against an owner), a diversion elsewhere and
        /// the mirror's illusion help; the owner's guard — its strongest realm — hinders.
        /// </summary>
        public static int ApproachChance(HuntPlan plan, WorldBeast beast, FactionManager factions, GameContent content)
        {
            var s = content.Balance.Hunt;
            int lookouts = Math.Min(s.MaxLookouts, plan.Team.Values.Count(r => r == HuntRole.Lookout));
            var owner = factions.GetFactionByName(beast.OwnerFaction);
            int timing = plan.Timing == HuntTiming.Festival && owner == null ? 0 : s.TimingApproach[(int)plan.Timing];
            int chance = s.ApproachBase + lookouts * s.LookoutBonus + timing
                + (plan.DiversionMemberId != null ? s.DiversionBonus : 0)
                + (plan.Aid == MirrorAid.Illusion ? s.IllusionBonus : 0)
                - (owner == null ? 0 : (int)owner.HighestRealm * s.GuardPenaltyPerRealm);
            return Clamp(chance);
        }

        /// <summary>Taking the beast: the best striker's power against the beast's, the lures, the timing.</summary>
        public static int CaptureChance(HuntPlan plan, WorldBeast beast, ClanManager clan, GameContent content)
        {
            var s = content.Balance.Hunt;
            int best = plan.Team.Where(p => p.Value == HuntRole.Striker)
                .Select(p => clan.FindById(p.Key)).Where(m => m != null)
                .Select(m => Power(m.Realm, m.RealmStage)).DefaultIfEmpty(0).Max();
            int lures = plan.Team.Values.Count(r => r == HuntRole.Lure);
            int chance = s.CaptureBase + (best - Power(beast.Realm, beast.Stage)) * s.CapturePerPowerPoint
                + lures * s.LureBonus + s.TimingCapture[(int)plan.Timing];
            return Clamp(chance);
        }

        /// <summary>The traces a clean hunt leaves: fewer with a cover story and the mirror's stolen memories.</summary>
        public static int CleanExposure(HuntPlan plan, GameContent content)
        {
            var s = content.Balance.Hunt;
            return Math.Max(0, s.CleanBaseExposure - s.CoverExposure[(int)plan.Cover]
                - (plan.Aid == MirrorAid.MemoryTheft ? s.MemoryTheftExposure : 0));
        }

        private static int Clamp(int chance) => Math.Max(1, Math.Min(99, chance));
    }
}
