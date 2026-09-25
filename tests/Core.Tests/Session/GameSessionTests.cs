using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>A game session: founding the clan, and the yearly turn in a fixed order.</summary>
    [TestFixture]
    public class GameSessionTests
    {
        /// <summary>No random events, so a test sees only what it provokes.</summary>
        private static GameSession Quiet(int seed = 1) =>
            GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });

        [Test]
        public void NewGame_FoundsTheClanWithFiveExaminedCultivators()
        {
            var s = Quiet();
            Assert.IsTrue(s.Clan.LivingMembers.Count == 5
                && s.Clan.LivingMembers.All(m => m.OrificeKnown && SpiritualOrificeRules.CanCultivate(m)));
        }

        [Test]
        public void NewGame_NamesTheClanFromTheLexicon()
        {
            var s = Quiet();
            Assert.IsTrue(s.Clan.ClanName == "Mo" && s.Clan.LivingMembers.All(m => m.LastName == "Mo"));
        }

        [Test]
        public void NewGame_AppointsTheQiCultivatorFounderAsPatriarch()
        {
            var patriarch = Quiet().Clan.GetPatriarch();
            Assert.IsTrue(patriarch.Realm == CultivationRealm.QiRefinement && patriarch.RealmStage == 3);
        }

        [Test]
        public void NewGame_StartsInYearOneManagement()
        {
            var s = Quiet();
            Assert.IsTrue(s.Clock.Year == 1 && s.Clock.Phase == GamePhase.Management);
        }

        [Test]
        public void NewGame_KnowsTheWorldAndHoldsTwoFragments()
        {
            var s = Quiet();
            Assert.IsTrue(s.Factions.Factions.Count == Fixtures.Content.Factions.Count && s.Deduction.Fragments.Count == 2);
        }

        [Test]
        public void AdvancePhase_WalksThroughTheYear()
        {
            var s = Quiet();
            var seen = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                s.AdvancePhase();
                seen.Add($"{s.Clock.Year}:{s.Clock.Phase}");
            }
            CollectionAssert.AreEqual(new[] { "1:Events", "1:Breakthrough", "1:Inheritance", "2:Management" }, seen);
        }

        [Test]
        public void AdvancePhase_AnnouncesEveryPhase()
        {
            var s = Quiet();
            int announced = 0;
            s.Events.OnPhaseChanged += p => announced++;
            s.AdvanceYear();
            Assert.AreEqual(4, announced);
        }

        [Test]
        public void EnteringTheEventsPhase_ResolvesTheTasks()
        {
            var s = Quiet();
            var patriarch = s.Clan.GetPatriarch();
            s.Tasks.AssignTask(patriarch, TaskType.Rest);
            int before = patriarch.MentalStability;
            s.AdvancePhase();
            Assert.AreEqual(before + TaskAssignmentSystemRest, patriarch.MentalStability);
        }

        [Test]
        public void AdvanceYear_AgesEveryFounderOnce()
        {
            var s = Quiet();
            var founders = s.Clan.LivingMembers.ToDictionary(m => m, m => m.Age);
            s.AdvanceYear();
            Assert.IsTrue(founders.All(f => f.Key.Age == f.Value + 1));
        }

        [Test]
        public void AdvanceYear_RechargesTheMirror()
        {
            var s = Quiet();
            s.AdvanceYear();
            Assert.AreEqual(51, s.Mirror.MirrorPower);
        }

        [Test]
        public void AdvancePhase_DoesNothing_OnceTheGameIsOver()
        {
            var s = Quiet();
            foreach (var m in s.Clan.LivingMembers.ToList()) s.Clan.Kill(m, DeathCause.Illness);
            s.AdvancePhase();
            Assert.IsTrue(s.Victory.GameLost && s.Clock.Phase == GamePhase.Management && s.Clock.Year == 1);
        }

        private const int TaskAssignmentSystemRest = MirrorChronicles.Economy.TaskAssignmentSystem.RestStability;
    }
}
