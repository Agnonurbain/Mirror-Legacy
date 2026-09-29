using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// Long automatic games (balance, 2026-09-29): a passive clan lives through the years while the world plots, probes,
    /// bands and wars; the run counts what befell the clan and the powers, so that none of it runs away.
    /// </summary>
    [TestFixture]
    public class BalanceRunTests
    {
        [Test]
        public void ARun_IsReproducible_AndReflectsTheSession()
        {
            var first = BalanceRun.Play(Fixtures.Content, seed: 3, years: 15, out var session);
            var again = BalanceRun.Play(Fixtures.Content, seed: 3, years: 15, out _);

            Assert.AreEqual(first, again, "the same seed, the same story");
            Assert.AreEqual(session.Clan.LivingMembers.Count, first.Members);
            Assert.AreEqual(session.Factions.Factions.Count, first.Powers);
            Assert.IsTrue(first.Years == 15 || first.Lost || first.Won, "every year played, unless the game ended");
        }

        [Test]
        public void TheAutopilot_SetsTheIdleToWork()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            BalanceRun.SetTheIdleToWork(session);
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            Assert.IsTrue(free.Any(m => m.CurrentTask == TaskType.Cultivation), "those who can, cultivate");
            Assert.IsTrue(free.All(m => m.CurrentTask != TaskType.None || !session.Tasks.AssignTask(m, TaskType.Mine)), "nobody idle who could work");
        }

        [Test]
        public void TheAutopilot_GathersTheQi_TheFoundationWallAbsorbs()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            var peak = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9);
            session.Clan.AddMember(peak);
            session.Resources.ConsumeQi(peak.QiId, session.Resources.QiPortions(peak.QiId));
            Assume.That(session.Cultivation.HasTrialQi(peak, TrialKind.FoundationWall), Is.False);

            BalanceRun.SetTheIdleToWork(session);

            Assert.AreEqual(TaskType.GatherQi, peak.CurrentTask, "the wall absorbs a portion of their Qi: they gather it");
        }

        // ---- The active pilot ----

        [Test]
        public void ThePilot_PaysADemandItCanAfford_AndRefusesOneItCannot()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Resources.SetSpiritStones(100000);
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Ruan", 500, s.Clock.Year, DemandKind.Protection) }, null);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Intrigues.Demands);
            Assert.AreEqual(100000 - 500, s.Resources.SpiritStones);

            int short_ = 500 + 2 * s.Upkeep.YearlyUpkeep - 1; // paying would leave less than two years of upkeep
            s.Resources.SetSpiritStones(short_);
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Fang", 500, s.Clock.Year, DemandKind.Protection) }, null);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Intrigues.Demands);
            Assert.AreEqual(short_, s.Resources.SpiritStones, "it could not keep its reserve: it refused");
        }

        [Test]
        public void ThePilot_AnswersAChallenge_AndFightsItOut()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Challenges.Issue(s.Factions.GetFactionByName("Famille Ruan"));
            ChallengeOutcome? outcome = null;
            s.Events.OnChallengeSettled += (_, o) => outcome = o;
            BalanceRun.Act(s);
            Assert.IsNull(s.Challenges.Pending);
            Assert.IsNull(s.Challenges.Current);
            Assert.AreNotEqual(ChallengeOutcome.Declined, outcome, "a challenge of its own rank is fought");
        }

        [Test]
        public void ThePilot_SuesForPeace_InALongWar()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            Assert.IsNull(s.Wars.DeclareOn("Famille Ruan"));
            s.Context.Clock.Restore(s.Clock.Year + 3, s.Clock.Phase);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Wars.ClanWars);
        }

        [Test]
        public void ThePilot_PlantsATalismanSeed_WhenTheMirrorCan()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            var mortal = Fixtures.Mortal(age: 14);
            s.Clan.AddMember(mortal);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.HasTalismanSeed));
        }

        [Test]
        public void ALongGame_WithTheActivePilot_StaysSane([Values(1, 2, 3)] int seed)
        {
            var run = BalanceRun.Play(Fixtures.Content, seed, years: 150, out _, autopilot: true);
            Assert.That(run.Betrayals, Is.LessThanOrEqualTo(12), "a treaty is betrayed for a reason, not as a matter of course");
            Assert.That(run.CombatDeaths, Is.LessThanOrEqualTo(run.Challenges + run.ClanWars), "a challenge by the rules seldom kills");
            Assert.That(run.Devoured, Is.LessThanOrEqualTo(3), "a prudent clan keeps most of its ripe Daos");
        }

        /// <summary>The report behind the tuning: <c>./Scripts/dev.sh balance</c> (seeds × years, env BALANCE_SEEDS/BALANCE_YEARS).</summary>
        [Test, Explicit, Category("Balance")]
        public void Report()
        {
            int seeds = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_SEEDS"), out var s) ? s : 10;
            int years = int.TryParse(Environment.GetEnvironmentVariable("BALANCE_YEARS"), out var y) ? y : 150;
            bool autopilot = Environment.GetEnvironmentVariable("BALANCE_AUTOPILOT") != "0";
            var runs = Enumerable.Range(1, seeds).Select(seed => BalanceRun.Play(Fixtures.Content, seed, years, out _, autopilot)).ToList();
            TestContext.Progress.WriteLine(BalanceRun.Table(runs));
        }
    }
}
