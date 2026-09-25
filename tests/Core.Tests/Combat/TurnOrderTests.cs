using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>Initiative (agility + 10 per realm) orders each round; the fallen and the fled lose their turn.</summary>
    [TestFixture]
    public class TurnOrderTests
    {
        [Test]
        public void Next_GoesByInitiative()
        {
            var novice = CombatFixtures.Unit(CultivationRealm.Embryonic);
            var master = CombatFixtures.Unit(CultivationRealm.Foundation);
            var adept = CombatFixtures.Unit(CultivationRealm.QiRefinement);
            var turns = new TurnOrder(new[] { novice, master, adept });

            var order = new List<CombatUnit> { turns.Next(), turns.Next(), turns.Next() };

            CollectionAssert.AreEqual(new[] { master, adept, novice }, order);
        }

        [Test]
        public void Next_StartsANewRound_AfterEveryoneActed()
        {
            var turns = new TurnOrder(new[] { CombatFixtures.Unit(), CombatFixtures.Unit() });
            turns.Next();
            turns.Next();
            turns.Next();
            Assert.AreEqual(2, turns.Round);
        }

        [Test]
        public void Next_SkipsTheFallenAndTheFled()
        {
            var fallen = CombatFixtures.Unit(CultivationRealm.Foundation);
            var fled = CombatFixtures.Unit(CultivationRealm.QiRefinement);
            var standing = CombatFixtures.Unit();
            var turns = new TurnOrder(new[] { fallen, fled, standing });
            fallen.TakeDamage(1000);
            fled.Flee();
            Assert.AreSame(standing, turns.Next());
        }

        [Test]
        public void Next_ReturnsNull_WhenNobodyStands()
        {
            var u = CombatFixtures.Unit();
            var turns = new TurnOrder(new[] { u });
            u.TakeDamage(1000);
            Assert.IsNull(turns.Next());
        }

        [Test]
        public void Next_ClearsTheNewUnitsTurnState()
        {
            var u = CombatFixtures.Unit();
            u.IsDefending = true;
            u.HasActedThisTurn = true;
            new TurnOrder(new[] { u }).Next();
            Assert.IsTrue(!u.IsDefending && !u.HasActedThisTurn);
        }
    }
}
