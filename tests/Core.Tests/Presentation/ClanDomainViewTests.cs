using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>What the clan domain screen shows: the turn, the treasury and the roster.</summary>
    [TestFixture]
    public class ClanDomainViewTests
    {
        private static GameSession NewGame() =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void Header_ShowsTheTurnTheTreasuryAndTheMirror()
        {
            var header = ClanDomainView.Header(NewGame());
            Assert.AreEqual(new DomainHeader(1, "Gestion", 1000, 50, 1), header);
        }

        [Test]
        public void Roster_PutsThePatriarchFirst()
        {
            var s = NewGame();
            var first = ClanDomainView.Roster(s)[0];
            Assert.IsTrue(first.IsPatriarch && first.Id == s.Clan.PatriarchID);
        }

        [Test]
        public void Roster_ListsOnlyTheLiving()
        {
            var s = NewGame();
            s.Clan.Kill(s.Clan.LivingMembers.Last(), DeathCause.Illness);
            Assert.AreEqual(4, ClanDomainView.Roster(s).Count);
        }

        [Test]
        public void Roster_ShowsTheRankName()
        {
            Assert.AreEqual("Culture du Qi — 3e niveau (début)", ClanDomainView.Roster(NewGame())[0].Rank);
        }

        [Test]
        public void Roster_OffersOnlyTheTasksAMemberMayTake()
        {
            var s = NewGame();
            var embryonic = ClanDomainView.Roster(s).First(r => r.Rank.StartsWith("Respiration Embryonnaire"));
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Cultivation, TaskType.Rest }, embryonic.AllowedTasks);
        }

        [Test]
        public void PhaseLabel_NamesEveryPhaseDifferently()
        {
            var labels = Enum.GetValues(typeof(GamePhase)).Cast<GamePhase>().Select(ClanDomainView.PhaseLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        [Test]
        public void TaskLabel_NamesEveryTaskDifferently()
        {
            var labels = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().Select(ClanDomainView.TaskLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        [Test]
        public void DeathLabel_NamesEveryCause()
        {
            var causes = Enum.GetValues(typeof(DeathCause)).Cast<DeathCause>().Where(c => c != DeathCause.None);
            Assert.IsTrue(causes.All(c => !string.IsNullOrWhiteSpace(ClanDomainView.DeathLabel(c))));
        }
    }
}
