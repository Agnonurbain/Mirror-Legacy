using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>
    /// The Autumn Convergence Pill (AUDIT_LORE.md §2.9, 📚 wiki: « replenishes mana, greatly enhances spell arts, at the cost of
    /// some three years of lifespan; the maximum safe intake is three; beyond, the Immortal Foundation may collapse »).
    /// </summary>
    [TestFixture]
    public class ConvergencePillTests
    {
        private static ConvergenceSettings C => Fixtures.Content.Balance.Arts.Convergence;

        private static (BattleField Field, CombatUnit Unit) Fighter(Random rng = null, int pills = 10)
        {
            var field = new BattleField(new CombatGrid(10, 10), rng ?? new Random(1), new RecordingGameLog()) { Convergence = C };
            var unit = CombatFixtures.Place(field, 0, 0, realm: CultivationRealm.Foundation);
            unit.BaseData.MaxLifespan = 200;
            int left = pills;
            unit.TakeConvergencePill = () => left-- > 0;
            return (field, unit);
        }

        [Test]
        public void ThePill_FillsTheQi_StrengthensTheSpells_AndCostsYearsOfLife()
        {
            var (field, unit) = Fighter();
            unit.ConsumeQi(unit.CurrentQi);
            var pill = new ConvergencePillAction();
            Assert.IsTrue(pill.IsValid(unit, unit.CurrentCell, field));
            pill.Execute(unit, unit.CurrentCell, field);
            Assert.AreEqual(unit.MaxQi, unit.CurrentQi);
            Assert.AreEqual(1 + C.SpellBoost, unit.SpellFactor);
            Assert.AreEqual(200 - C.LifespanCost, unit.BaseData.MaxLifespan);
            Assert.AreEqual(1, unit.ConvergenceDoses);
            Assert.IsTrue(unit.HasActedThisTurn);
        }

        [Test]
        public void WithoutAPill_OrAnotherCell_ItCannotBeTaken()
        {
            var (field, unit) = Fighter(pills: 0);
            var pill = new ConvergencePillAction();
            unit.TakeConvergencePill = null;
            Assert.IsFalse(pill.IsValid(unit, unit.CurrentCell, field));
            var (field2, unit2) = Fighter();
            Assert.IsFalse(pill.IsValid(unit2, field2.Grid.GetCellAt(3, 3), field2));
        }

        [Test]
        public void BeyondTheSafeDose_TheFoundationMayCollapse()
        {
            var (field, unit) = Fighter(new SequenceRandom(0.0)); // every collapse roll falls
            var pill = new ConvergencePillAction();
            for (int i = 0; i < C.SafeDoses; i++)
            {
                unit.HasActedThisTurn = false;
                pill.Execute(unit, unit.CurrentCell, field);
            }
            Assert.IsFalse(unit.IsDown, "the safe doses hold");
            unit.HasActedThisTurn = false;
            pill.Execute(unit, unit.CurrentCell, field);
            Assert.IsTrue(unit.IsDown, "one dose too many: the foundation collapses");
            Assert.AreEqual(200 - (C.SafeDoses + 1) * C.LifespanCost - C.CollapseLifespan, unit.BaseData.MaxLifespan);
        }

        [Test]
        public void AStrengthenedSpell_StrikesHarder()
        {
            var strike = new TechniqueData { ID = "bolt", Kind = TechniqueKind.Spell, Effect = TechniqueEffect.Strike, PowerModifier = 30, Range = 3, QiCost = 1 };
            int Hit(bool pill)
            {
                var field = new BattleField(new CombatGrid(10, 10), new Random(1), new RecordingGameLog(), id => id == "bolt" ? strike : null) { Convergence = C };
                var unit = CombatFixtures.Place(field, 0, 0, realm: CultivationRealm.Foundation);
                unit.BaseData.KnownTechniqueIDs.Add("bolt");
                unit.BaseData.MaxLifespan = 200;
                unit.TakeConvergencePill = () => true;
                var foe = CombatFixtures.Place(field, 1, 0, realm: CultivationRealm.Foundation, isAlly: false);
                if (pill) { new ConvergencePillAction().Execute(unit, unit.CurrentCell, field); unit.HasActedThisTurn = false; }
                new TechniqueAction(strike).Execute(unit, foe.CurrentCell, field);
                return foe.MaxVitality - foe.CurrentVitality;
            }
            Assert.Greater(Hit(pill: true), Hit(pill: false));
        }

        [Test]
        public void AFaltering_Rival_SwallowsItsPill()
        {
            var field = new BattleField(new CombatGrid(10, 10), new Random(1), new RecordingGameLog()) { Convergence = C };
            var rival = CombatFixtures.Place(field, 9, 9, realm: CultivationRealm.Foundation, isAlly: false);
            rival.BaseData.MaxLifespan = 200;
            CombatFixtures.Place(field, 0, 0, realm: CultivationRealm.Foundation);
            int pills = 1;
            rival.TakeConvergencePill = () => pills-- > 0;
            rival.TakeDamage((int)(rival.MaxVitality * (1 - C.AiUseBelow)) + 1);
            CombatAI.PlayTurn(rival, field);
            Assert.AreEqual(1, rival.ConvergenceDoses);
        }

        [Test]
        public void InAChallenge_TheClanDrawsOnItsStore_AndARivalOfAnAlchemyPower_CarriesOne()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var power = s.Factions.Factions.First(p => MirrorChronicles.Characters.PowerArts.Knows(p, ImmortalArt.Alchemy, s.Context.Content));
            s.Alchemy.GainPills(PillKind.AutumnConvergence, 1);
            Assert.IsNotNull(s.Challenges.Issue(power));
            var fighter = s.Clan.LivingMembers.First(m => s.Challenges.FighterRefusal(m) == null);
            Assert.IsNull(s.Challenges.Accept(new[] { fighter.ID }));
            var battle = s.Challenges.Current;
            Assert.IsTrue(battle.Enemies.All(u => u.TakeConvergencePill != null), "its alchemists armed it");
            var ally = battle.Allies.Single();
            Assert.IsTrue(ally.TakeConvergencePill());
            Assert.AreEqual(0, s.Alchemy.PillsOf(PillKind.AutumnConvergence));
            Assert.IsFalse(ally.TakeConvergencePill(), "the store is empty");
        }

        [Test]
        public void TheBattleScreen_OffersThePill_ToTheClansFighter()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Alchemy.GainPills(PillKind.AutumnConvergence, 2);
            s.Challenges.Issue(s.Factions.Factions.First());
            s.Challenges.Accept(new[] { s.Clan.LivingMembers.First(m => s.Challenges.FighterRefusal(m) == null).ID });
            var battle = s.Challenges.Current;
            if (battle.IsOver) Assert.Inconclusive("the rivals ended it before the clan's turn");
            var line = MirrorChronicles.Presentation.BattleView.Actions(battle, s.Techniques.Find)
                .Single(a => a.Id == MirrorChronicles.Presentation.BattleView.Pill);
            StringAssert.Contains("Convergence d'Automne", line.Label);
            Assert.AreEqual(1, line.Targets.Count, "on oneself");
            Assert.IsTrue(MirrorChronicles.Presentation.BattleView.Perform(battle, line.Id, 0, 0, s.Techniques.Find));
            Assert.AreEqual(1, s.Alchemy.PillsOf(PillKind.AutumnConvergence));
        }
    }
}
