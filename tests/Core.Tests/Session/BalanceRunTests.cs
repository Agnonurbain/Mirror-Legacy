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
