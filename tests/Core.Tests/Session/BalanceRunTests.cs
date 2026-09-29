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
        public void ThePilot_SendsCultivatorsToTheMine_WhenTheCoffersRunLow()
        {
            var session = GameSession.NewGame(Fixtures.Setup(3));
            foreach (var m in session.Clan.LivingMembers.Where(m => !SpiritualOrificeRules.CanCultivate(m)).ToList())
                session.Clan.Kill(m, DeathCause.Illness); // a clan of cultivators only
            session.Resources.SetSpiritStones(0);
            BalanceRun.SetTheIdleToWork(session);
            Assert.IsTrue(session.Clan.LivingMembers.Any(m => m.CurrentTask == TaskType.Mine), "someone must feed the clan");
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
            Assert.IsFalse(s.Wars.ClanWars.Any(w => w.Enemy == "Famille Ruan"), "paid: no war");

            int short_ = 500 + 2 * s.Upkeep.YearlyUpkeep - 1; // paying would leave less than two years of upkeep
            s.Resources.SetSpiritStones(short_);
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Fang", 500, s.Clock.Year, DemandKind.Protection) }, null);
            BalanceRun.Act(s);
            Assert.IsEmpty(s.Intrigues.Demands);
            Assert.IsTrue(s.Wars.ClanWars.Any(w => w.Enemy == "Famille Fang"), "it could not keep its reserve: it refused, and war came");
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
            Assert.That(run.CombatDeaths, Is.LessThanOrEqualTo(run.Challenges + run.ClanWars + run.Hunts), "a challenge by the rules seldom kills (a failed hunt may)");
            Assert.That(run.Devoured, Is.LessThanOrEqualTo(3), "a prudent clan keeps most of its ripe Daos");
            Assert.That(run.Strikes, Is.LessThanOrEqualTo(40), "no chain reaction of blows");
            Assert.That(run.ClanWars, Is.LessThanOrEqualTo(10), "no endless wars against the clan");
        }

        // ---- The pilot hunts (2026-09-29) ----

        private static GameSession InTheHuntWindow()
        {
            var s = GameSession.NewGame(Fixtures.Setup(3));
            s.Talismans.RestoreCalendar(s.Clock.Year); // the ritual's year: the window is open
            return s;
        }

        [Test]
        public void ThePilot_Scouts_WhenItKnowsNoBeast()
        {
            var s = InTheHuntWindow();
            Assume.That(s.Bestiary.Beasts.Any(b => s.Knowledge.Knows(MirrorChronicles.World.FactKind.Beast, b.Id)), Is.False);
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.CurrentTask == TaskType.ScoutBeasts));
        }

        [Test]
        public void ThePilot_HuntsABeastItCanTake()
        {
            var s = InTheHuntWindow();
            int strongest = s.Clan.LivingMembers.Where(s.Hunts.IsFree).Max(m => MirrorChronicles.Mirror.HuntRules.Power(m.Realm, m.RealmStage));
            var prey = s.Bestiary.Beasts.First(b => MirrorChronicles.Mirror.HuntRules.Power(b.Realm, b.Stage) < strongest);
            s.Knowledge.Reveal(MirrorChronicles.World.FactKind.Beast, prey.Id, MirrorChronicles.World.KnowledgeSource.Studied);
            bool? taken = null;
            s.Events.OnHunt += (_, captured) => taken = captured;
            BalanceRun.Act(s);
            Assert.IsNotNull(taken, "a hunt was carried out");
        }

        [Test]
        public void AMemberAwayOnAnOperation_TakesNoOtherTask()
        {
            var s = InTheHuntWindow();
            var hunter = s.Clan.LivingMembers.First(s.Hunts.IsFree);
            hunter.CurrentTask = TaskType.HuntBeast; // sent out this year
            Assert.IsFalse(s.Tasks.AssignTask(hunter, TaskType.Mine), "away for the year");
            Assert.AreEqual(TaskType.HuntBeast, hunter.CurrentTask);
        }

        [Test]
        public void TheChronicle_TellsAHunt()
        {
            var s = InTheHuntWindow();
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.Events.TriggerHunt("a-beast", true);
            StringAssert.Contains("bête", chronicle.Entries.Last());
        }

        [Test]
        public void ThePilot_OffersItsBeast_AndTakesATalisman()
        {
            var s = InTheHuntWindow();
            s.Resources.AddPrayers(s.Context.Content.Balance.Talismans.PrayersPerRitual);
            s.Resources.AddBeast(new CapturedBeast("taken", CultivationRealm.QiRefinement, 5, null));
            BalanceRun.Act(s);
            Assert.IsTrue(s.Clan.LivingMembers.Any(m => m.TalismanQiId != null), "the ritual performed, a talisman chosen");
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
