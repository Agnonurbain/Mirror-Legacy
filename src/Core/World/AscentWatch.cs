using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// A new Purple Mansion draws the old powers' eyes (audit §1.9, the user's decision 2026-10-03): the powers of that realm
    /// or above that can reach it grow wary, and one may come to test it — a contest that may wound either side. What befalls
    /// the clan befalls the world: a power's new Purple Mansion is watched and tested by the others.
    /// </summary>
    public sealed class AscentWatch
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly WoundSystem wounds;

        public AscentWatch(GameContext ctx, FactionManager factions, SuspicionLedger suspicion, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.suspicion = suspicion;
            this.wounds = wounds;
            ctx.Events.OnPurpleMansionAscent += ClanAscent;
            ctx.Events.OnElderRose += (power, elder) => { if (elder.Realm == CultivationRealm.PurpleMansion) ElderAscent(power, elder); };
        }

        private AscentWatchSettings Settings => ctx.Content.Balance.AscentWatch;

        /// <summary>The powers of the Purple Mansion or above, other than <paramref name="except"/>, that can reach the place.</summary>
        private List<FactionData> Watchers(string region, FactionData except) =>
            factions.Factions.Where(f => f != except && f.HighestRealm >= CultivationRealm.PurpleMansion && TravelRules.PowerReaches(f, region, ctx.Content))
                .ToList();

        /// <summary>The newcomer's odds of holding its own against the tester's strongest.</summary>
        private double HoldChance(int newcomer, FactionData tester) =>
            Math.Clamp(Settings.TestBase + (newcomer - HuntRules.Power(tester.HighestRealm, 5)) * Settings.TestPerPower, 0.05, 0.95);

        private void ClanAscent(CharacterData member)
        {
            var watchers = Watchers(ctx.Content.Clan.HomeRegion, null);
            if (watchers.Count == 0) return;
            foreach (var w in watchers) suspicion.AddToClan(w.Name, Settings.Wariness);
            ctx.Log.Info($"[Ascent] {member.FullName}'s ascent draws the eyes of {string.Join(", ", watchers.Select(w => w.Name))}.");
            if (!ctx.Rng.Chance(Settings.TestChance)) return;
            var tester = ctx.Rng.Pick(watchers);
            if (ctx.Rng.Chance(HoldChance(HuntRules.Power(member), tester)))
            {
                Wound(tester);
                ctx.Log.Info($"[Ascent] {tester.Name} comes to test {member.FullName}, and its elder leaves hurt.");
            }
            else
            {
                wounds.ApplyDaoWound(member);
                ctx.Log.Warning($"[Ascent] {tester.Name} comes to test {member.FullName}, who bears the wound of it.");
            }
        }

        private void ElderAscent(FactionData power, FactionElder elder)
        {
            var watchers = Watchers(power.RegionId, power);
            if (watchers.Count == 0) return;
            foreach (var w in watchers) suspicion.AddDistrust(w.Name, power.Name, Settings.Wariness);
            if (!ctx.Rng.Chance(Settings.TestChance)) return;
            var tester = ctx.Rng.Pick(watchers);
            if (ctx.Rng.Chance(HoldChance(HuntRules.Power(elder.Realm, elder.Stage), tester))) Wound(tester);
            else elder.MaxLifespan = Math.Max(elder.Age(ctx.Clock.Year) + 1, elder.MaxLifespan - Settings.ElderLifespanLoss);
            ctx.Log.Info($"[Ascent] {tester.Name} tests {elder.Name} of {power.Name}, newly at the Purple Mansion.");
        }

        /// <summary>The tester's strongest elder bears lasting harm: years of its life (📚 Chi Wei).</summary>
        private void Wound(FactionData tester)
        {
            var strongest = tester.Elders.OrderByDescending(e => e.Realm).ThenByDescending(e => e.Stage).FirstOrDefault();
            if (strongest != null) strongest.MaxLifespan = Math.Max(strongest.Age(ctx.Clock.Year) + 1, strongest.MaxLifespan - Settings.ElderLifespanLoss);
        }
    }
}
