using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The current ending conditions: victory after ten generations and one ascension, defeat when the
    /// line dies out. (The dynastic endings of LORE.md §11.9 arrive with phase L6.)
    /// </summary>
    [TestFixture]
    public class VictoryConditionSystemTests
    {
        private static (TestWorld world, AscensionSystem ascension, VictoryConditionSystem victory) Build()
        {
            var w = new TestWorld();
            var ascension = new AscensionSystem(w.Ctx, w.Clan);
            return (w, ascension, new VictoryConditionSystem(w.Ctx, w.Clan, w.Karma, ascension));
        }

        [Test]
        public void LastDeath_EndsTheGameInDefeat()
        {
            var (w, _, victory) = Build();
            bool? outcome = null;
            w.Ctx.Events.OnGameOver += won => outcome = won;
            w.Clan.Kill(w.Join(Fixtures.Cultivator()), DeathCause.OldAge);
            Assert.IsTrue(victory.GameLost && outcome == false);
        }

        [Test]
        public void TenGenerationsAndAnAscension_WinAtTheTurnOfTheYear()
        {
            var (w, ascension, victory) = Build();
            w.Join(Fixtures.Cultivator());
            w.Karma.Restore(10, 0, 0, null);
            ascension.Restore(1);
            w.Ctx.Events.TriggerYearStarted(300);
            Assert.IsTrue(victory.GameWon);
        }

        [Test]
        public void TenGenerations_AreNotEnoughWithoutAnAscension()
        {
            var (w, _, victory) = Build();
            w.Join(Fixtures.Cultivator());
            w.Karma.Restore(10, 0, 0, null);
            w.Ctx.Events.TriggerYearStarted(300);
            Assert.IsFalse(victory.GameWon);
        }

        [Test]
        public void AFinishedGame_StaysFinished()
        {
            var (w, ascension, victory) = Build();
            w.Clan.Kill(w.Join(Fixtures.Cultivator()), DeathCause.OldAge);
            w.Karma.Restore(10, 0, 0, null);
            ascension.Restore(1);
            w.Ctx.Events.TriggerYearStarted(300);
            Assert.IsTrue(victory.GameLost && !victory.GameWon);
        }
    }
}
