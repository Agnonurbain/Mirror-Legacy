using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    /// <summary>What befell the clan and the powers in one automatic game (counts for the whole run, and the final state).</summary>
    public sealed record BalanceReport(
        int Seed, int Years, bool Won, bool Lost, int Members, int Powers, int Stones, CultivationRealm BestRealm,
        int Strikes, int Captures, int Coalitions, int ClanWars, int PowerWars, int Peaces, int Absorptions,
        int Betrayals, int Blackmails, int Thefts, int ProbesSpotted, int Challenges, int Deaths, int CombatDeaths, int PoorYears,
        int Cultivators, int Devoured, int Extortions, int Foiled);

    /// <summary>
    /// Long automatic games (balance, 2026-09-29): a passive clan — no orders given, or only the idle set to work — lives through the years while the
    /// world plots, probes, bands and wars. The run counts what befell it, so the tuning of balance.json can check that
    /// none of it runs away. Engine-free: <c>./Scripts/dev.sh balance</c> prints <see cref="Table"/> over many seeds.
    /// </summary>
    public static class BalanceRun
    {
        /// <param name="autopilot">Each year the pilot acts (<see cref="Act"/>) and sets the free members to work (<see cref="SetTheIdleToWork"/>).</param>
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
            bus.OnExtortion += _ => Count("extortion");
            bus.OnDaoHuntFoiled += _ => Count("foiled");
            bus.OnTheft += (_, _) => Count("theft");
            bus.OnProbeSpotted += _ => Count("probe");
            bus.OnChallengeSettled += (_, _) => Count("challenge");
            bus.OnCharacterDied += (_, cause) =>
            {
                Count("death");
                if (cause == DeathCause.Combat) Count("combatDeath");
                if (cause == DeathCause.RipeDaoHarvested) Count("devoured");
            };

            int start = s.Clock.Year;
            while (s.Clock.Year - start < years && !s.Victory.IsOver)
            {
                if (autopilot)
                {
                    Act(s);
                    SetTheIdleToWork(s);
                }
                s.AdvanceYear();
                if (s.Upkeep.Impoverished) Count("poor");
            }

            session = s;
            int Get(string key) => counts.TryGetValue(key, out int n) ? n : 0;
            var living = s.Clan.LivingMembers.ToList();
            return new BalanceReport(seed, s.Clock.Year - start, s.Victory.GameWon, s.Victory.GameLost, living.Count,
                s.Factions.Factions.Count, s.Resources.SpiritStones,
                living.Count == 0 ? CultivationRealm.Embryonic : living.Max(m => m.Realm),
                Get("strike"), Get("capture"), Get("coalition"), Get("clanWar"), Get("powerWar"), Get("peace"), Get("absorption"),
                Get("betrayal"), Get("blackmail"), Get("theft"), Get("probe"), Get("challenge"), Get("death"), Get("combatDeath"), Get("poor"),
                living.Count(SpiritualOrificeRules.CanCultivate), Get("devoured"), Get("extortion"), Get("foiled"));
        }

        private const int ReserveYears = 2;      // the pilot pays a demand only if it keeps two years of upkeep
        private const int PeaceAfterYears = 2;   // it sues for peace after two years of war
        private const int TreatyEveryYears = 5;  // it seeks a treaty every five years
        private const int MostTreaties = 3;      // and keeps a few, not a web of them
        private const int SeedReserve = 20;      // it keeps some of the mirror's power
        private const int SeedMinAge = 10;       // a seed for the young: old enough to be examined,
        private const int SeedMaxAge = 30;       // young enough to cultivate long

        /// <summary>
        /// The active pilot (user decision 2026-09-29): what a prudent clan does each year before its tasks — it pays a
        /// demand it can afford and refuses the rest, answers a challenge of its own rank with its best fighters (who
        /// then fight on their own) and flees the others, sues for peace after two years of war, plants a Talisman Seed
        /// in a young examined mortal when the mirror can spare it, hides its ripe Daos in seclusion, and seeks a non-aggression pact now and then (a few at most).
        /// </summary>
        public static void Act(GameSession session)
        {
            AnswerDemands(session);
            AnswerChallenge(session);
            foreach (var war in session.Wars.ClanWars.Where(w => session.Clock.Year - w.StartYear >= PeaceAfterYears).ToList())
                session.Wars.SuePeace(war.Enemy);
            PlantASeed(session);
            foreach (var prey in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Retreat == Retreat.None
                && m.CurrentTask != TaskType.Seclusion && FoundationRules.IsPrey(m, session.Context.Content)).ToList())
                session.Tasks.AssignTask(prey, TaskType.Seclusion); // a ripe Dao hides
            if (session.Clock.Year % TreatyEveryYears == 0 && session.Treaties.All.Count < MostTreaties) SeekATreaty(session);
        }

        private static void AnswerDemands(GameSession session)
        {
            foreach (var demand in session.Intrigues.Demands.ToList())
            {
                bool affordable = session.Resources.SpiritStones - demand.Stones >= session.Upkeep.YearlyUpkeep * ReserveYears;
                if (affordable) session.Intrigues.Pay(demand.Faction);
                else session.Intrigues.Refuse(demand.Faction);
            }
        }

        private static void AnswerChallenge(GameSession session)
        {
            var challenge = session.Challenges.Pending;
            if (challenge == null) return;
            int most = session.Context.Content.Balance.Challenges.MaxFighters;
            var fighters = session.Clan.LivingMembers.Where(m => session.Challenges.FighterRefusal(m) == null)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).Take(most).ToList();
            if (fighters.Count == 0 || fighters[0].Realm < challenge.Realm)
            {
                session.Challenges.Decline();
                return;
            }
            if (session.Challenges.Accept(fighters.Select(m => m.ID).ToList()) != null)
            {
                session.Challenges.Decline(); // it could not answer: better to flee than to leave it hanging
                return;
            }
            session.Challenges.Current?.AutoPlay();
            session.Challenges.Conclude();
        }

        private static void PlantASeed(GameSession session)
        {
            if (session.Mirror.MirrorPower < Mirror.MirrorSystem.TalismanSeedCost + SeedReserve) return;
            var mortal = session.Clan.LivingMembers
                .Where(m => m.CaptorFaction == null && m.OrificeKnown && !m.HasTalismanSeed && !SpiritualOrificeRules.CanCultivate(m)
                    && m.Age >= SeedMinAge && m.Age <= SeedMaxAge)
                .OrderBy(m => m.Age).FirstOrDefault();
            if (mortal != null) session.Mirror.GrantTalismanSeed(mortal);
        }

        private static void SeekATreaty(GameSession session)
        {
            var friend = session.Factions.Factions.Where(f => session.Treaties.With(f.Name).Count == 0)
                .OrderByDescending(f => f.RelationWithPlayer).FirstOrDefault();
            if (friend != null) session.Treaties.Propose(friend.Name, TreatyKind.NonAggression);
        }

        /// <summary>
        /// Every free member is set to work: a cultivator at the Foundation wall without the portion of Qi it absorbs
        /// gathers it (LORE.md §2.5), then cultivates again; the idle cultivate, or else mine.
        /// </summary>
        public static void SetTheIdleToWork(GameSession session)
        {
            bool huntOpen = session.Talismans.HuntWindowOpen;
            foreach (var member in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList())
            {
                var trial = PowerLadder.Next(member.Realm, member.RealmStage).Trial;
                bool lacksQi = trial == TrialKind.FoundationWall && !session.Cultivation.HasTrialQi(member, trial);
                TaskType wanted = lacksQi && TaskRules.IsAllowed(member, TaskType.GatherQi, huntOpen) ? TaskType.GatherQi
                    : member.CurrentTask == TaskType.GatherQi || member.CurrentTask == TaskType.None ? TaskType.Cultivation
                    : member.CurrentTask;
                if (!TaskRules.IsAllowed(member, wanted, huntOpen)) wanted = TaskType.Mine;
                if (wanted != member.CurrentTask && TaskRules.IsAllowed(member, wanted, huntOpen)) session.Tasks.AssignTask(member, wanted);
            }
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
                ("combatDeaths", r => r.CombatDeaths), ("poorYears", r => r.PoorYears),
                ("cultivators", r => r.Cultivators), ("devoured", r => r.Devoured),
                ("extortions", r => r.Extortions), ("foiled", r => r.Foiled),
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
