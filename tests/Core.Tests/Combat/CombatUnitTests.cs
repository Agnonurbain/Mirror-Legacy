using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>A fighter on the grid: stats from the realm, vitality, Qi, defence and flight.</summary>
    [TestFixture]
    public class CombatUnitTests
    {
        [TestCase(CultivationRealm.Embryonic, 100, 10, 10, 5)]
        [TestCase(CultivationRealm.QiRefinement, 150, 15, 15, 8)]
        [TestCase(CultivationRealm.Foundation, 200, 20, 20, 11)]
        public void Stats_GrowWithTheRealm(CultivationRealm realm, int vitality, int agility, int strength, int defense)
        {
            var u = CombatFixtures.Unit(realm);
            Assert.IsTrue(u.MaxVitality == vitality && u.Agility == agility && u.Strength == strength && u.Defense == defense);
        }

        [Test]
        public void Qi_IsTheRootTimesTheRealmPlusOne()
        {
            Assert.AreEqual(120, CombatFixtures.Unit(CultivationRealm.Foundation, root: 40).MaxQi);
        }

        [Test]
        public void TakeDamage_IsHalved_WhenDefending()
        {
            var u = CombatFixtures.Unit();
            u.IsDefending = true;
            u.TakeDamage(13);
            Assert.AreEqual(94, u.CurrentVitality);
        }

        [Test]
        public void TakeDamage_BringsTheUnitDownAndOffTheGrid_AtZero()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 2, 2);
            u.TakeDamage(500);
            Assert.IsTrue(u.CurrentVitality == 0 && u.IsDown && !u.IsActive && !field.Grid.GetCellAt(2, 2).IsOccupied);
        }

        [Test]
        public void Heal_NeverExceedsTheMaximum()
        {
            var u = CombatFixtures.Unit();
            u.TakeDamage(10);
            u.Heal(50);
            Assert.AreEqual(u.MaxVitality, u.CurrentVitality);
        }

        [Test]
        public void ConsumeQi_Refuses_WhenShort()
        {
            var u = CombatFixtures.Unit(root: 10);
            Assert.IsTrue(!u.ConsumeQi(11) && u.CurrentQi == 10);
        }

        [Test]
        public void Flee_LeavesTheBattleAlive()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 2, 2);
            u.Flee();
            Assert.IsTrue(u.HasFled && !u.IsDown && !u.IsActive && u.CurrentCell == null);
        }

        [Test]
        public void ResetTurnState_RestoresFivePercentQi_OnConcentratedQi()
        {
            var field = CombatFixtures.Field();
            field.Grid.SetTerrain(3, 3, TerrainType.ConcentratedQi);
            var u = CombatFixtures.Place(field, 3, 3, CultivationRealm.QiRefinement, root: 50); // 100 Qi
            u.ConsumeQi(20);
            u.ResetTurnState();
            Assert.AreEqual(85, u.CurrentQi);
        }
    }
}
