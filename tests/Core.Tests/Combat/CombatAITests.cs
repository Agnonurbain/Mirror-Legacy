using System;
using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>The five enemy behaviours (strategy pattern), which the Unity battle never called.</summary>
    [TestFixture]
    public class CombatAITests
    {
        [Test]
        public void Aggressive_ClosesInAndStrikes()
        {
            var field = CombatFixtures.Field();
            var ally = CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 2, 0, CultivationRealm.QiRefinement, isAlly: false);
            CombatAI.PlayTurn(foe, field);
            Assert.IsTrue(foe.CurrentCell == field.Grid.GetCellAt(1, 0) && ally.CurrentVitality == 100 - 13);
        }

        [Test]
        public void Defensive_HoldsItsGround()
        {
            var field = CombatFixtures.Field();
            CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 5, 5, isAlly: false, strategy: AIStrategyType.Defensive);
            CombatAI.PlayTurn(foe, field);
            Assert.IsTrue(foe.IsDefending && foe.CurrentCell == field.Grid.GetCellAt(5, 5));
        }

        [Test]
        public void Cautious_Flees_WhenBadlyHurt()
        {
            var field = CombatFixtures.Field(new FixedRandom(0.0));
            CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 5, 5, isAlly: false, strategy: AIStrategyType.Cautious);
            foe.TakeDamage(70);
            CombatAI.PlayTurn(foe, field);
            Assert.IsTrue(foe.HasFled);
        }

        [Test]
        public void Cautious_Guards_WhenAFoeIsNextToIt()
        {
            var field = CombatFixtures.Field();
            CombatFixtures.Place(field, 4, 5);
            var foe = CombatFixtures.Place(field, 5, 5, isAlly: false, strategy: AIStrategyType.Cautious);
            CombatAI.PlayTurn(foe, field);
            Assert.IsTrue(foe.IsDefending);
        }

        [Test]
        public void Berserker_UnleashesItsStrongestTechnique()
        {
            var weak = new TechniqueData { Type = TechniqueType.MartialArt, PowerModifier = 10, Range = 2, QiCost = 5 };
            var strong = new TechniqueData { Type = TechniqueType.MartialArt, PowerModifier = 30, Range = 2, QiCost = 5 };
            var known = new Dictionary<string, TechniqueData> { [weak.ID] = weak, [strong.ID] = strong };
            var field = new BattleField(new CombatGrid(10, 10), new Random(1), new RecordingGameLog(), id => known.GetValueOrDefault(id));
            var ally = CombatFixtures.Place(field, 0, 0);
            var foe = CombatFixtures.Place(field, 2, 0, CultivationRealm.QiRefinement, isAlly: false, strategy: AIStrategyType.Berserker);
            foe.BaseData.KnownTechniqueIDs.AddRange(new[] { weak.ID, strong.ID });

            CombatAI.PlayTurn(foe, field);

            Assert.AreEqual(100 - 31, ally.CurrentVitality); // 30 × 1.2 − 5
        }

        [Test]
        public void Strategic_StrikesTheWeakestFoe_WithoutSteppingAway()
        {
            var field = CombatFixtures.Field();
            var strong = CombatFixtures.Place(field, 1, 0, CultivationRealm.Foundation);
            var weak = CombatFixtures.Place(field, 1, 2);
            var foe = CombatFixtures.Place(field, 1, 1, CultivationRealm.QiRefinement, isAlly: false, strategy: AIStrategyType.Strategic);

            CombatAI.PlayTurn(foe, field);

            Assert.IsTrue(weak.CurrentVitality == 100 - 13 && strong.CurrentVitality == strong.MaxVitality);
        }

        [Test]
        public void AnyStrategy_WaitsQuietly_WhenNoFoeStands()
        {
            var field = CombatFixtures.Field();
            var foe = CombatFixtures.Place(field, 5, 5, isAlly: false);
            Assert.DoesNotThrow(() => CombatAI.PlayTurn(foe, field));
        }
    }
}
