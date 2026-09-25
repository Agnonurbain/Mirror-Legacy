using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The clan's chronicle: births, deaths, breakthroughs and events, told in French by year.</summary>
    [TestFixture]
    public class ChronicleTests
    {
        private GameSession session;
        private Chronicle chronicle;

        [SetUp]
        public void SetUp()
        {
            session = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            chronicle = new Chronicle(session);
        }

        private string Last => chronicle.Entries.Last();

        [Test]
        public void RecordsABirthWithItsYear()
        {
            var child = session.Clan.GenerateChild(session.Clan.GetPatriarch(), null);
            Assert.AreEqual($"An 1 : naissance de {child.FullName}.", Last);
        }

        [Test]
        public void RecordsADeathAndItsCause()
        {
            var patriarch = session.Clan.GetPatriarch();
            session.Clan.Kill(patriarch, DeathCause.OldAge);
            StringAssert.StartsWith($"An 1 : {patriarch.FullName} meurt de vieillesse.", chronicle.Entries.First(e => e.Contains("meurt")));
        }

        [Test]
        public void RecordsABreakthroughWithTheNewRank()
        {
            var patriarch = session.Clan.GetPatriarch();
            session.Events.TriggerBreakthroughSuccess(patriarch, patriarch.Realm);
            StringAssert.Contains("Culture du Qi — 3e niveau", Last);
        }

        [Test]
        public void RecordsTheYearsRandomEvent()
        {
            var evt = new RandomEventData { Name = "Marchand itinérant", Description = "Un marchand propose des marchandises rares." };
            session.Events.TriggerRandomEventOccurred(evt);
            Assert.AreEqual("An 1 : Marchand itinérant — Un marchand propose des marchandises rares.", Last);
        }

        [Test]
        public void KeepsOnlyTheMostRecentEntries()
        {
            var short_ = new Chronicle(session, capacity: 3);
            for (int i = 0; i < 5; i++)
                session.Events.TriggerRandomEventOccurred(new RandomEventData { Name = $"E{i}", Description = "." });
            Assert.IsTrue(short_.Entries.Count == 3 && short_.Entries.Last().Contains("E4") && short_.Entries.First().Contains("E2"));
        }

        [Test]
        public void AnnouncesEachNewEntry()
        {
            string heard = null;
            chronicle.OnEntryAdded += e => heard = e;
            session.Events.TriggerRandomEventOccurred(new RandomEventData { Name = "Année paisible", Description = "Rien." });
            Assert.AreEqual(Last, heard);
        }
    }
}
