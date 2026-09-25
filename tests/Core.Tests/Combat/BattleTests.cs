using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>A whole battle: deployment, enemy turns played by their AI, the ending, and its aftermath.</summary>
    [TestFixture]
    public class BattleTests
    {
        private static CharacterData Fighter(CultivationRealm realm) => Fixtures.Cultivator(realm: realm);

        private static Battle Start(Random rng, CharacterData[] allies, CharacterData[] enemies) =>
            Battle.Start(allies, enemies, rng, new RecordingGameLog(), new CombatGrid(10, 10));

        [Test]
        public void Start_DeploysAlliesWestAndEnemiesEast()
        {
            var b = Start(new Random(1), new[] { Fighter(CultivationRealm.QiRefinement) }, new[] { Fighter(CultivationRealm.Embryonic) });
            Assert.IsTrue(b.Allies.All(u => u.CurrentCell.X == 0) && b.Enemies.All(u => u.CurrentCell.X == 9));
        }

        [Test]
        public void Start_PlaysTheEnemyTurns_UntilAnAllyMustAct()
        {
            var b = Start(new Random(1), new[] { Fighter(CultivationRealm.Embryonic) }, new[] { Fighter(CultivationRealm.Foundation) });
            Assert.IsTrue(b.State == CombatState.PlayerTurn && b.CurrentUnit.IsAlly);
        }

        [Test]
        public void Perform_RefusesAnInvalidAction()
        {
            var b = Start(new Random(1), new[] { Fighter(CultivationRealm.Embryonic) }, new[] { Fighter(CultivationRealm.Embryonic) });
            var farFoe = b.Enemies[0].CurrentCell;
            Assert.IsFalse(b.Perform(new AttackAction(), farFoe));
        }

        [Test]
        public void AutoPlay_EndsInVictory_WhenTheEnemiesFall()
        {
            var b = Start(new Random(1), new[] { Fighter(CultivationRealm.Foundation) }, new[] { Fighter(CultivationRealm.Embryonic) });
            b.AutoPlay();
            Assert.AreEqual(CombatState.Victory, b.State);
        }

        [Test]
        public void AutoPlay_EndsInDefeat_WhenTheAlliesFall()
        {
            var b = Start(new Random(1), new[] { Fighter(CultivationRealm.Embryonic) }, new[] { Fighter(CultivationRealm.Foundation) });
            b.AutoPlay();
            Assert.AreEqual(CombatState.Defeat, b.State);
        }

        [Test]
        public void Fleeing_EveryAlly_EndsTheBattleInDefeat()
        {
            var b = Start(new FixedRandom(0.0), new[] { Fighter(CultivationRealm.Embryonic) }, new[] { Fighter(CultivationRealm.Embryonic) });
            b.Perform(new FleeAction(), null);
            Assert.AreEqual(CombatState.Defeat, b.State);
        }

        [Test]
        public void ResolveAftermath_BuriesTheFallenAndTendsTheWounded()
        {
            var w = new TestWorld();
            var fallen = w.Join(Fighter(CultivationRealm.QiRefinement));
            var wounded = w.Join(Fighter(CultivationRealm.QiRefinement));
            var b = Start(new Random(1), new[] { fallen, wounded }, new[] { Fighter(CultivationRealm.Embryonic) });
            b.Allies[0].TakeDamage(1000);
            b.Allies[1].TakeDamage(75); // half its vitality: a moderate wound
            int before = wounded.MentalStability;

            b.ResolveAftermath(w.Clan, new WoundSystem(w.Ctx, w.Stability));

            Assert.IsTrue(!fallen.IsAlive && fallen.CauseOfDeath == DeathCause.Combat && wounded.MentalStability < before);
        }
    }
}
