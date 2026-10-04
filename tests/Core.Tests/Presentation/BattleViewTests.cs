using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The battle screen (G6): the grid and who stands where, each fighter's vitality and Qi, the actions open to the
    /// clan's fighter whose turn it is with the cells they may target, the outcome; and a pending challenge with the
    /// members who may answer it.
    /// </summary>
    [TestFixture]
    public class BattleViewTests
    {
        private static CharacterData Fighter(string id, CultivationRealm realm)
        {
            var fighter = Fixtures.Cultivator(realm: realm);
            fighter.ID = id;
            return fighter;
        }

        private static Battle Start() =>
            Battle.Start(new[] { Fighter("ally", CultivationRealm.Foundation) }, new[] { Fighter("foe", CultivationRealm.QiRefinement) },
                new Random(1), new RecordingGameLog(), new CombatGrid(10, 10));

        private static MirrorChronicles.Data.TechniqueData None(string id) => null;

        [Test]
        public void TheGrid_ShowsEveryCell_AndWhoStandsThere()
        {
            var battle = Start();
            var cells = BattleView.Cells(battle);
            Assert.AreEqual(100, cells.Count);
            var ally = battle.Allies.Single().CurrentCell;
            var cell = cells.Single(c => c.X == ally.X && c.Y == ally.Y);
            Assert.AreEqual("ally", cell.UnitId);
            Assert.IsTrue(cell.Ally);
        }

        [Test]
        public void TheFighters_ShowTheirVitality_AndWhoseTurnItIs()
        {
            var battle = Start();
            var units = BattleView.Units(battle);
            var ally = units.Single(u => u.Id == "ally");
            Assert.AreEqual((battle.Allies[0].MaxVitality, battle.Allies[0].MaxVitality), (ally.Vitality, ally.MaxVitality));
            Assert.AreEqual(battle.CurrentUnit.BaseData.ID, units.Single(u => u.IsCurrent).Id);
        }

        [Test]
        public void TheActions_OfTheFighterWhoseTurnItIs_NameTheirTargets()
        {
            var battle = Start();
            Assert.AreEqual(CombatState.PlayerTurn, battle.State);
            var actions = BattleView.Actions(battle, None);
            var move = actions.Single(a => a.Id == BattleView.Move);
            Assert.IsNotEmpty(move.Targets);
            Assert.IsEmpty(actions.Single(a => a.Id == BattleView.Attack).Targets, "the foe is far");
            Assert.IsTrue(actions.Any(a => a.Id == BattleView.Defend));
        }

        [Test]
        public void Performing_AnActionOnATarget_MovesTheFighter()
        {
            var battle = Start();
            var target = BattleView.Actions(battle, None).Single(a => a.Id == BattleView.Move).Targets.First();
            Assert.IsTrue(BattleView.Perform(battle, BattleView.Move, target.X, target.Y, None));
            Assert.AreEqual((target.X, target.Y), (battle.Allies[0].CurrentCell.X, battle.Allies[0].CurrentCell.Y));
            Assert.IsFalse(BattleView.Perform(battle, "nonsense", 0, 0, None));
        }

        [Test]
        public void TheOutcome_IsToldOnceTheBattleIsOver()
        {
            var battle = Start();
            Assert.IsNull(BattleView.Outcome(battle));
            battle.AutoPlay();
            Assert.AreEqual("Victoire", BattleView.Outcome(battle));
        }

        [Test]
        public void APendingChallenge_ShowsTheMembersWhoMayAnswer()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            Assert.IsNull(BattleView.Pending(s));
            s.Challenges.Issue(s.Factions.GetFactionByName("Famille Ruan"));
            var pending = BattleView.Pending(s);
            Assert.AreEqual("Famille Ruan", pending.Faction);
            Assert.IsTrue(pending.Candidates.Any(c => c.Refusal == null));
            Assert.AreEqual(s.Clan.LivingMembers.Count, pending.Candidates.Count);
        }
    }
}
