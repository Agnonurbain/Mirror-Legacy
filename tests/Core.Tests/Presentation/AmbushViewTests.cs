using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// An ambush on the battle screen (G6): who sprang it, on whom, the agents' rank and number, and the members away who
    /// will fight; the chronicle tells how it ended.
    /// </summary>
    [TestFixture]
    public class AmbushViewTests
    {
        private const string Ruan = "Famille Ruan";

        private static (GameSession s, CharacterData target) Ambushed()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent, InteractiveBattles = true });
            var target = s.Clan.LivingMembers.First(m => s.Challenges.FighterRefusal(m) == null);
            target.CurrentTask = TaskType.Diplomacy;
            s.Ambushes.Restore(new Ambush(Ruan, s.Clock.Year, target.ID, CultivationRealm.QiRefinement, 1, 2, 7));
            return (s, target);
        }

        [Test]
        public void Ambush_IsNull_WhenNoneAwaits()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            Assert.IsNull(BattleView.Ambush(s));
        }

        [Test]
        public void Ambush_TellsWhoSprangIt_OnWhom()
        {
            var (s, target) = Ambushed();
            var view = BattleView.Ambush(s);
            Assert.AreEqual((Ruan, target.FullName), (view.Faction, view.Target));
        }

        [Test]
        public void Ambush_TellsTheAgents()
        {
            var (s, _) = Ambushed();
            var view = BattleView.Ambush(s);
            Assert.AreEqual((2, RankCatalog.RealmName(CultivationRealm.QiRefinement)), (view.Agents, view.Rank));
        }

        [Test]
        public void Ambush_ShowsTheMembersWhoWillFight()
        {
            var (s, target) = Ambushed();
            CollectionAssert.AreEqual(new[] { target.FullName }, BattleView.Ambush(s).Fighters);
        }

        [Test]
        public void TheChronicle_TellsAnAmbushBeatenOff()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            var chronicle = new Chronicle(s);
            s.Events.TriggerAmbushSettled(Ruan, AmbushOutcome.Repelled);
            StringAssert.Contains("embuscade", chronicle.Entries.Last());
        }
    }
}
