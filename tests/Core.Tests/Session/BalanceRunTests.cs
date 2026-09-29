using System;
using System.Linq;
using NUnit.Framework;
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
        public void AnAutopilot_SetsTheIdleToWork()
        {
            BalanceRun.Play(Fixtures.Content, seed: 3, years: 3, out var session, autopilot: true);
            var free = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            Assert.IsTrue(free.Any(m => m.CurrentTask == MirrorChronicles.Data.TaskType.Cultivation), "those who can, cultivate");
            Assert.IsTrue(free.All(m => m.CurrentTask != MirrorChronicles.Data.TaskType.None
                || !session.Tasks.AssignTask(m, MirrorChronicles.Data.TaskType.Mine)), "nobody idle who could work");
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
