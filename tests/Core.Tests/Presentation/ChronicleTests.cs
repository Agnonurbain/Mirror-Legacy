using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

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

        // ---- What the clan learns (L4c: the knowledge base tells the chronicle) ----

        [TestCase(FactKind.Technique, "night-frost-canon", "le clan apprend la technique Canon du Givre Nocturne.")]
        [TestCase(FactKind.Qi, "night-frost-qi", "le clan apprend à connaître le Qi du Givre Nocturne.")]
        [TestCase(FactKind.Lineage, "orthodox-water", "le clan apprend l'existence de la lignée Eau Orthodoxe.")]
        [TestCase(FactKind.Ability, "orthodox-water:storm-sky", "le clan apprend la capacité Ciel d'Orage (Eau Orthodoxe).")]
        [TestCase(FactKind.DaoPartners, "orthodox-water:boundless-sea", "le clan apprend les Partenaires Dao de Mer sans Rivage (Eau Orthodoxe).")]
        [TestCase(FactKind.GoldSeeking, "orthodox-water", "le clan apprend la méthode de recherche d'or de la lignée Eau Orthodoxe.")]
        [TestCase(FactKind.GoldSeeking, "orthodox-water:intercalary", "le clan apprend la méthode de recherche d'or spécialisée de la lignée Eau Orthodoxe.")]
        [TestCase(FactKind.LeftHand, "lesser-yin", "le clan apprend la voie des Immortels de Jade Tressé (Yin Mineur).")]
        public void RecordsWhatTheClanLearns(FactKind kind, string subject, string line)
        {
            session.Knowledge.Reveal(kind, subject, KnowledgeSource.Mirror);
            CollectionAssert.Contains(chronicle.Entries, $"An 1 : {line}");
        }

        [Test]
        public void RecordsNothing_ForWhatItAlreadyKnew()
        {
            session.Knowledge.Reveal(FactKind.Lineage, "orthodox-water", KnowledgeSource.Mirror);
            int count = chronicle.Entries.Count;
            session.Knowledge.Reveal(FactKind.Lineage, "orthodox-water", KnowledgeSource.Mirror);
            Assert.AreEqual(count, chronicle.Entries.Count);
        }

        [Test]
        public void RecordsNothing_ForAnOlderSavesRebuiltKnowledge()
        {
            int count = chronicle.Entries.Count;
            session.Knowledge.Reveal(FactKind.Lineage, "mutable-water", KnowledgeSource.OlderSave);
            Assert.AreEqual(count, chronicle.Entries.Count);
        }
    }
}
