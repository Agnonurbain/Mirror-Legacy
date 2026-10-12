using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// A war's battle on the battle screen (G6): the enemy, its leader's and soldiers' ranks, how many soldiers take the field,
    /// how many allies came, who may fight and how many, the battles still waiting; the chronicle tells how it ended.
    /// </summary>
    [TestFixture]
    public class WarBattleViewTests
    {
        private const string Ruan = "Famille Ruan";

        private static GameSession Waiting()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent, InteractiveBattles = true });
            s.WarBattles.Restore(new[]
            {
                new PendingWarBattle(Ruan, s.Clock.Year, CultivationRealm.Foundation, CultivationRealm.QiRefinement, 3, 1, 7),
                new PendingWarBattle("Famille Lou", s.Clock.Year, CultivationRealm.QiRefinement, CultivationRealm.Embryonic, 0, 0, 9)
            });
            return s;
        }

        [Test]
        public void War_IsNull_WhenNoBattleWaits()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            Assert.IsNull(BattleView.War(s));
        }

        [Test]
        public void War_TellsTheEnemy_AndItsRanks()
        {
            var view = BattleView.War(Waiting());
            Assert.AreEqual((Ruan, RankCatalog.RealmName(CultivationRealm.Foundation), RankCatalog.RealmName(CultivationRealm.QiRefinement)),
                (view.Enemy, view.LeaderRank, view.SoldierRank));
        }

        [Test]
        public void War_TellsTheSoldiersOnTheField_AndTheAlliesWhoCame()
        {
            var view = BattleView.War(Waiting());
            Assert.AreEqual((2, 1), (view.Soldiers, view.AlliesCame));
        }

        [Test]
        public void War_TellsTheBattlesStillWaiting()
        {
            Assert.AreEqual(1, BattleView.War(Waiting()).StillWaiting);
        }

        [Test]
        public void War_ShowsEveryMember_WithWhyTheyMayNotFight()
        {
            var s = Waiting();
            Assert.AreEqual(s.Clan.LivingMembers.Count, BattleView.War(s).Candidates.Count);
        }

        [Test]
        public void TheChronicle_TellsAWarsBattleWon()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            var chronicle = new Chronicle(s);
            s.Events.TriggerWarBattleSettled(Ruan, WarBattleOutcome.Won);
            StringAssert.Contains(Ruan, chronicle.Entries.Last());
        }
    }
}
