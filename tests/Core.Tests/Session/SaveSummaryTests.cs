using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The title screen's save slots (G6): each slot tells what it holds — the clan, the year, the generation, the living,
    /// the patriarch — or that it is empty, or that it cannot be read (an older or damaged file never crashes the menu).
    /// </summary>
    [TestFixture]
    public class SaveSummaryTests
    {
        private static string SavedGame(int years)
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            for (int i = 0; i < years; i++) s.AdvanceYear();
            return SaveSerializer.Serialize(s.ToSaveData());
        }

        [Test]
        public void ASave_IsSummed_Up()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            for (int i = 0; i < 3; i++) s.AdvanceYear();
            var summary = SaveSummary.Read(SaveSerializer.Serialize(s.ToSaveData()));
            Assert.AreEqual(s.Clan.ClanName, summary.ClanName);
            Assert.AreEqual(s.Clock.Year, summary.Year);
            Assert.AreEqual(s.Clan.LivingMembers.Count, summary.Living);
            Assert.AreEqual(s.Clan.GetPatriarch().FullName, summary.Patriarch);
        }

        [Test]
        public void AnUnreadableSave_IsNull_NotACrash()
        {
            Assert.IsNull(SaveSummary.Read("{ not json"));
            Assert.IsNull(SaveSummary.Read(null));
        }

        [Test]
        public void TheSlots_TellWhatTheyHold()
        {
            var slots = TitleView.Slots(i => i == 1 ? SavedGame(2) : i == 2 ? "garbage" : null);
            Assert.AreEqual(TitleView.SlotCount, slots.Count);
            StringAssert.Contains("Clan Mo", slots[0].Label);
            StringAssert.Contains("an 3", slots[0].Label);
            Assert.IsTrue(slots[0].CanContinue);
            StringAssert.Contains("illisible", slots[1].Label);
            Assert.IsFalse(slots[1].CanContinue);
            StringAssert.Contains("vide", slots[2].Label);
            Assert.IsFalse(slots[2].CanContinue);
        }
    }
}
