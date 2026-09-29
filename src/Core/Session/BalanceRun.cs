using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    /// <summary>What befell the clan and the powers in one automatic game (counts for the whole run, and the final state).</summary>
    public sealed record BalanceReport(
        int Seed, int Years, bool Won, bool Lost, int Members, int Powers, int Stones, CultivationRealm BestRealm,
        int Strikes, int Captures, int Coalitions, int ClanWars, int PowerWars, int Peaces, int Absorptions,
        int Betrayals, int Blackmails, int Thefts, int ProbesSpotted, int Challenges, int Deaths, int CombatDeaths);

    /// <summary>
    /// Long automatic games (balance, 2026-09-29): a passive clan — no orders given, or only the idle set to work — lives through the years while the
    /// world plots, probes, bands and wars. The run counts what befell it, so the tuning of balance.json can check that
    /// none of it runs away. Engine-free: <c>./Scripts/dev.sh balance</c> prints <see cref="Table"/> over many seeds.
    /// </summary>
    public static class BalanceRun
    {
        /// <param name="autopilot">Each year, every free member without a task is set to cultivate, or else to mine.</param>
        public static BalanceReport Play(GameContent content, int seed, int years, out GameSession session, bool autopilot = false)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = seed, Content = content });
            var counts = new Dictionary<string, int>();
            void Count(string key) => counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            var bus = s.Events;
            bus.OnClanStruck += _ => Count("strike");
            bus.OnMemberCaptured += (_, _) => Count("capture");
            bus.OnCoalitionFormed += _ => Count("coalition");
            bus.OnWarBegun += (a, d) => Count(a == SecretBook.ClanHolder || d == SecretBook.ClanHolder ? "clanWar" : "powerWar");
            bus.OnPeace += (_, _) => Count("peace");
            bus.OnPowerAbsorbed += (_, _) => Count("absorption");
            bus.OnTreatyBetrayed += _ => Count("betrayal");
            bus.OnBlackmail += _ => Count("blackmail");
            bus.OnTheft += (_, _) => Count("theft");
            bus.OnProbeSpotted += _ => Count("probe");
            bus.OnChallengeSettled += (_, _) => Count("challenge");
            bus.OnCharacterDied += (_, cause) => { Count("death"); if (cause == DeathCause.Combat) Count("combatDeath"); };

            int start = s.Clock.Year;
            while (s.Clock.Year - start < years && !s.Victory.IsOver)
            {
                if (autopilot) SetTheIdleToWork(s);
                s.AdvanceYear();
            }

            session = s;
            int Get(string key) => counts.TryGetValue(key, out int n) ? n : 0;
            var living = s.Clan.LivingMembers.ToList();
            return new BalanceReport(seed, s.Clock.Year - start, s.Victory.GameWon, s.Victory.GameLost, living.Count,
                s.Factions.Factions.Count, s.Resources.SpiritStones,
                living.Count == 0 ? CultivationRealm.Embryonic : living.Max(m => m.Realm),
                Get("strike"), Get("capture"), Get("coalition"), Get("clanWar"), Get("powerWar"), Get("peace"), Get("absorption"),
                Get("betrayal"), Get("blackmail"), Get("theft"), Get("probe"), Get("challenge"), Get("death"), Get("combatDeath"));
        }

        /// <summary>Every free member without a task is set to cultivate, or else to mine.</summary>
        public static void SetTheIdleToWork(GameSession session)
        {
            foreach (var member in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.CurrentTask == TaskType.None).ToList())
                if (!session.Tasks.AssignTask(member, TaskType.Cultivation)) session.Tasks.AssignTask(member, TaskType.Mine);
        }

        /// <summary>One line per run, then the means: what to read when tuning.</summary>
        public static string Table(IReadOnlyList<BalanceReport> runs)
        {
            var columns = new (string Name, Func<BalanceReport, double> Value)[]
            {
                ("seed", r => r.Seed), ("years", r => r.Years), ("lost", r => r.Lost ? 1 : 0), ("members", r => r.Members),
                ("powers", r => r.Powers), ("stones", r => r.Stones), ("realm", r => (int)r.BestRealm), ("strikes", r => r.Strikes),
                ("captures", r => r.Captures), ("coalitions", r => r.Coalitions), ("clanWars", r => r.ClanWars),
                ("powerWars", r => r.PowerWars), ("peaces", r => r.Peaces), ("absorbed", r => r.Absorptions),
                ("betrayals", r => r.Betrayals), ("blackmail", r => r.Blackmails), ("thefts", r => r.Thefts),
                ("probes", r => r.ProbesSpotted), ("challenges", r => r.Challenges), ("deaths", r => r.Deaths),
                ("combatDeaths", r => r.CombatDeaths),
            };
            var text = new StringBuilder();
            text.AppendLine(string.Join(" ", columns.Select(c => c.Name.PadLeft(Math.Max(6, c.Name.Length)))));
            foreach (var run in runs)
                text.AppendLine(string.Join(" ", columns.Select(c => c.Value(run).ToString("0").PadLeft(Math.Max(6, c.Name.Length)))));
            if (runs.Count > 0)
                text.AppendLine(string.Join(" ", columns.Select(c => (c.Name == "seed" ? "mean" : runs.Average(c.Value).ToString("0.0"))
                    .PadLeft(Math.Max(6, c.Name.Length)))));
            return text.ToString();
        }
    }
}
