using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    [TestFixture]
    public class GameClockTests
    {
        [Test]
        public void NewClock_StartsInYearOneManagement()
        {
            var clock = new GameClock();
            Assert.IsTrue(clock.Year == 1 && clock.Phase == GamePhase.Management);
        }

        [Test]
        public void Advance_MovesToTheNextPhase_WithinTheYear()
        {
            var clock = new GameClock();
            clock.Advance();
            Assert.AreEqual(GamePhase.Events, clock.Phase);
        }

        [Test]
        public void Advance_ReturnsFalse_WithinTheYear()
        {
            Assert.IsFalse(new GameClock().Advance());
        }

        [Test]
        public void Advance_StartsTheNextYear_AfterInheritance()
        {
            var clock = new GameClock();
            clock.Restore(4, GamePhase.Inheritance);
            bool newYear = clock.Advance();
            Assert.IsTrue(newYear && clock.Year == 5 && clock.Phase == GamePhase.Management);
        }

        [Test]
        public void Restore_SetsYearAndPhase()
        {
            var clock = new GameClock();
            clock.Restore(12, GamePhase.Breakthrough);
            Assert.IsTrue(clock.Year == 12 && clock.Phase == GamePhase.Breakthrough);
        }
    }
}
