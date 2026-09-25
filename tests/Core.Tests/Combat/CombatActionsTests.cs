using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>Move, attack, defend, flee, items and techniques (command pattern).</summary>
    [TestFixture]
    public class CombatActionsTests
    {
        private static TechniqueData Technique(TechniqueType type, int power = 20, Element element = Element.Fire,
            int range = 2, int qiCost = 10, CultivationRealm required = CultivationRealm.Embryonic) =>
            new TechniqueData { Type = type, PowerModifier = power, DominantElement = element, Range = range, QiCost = qiCost, RequiredRealm = required };

        private static CombatUnit Knowing(CombatUnit unit, TechniqueData technique)
        {
            unit.BaseData.KnownTechniqueIDs.Add(technique.ID);
            return unit;
        }

        // ---- Move ----

        [Test]
        public void Move_ReachesACellWithinItsRange()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0, CultivationRealm.Foundation); // agility 20: two cells
            new MoveAction().Execute(u, field.Grid.GetCellAt(1, 1), field);
            Assert.AreEqual(field.Grid.GetCellAt(1, 1), u.CurrentCell);
        }

        [Test]
        public void Move_IsInvalid_BeyondItsRange()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0); // agility 10: one cell
            Assert.IsFalse(new MoveAction().IsValid(u, field.Grid.GetCellAt(2, 0), field));
        }

        [Test]
        public void Move_IsInvalid_OntoAnOccupiedCell()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            CombatFixtures.Place(field, 1, 0, isAlly: false);
            Assert.IsFalse(new MoveAction().IsValid(u, field.Grid.GetCellAt(1, 0), field));
        }

        [Test]
        public void Move_IsInvalid_WhenTheTerrainCostsMoreThanItsRange()
        {
            var field = CombatFixtures.Field(width: 3, height: 1);
            field.Grid.SetTerrain(1, 0, TerrainType.Mountain);
            var u = CombatFixtures.Place(field, 0, 0, CultivationRealm.Foundation); // two movement points
            Assert.IsFalse(new MoveAction().IsValid(u, field.Grid.GetCellAt(2, 0), field)); // mountain 2 + plain 1
        }

        [Test]
        public void Move_IsInvalid_ThroughAnotherUnit()
        {
            var field = CombatFixtures.Field(width: 3, height: 1);
            var u = CombatFixtures.Place(field, 0, 0, CultivationRealm.Foundation);
            CombatFixtures.Place(field, 1, 0, isAlly: false);
            Assert.IsFalse(new MoveAction().IsValid(u, field.Grid.GetCellAt(2, 0), field));
        }

        [Test]
        public void Move_AlwaysAllowsASingleStep_EvenUphill()
        {
            var field = CombatFixtures.Field();
            field.Grid.SetTerrain(1, 0, TerrainType.Mountain);
            var u = CombatFixtures.Place(field, 0, 0); // one movement point, the mountain costs two
            Assert.IsTrue(new MoveAction().IsValid(u, field.Grid.GetCellAt(1, 0), field));
        }

        [Test]
        public void Move_HappensOncePerTurn()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            new MoveAction().Execute(u, field.Grid.GetCellAt(1, 0), field);
            Assert.IsFalse(new MoveAction().IsValid(u, field.Grid.GetCellAt(2, 0), field));
        }

        // ---- Attack ----

        [Test]
        public void Attack_DealsStrengthByRealmMinusDefence()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement); // 15 × 1.2 = 18
            var foe = CombatFixtures.Place(field, 1, 0, isAlly: false);                // defence 5
            new AttackAction().Execute(u, foe.CurrentCell, field);
            Assert.AreEqual(100 - 13, foe.CurrentVitality);
        }

        [Test]
        public void Attack_AlwaysDealsAtLeastOne()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 1, 0, CultivationRealm.Foundation, isAlly: false);
            new AttackAction().Execute(u, foe.CurrentCell, field);
            Assert.AreEqual(foe.MaxVitality - 1, foe.CurrentVitality);
        }

        [Test]
        public void Attack_IsInvalid_AgainstAnAlly()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            var friend = CombatFixtures.Place(field, 1, 0);
            Assert.IsFalse(new AttackAction().IsValid(u, friend.CurrentCell, field));
        }

        [Test]
        public void Attack_IsInvalid_FromAfar()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 2, 0, isAlly: false);
            Assert.IsFalse(new AttackAction().IsValid(u, foe.CurrentCell, field));
        }

        // ---- Defend ----

        [Test]
        public void Defend_HalvesTheNextBlows()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            new DefendAction().Execute(u, u.CurrentCell, field);
            Assert.IsTrue(u.IsDefending && u.HasActedThisTurn);
        }

        // ---- Flee ----

        [Test]
        public void Flee_Succeeds_WhenTheRollBeatsTheOdds()
        {
            var field = CombatFixtures.Field(new FixedRandom(0.0));
            var u = CombatFixtures.Place(field, 0, 0);
            CombatFixtures.Place(field, 5, 5, isAlly: false);
            new FleeAction().Execute(u, null, field);
            Assert.IsTrue(u.HasFled);
        }

        [Test]
        public void Flee_CostsTheTurn_WhenItFails()
        {
            var field = CombatFixtures.Field(new FixedRandom(0.99));
            var u = CombatFixtures.Place(field, 0, 0);
            CombatFixtures.Place(field, 5, 5, isAlly: false);
            new FleeAction().Execute(u, null, field);
            Assert.IsTrue(!u.HasFled && u.HasActedThisTurn && u.HasMovedThisTurn);
        }

        // ---- Items ----

        [Test]
        public void HealingPill_HealsAnAdjacentAllyAndIsUsedUp()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            var friend = CombatFixtures.Place(field, 1, 0);
            friend.TakeDamage(40);
            var pill = new ItemData { Type = ItemType.HealingPill, Power = 25, Quantity = 1 };
            new ItemAction(pill).Execute(u, friend.CurrentCell, field);
            Assert.IsTrue(friend.CurrentVitality == 85 && pill.Quantity == 0);
        }

        [Test]
        public void Item_IsInvalid_WhenNoneIsLeft()
        {
            var field = CombatFixtures.Field();
            var u = CombatFixtures.Place(field, 0, 0);
            var pill = new ItemData { Type = ItemType.QiRestorationPill, Power = 10, Quantity = 0 };
            Assert.IsFalse(new ItemAction(pill).IsValid(u, u.CurrentCell, field));
        }

        // ---- Techniques ----

        [Test]
        public void MartialArt_SpendsQiAndStrikesWithTheAffinityBonus()
        {
            var field = CombatFixtures.Field();
            var art = Technique(TechniqueType.MartialArt, power: 20, element: Element.Fire);
            var u = Knowing(CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement, affinity: Element.Fire), art);
            var foe = CombatFixtures.Place(field, 2, 0, isAlly: false);
            new TechniqueAction(art).Execute(u, foe.CurrentCell, field);
            Assert.IsTrue(foe.CurrentVitality == 100 - 25 && u.CurrentQi == u.MaxQi - 10); // 20 × 1.2 × 1.25 − 5
        }

        [Test]
        public void WaterArt_StrikesHarder_OnWater()
        {
            var field = CombatFixtures.Field();
            field.Grid.SetTerrain(2, 0, TerrainType.Water);
            var art = Technique(TechniqueType.MartialArt, power: 20, element: Element.Water);
            var u = Knowing(CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement), art);
            var foe = CombatFixtures.Place(field, 2, 0, isAlly: false);
            new TechniqueAction(art).Execute(u, foe.CurrentCell, field);
            Assert.AreEqual(100 - 24, foe.CurrentVitality); // round(24 × 1.2) = 29, − 5
        }

        [Test]
        public void SupportArt_HealsAnAlly()
        {
            var field = CombatFixtures.Field();
            var art = Technique(TechniqueType.SupportArt, power: 20, element: Element.Wood);
            var u = Knowing(CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement, affinity: Element.Wood), art);
            var friend = CombatFixtures.Place(field, 1, 0);
            friend.TakeDamage(50);
            new TechniqueAction(art).Execute(u, friend.CurrentCell, field);
            Assert.AreEqual(50 + 29, friend.CurrentVitality); // round(20 × 1.15 × 1.25)
        }

        [Test]
        public void Technique_IsInvalid_WhenUnknown()
        {
            var field = CombatFixtures.Field();
            var art = Technique(TechniqueType.MartialArt);
            var u = CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement);
            var foe = CombatFixtures.Place(field, 1, 0, isAlly: false);
            Assert.IsFalse(new TechniqueAction(art).IsValid(u, foe.CurrentCell, field));
        }

        [Test]
        public void Technique_IsInvalid_BelowTheRequiredRealm()
        {
            var field = CombatFixtures.Field();
            var art = Technique(TechniqueType.MartialArt, required: CultivationRealm.Foundation);
            var u = Knowing(CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement), art);
            var foe = CombatFixtures.Place(field, 1, 0, isAlly: false);
            Assert.IsFalse(new TechniqueAction(art).IsValid(u, foe.CurrentCell, field));
        }

        [Test]
        public void CultivationMethod_CannotBeUsedInCombat()
        {
            var field = CombatFixtures.Field();
            var method = Technique(TechniqueType.CultivationMethod);
            var u = Knowing(CombatFixtures.Place(field, 0, 0, CultivationRealm.QiRefinement), method);
            Assert.IsFalse(new TechniqueAction(method).IsValid(u, u.CurrentCell, field));
        }
    }
}
