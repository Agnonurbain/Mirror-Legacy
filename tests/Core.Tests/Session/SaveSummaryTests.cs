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
            StringAssert.Contains("an 2", slots[0].Label); // two years played from year 0
            Assert.IsTrue(slots[0].CanContinue);
            StringAssert.Contains("illisible", slots[1].Label);
            Assert.IsFalse(slots[1].CanContinue);
            StringAssert.Contains("vide", slots[2].Label);
            Assert.IsFalse(slots[2].CanContinue);
        }

        // ---- Review: nothing on the title screen may crash, nothing may be lost ----

        [Test]
        public void ASaveWithHolesInItsRecords_IsStillSummedUp()
        {
            var summary = SaveSummary.Read("{ \"ClanName\": \"Mo\", \"CurrentYear\": 4, \"HistoricalRecords\": [null] }");
            Assert.IsNotNull(summary);
            Assert.AreEqual(0, summary.Living);
        }

        [Test]
        public void AnEmptyFile_IsUnreadable_NotAnEmptySlot()
        {
            var slot = TitleView.Slots(i => i == 1 ? "" : null)[0];
            StringAssert.Contains("illisible", slot.Label, "a file that reads empty is not an empty slot: never overwrite it unasked");
            Assert.IsFalse(slot.IsEmpty);
        }

        [Test]
        public void AnInconsistentSave_IsRefused_WithAReason_NotACrash()
        {
            var session = GameSession.TryLoad("{ \"SaveVersion\": \"2.20\", \"HistoricalRecords\": [null] }", Fixtures.Setup(), out string error);
            Assert.IsNull(session);
            Assert.IsNotNull(error);
        }

        [Test]
        public void AGoodSave_Loads()
        {
            var session = GameSession.TryLoad(SavedGame(1), Fixtures.Setup(), out string error);
            Assert.IsNotNull(session, error);
            Assert.IsNull(error);
        }
    }
}
