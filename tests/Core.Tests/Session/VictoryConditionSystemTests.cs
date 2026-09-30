using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The defeats of LORE.md §11.9: the line dies out, the mirror is seized, the clan is absorbed. There is no
    /// forced victory any more (B3, 2026-09-30): the dynastic endings are told, and the game goes on.
    /// </summary>
    [TestFixture]
    public class VictoryConditionSystemTests
    {
        private static (TestWorld world, VictoryConditionSystem victory) Build()
        {
            var w = new TestWorld();
            return (w, new VictoryConditionSystem(w.Ctx, w.Clan));
        }

        [Test]
        public void LastDeath_EndsTheGameInDefeat()
        {
            var (w, victory) = Build();
            bool over = false;
            w.Ctx.Events.OnGameOver += () => over = true;
            w.Clan.Kill(w.Join(Fixtures.Cultivator()), DeathCause.OldAge);
            Assert.IsTrue(victory.GameLost && victory.IsOver && over);
        }

        [Test]
        public void TenGenerations_NoLongerEndTheGame()
        {
            var (w, victory) = Build();
            w.Join(Fixtures.Cultivator());
            w.Karma.Restore(10, 0, 0, null);
            w.Ctx.Events.TriggerYearStarted(300);
            Assert.IsFalse(victory.IsOver);
        }

        [Test]
        public void ASeizedMirror_EndsTheGameInDefeat()
        {
            var (w, victory) = Build();
            w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerMirrorSeized("Secte du Pic des Nuées");
            Assert.IsTrue(victory.GameLost);
        }

        [Test]
        public void AFinishedGame_StaysFinished()
        {
            var (w, victory) = Build();
            int endings = 0;
            w.Ctx.Events.OnGameOver += () => endings++;
            w.Clan.Kill(w.Join(Fixtures.Cultivator()), DeathCause.OldAge);
            w.Ctx.Events.TriggerMirrorSeized("Secte du Pic des Nuées");
            Assert.AreEqual(1, endings);
        }
    }
}
