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
        public void NewGame_FoundsTheNovelsFamily_OfMortalPeasants()
        {
            // 📚 the novel's start (audit §4.3): a former soldier of sixty, his wife, four sons — the third, thirteen, finds the mirror
            var s = Quiet();
            Assert.AreEqual(6, s.Clan.LivingMembers.Count);
            Assert.IsTrue(s.Clan.LivingMembers.All(m => m.OrificeKnown && !SpiritualOrificeRules.CanCultivate(m)), "mortals, as the mirror sees them");
            var sons = s.Clan.LivingMembers.Where(m => m.FatherID == s.Clan.PatriarchID).OrderByDescending(m => m.Age).ToList();
            Assert.AreEqual(4, sons.Count);
            Assert.IsTrue(sons.All(m => m.IsMale));
            Assert.AreEqual(("Jian", 13), (sons[2].FirstName, sons[2].Age), "Mo Jian, the third son, thirteen");
        }

        [Test]
        public void NewGame_NamesTheClanFromTheLexicon()
        {
            var s = Quiet();
            Assert.IsTrue(s.Clan.ClanName == "Mo" && s.Clan.LivingMembers.All(m => m.LastName == "Mo"));
        }

        [Test]
        public void NewGame_AppointsTheOldSoldier_AsPatriarch()
        {
            var patriarch = Quiet().Clan.GetPatriarch();
            Assert.AreEqual(("Wei", 60), (patriarch.FirstName, patriarch.Age));
            Assert.IsFalse(patriarch.HasSpiritualOrifice, "never a cultivator himself");
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
        public void NewGame_KnowsTheMirrorsMethods_ButHoldsNoQi()
        {
            var s = Quiet();
            Assert.IsTrue(s.Techniques.Knows("clear-spring-sutra") && s.Techniques.Knows("common-breath-method"), "the mirror's gift");
            Assert.AreEqual(0, s.Resources.QiPortions("clear-spring-qi"), "peasants hold no spiritual Qi");
        }

        [Test]
        public void TheMirror_MaySeedTheSons_FromTheFirstYear()
        {
            var s = Quiet();
            s.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            var candidates = MirrorChronicles.Presentation.MirrorView.SeedCandidates(s);
            Assert.IsTrue(candidates.Any(c => c.Refusal == null), "the mirror sees who it may seed");
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
            int before = s.Mirror.MirrorPower;
            s.AdvanceYear();
            Assert.AreEqual(System.Math.Min(s.Mirror.Cap, before + s.Mirror.Tier.MoonlightPerYear), s.Mirror.MirrorPower);
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
