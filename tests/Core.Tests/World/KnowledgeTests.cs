using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// Knowledge as a module (user request, 2026-09-26; LORE.md §11 P3 « knowledge is a resource »): typed
    /// facts revealed by a source, pluggable implications, and the rules it gates.
    /// </summary>
    [TestFixture]
    public class KnowledgeTests
    {
        private static readonly Fact Sea = new Fact(FactKind.Ability, "orthodox-water:boundless-sea");
        private static readonly Fact Farewell = new Fact(FactKind.Ability, "orthodox-water:river-farewell");

        // ---- The module ----

        [Test]
        public void Reveal_TeachesAFactOnce()
        {
            var knowledge = new KnowledgeBase();
            Assert.IsTrue(knowledge.Reveal(Sea, KnowledgeSource.Mirror));
            Assert.IsFalse(knowledge.Reveal(Sea, KnowledgeSource.Studied));
            Assert.IsTrue(knowledge.Knows(Sea));
            CollectionAssert.AreEqual(new[] { "orthodox-water:boundless-sea" }, knowledge.Subjects(FactKind.Ability));
        }

        [Test]
        public void Implications_RevealWhatAFactEntails()
        {
            var knowledge = new KnowledgeBase();
            knowledge.AddImplication(fact => fact == Sea ? new[] { new Fact(FactKind.Lineage, "orthodox-water") } : Enumerable.Empty<Fact>());

            knowledge.Reveal(Sea, KnowledgeSource.Formed);

            Assert.IsTrue(knowledge.Knows(new Fact(FactKind.Lineage, "orthodox-water")));
        }

        [Test]
        public void Keys_RoundTripThroughASave()
        {
            var knowledge = new KnowledgeBase();
            knowledge.Reveal(Sea, KnowledgeSource.Start);
            var copy = new KnowledgeBase();
            copy.Restore(knowledge.Keys);
            Assert.IsTrue(copy.Knows(Sea));
            Assert.AreEqual("Ability:orthodox-water:boundless-sea", Sea.Key);
            Assert.AreEqual(Sea, Fact.Parse(Sea.Key));
        }

        // ---- The world's implications ----

        [Test]
        public void LearningAMethod_TeachesItsQiAndTheFoundationItBuilds()
        {
            var w = new TestWorld();
            w.Techniques.Learn("upstream-brook-method");
            Assert.IsTrue(w.Knowledge.Knows(new Fact(FactKind.Qi, "upstream-brook-qi")));
            Assert.IsTrue(w.Knowledge.Knows(Farewell));
            Assert.IsTrue(w.Knowledge.Knows(new Fact(FactKind.Lineage, "orthodox-water")));
        }

        [Test]
        public void KnowingOnesDaoPartners_RevealsThem()
        {
            var w = new TestWorld();
            w.Knowledge.Reveal(new Fact(FactKind.DaoPartners, "orthodox-water:boundless-sea"), KnowledgeSource.Mirror);
            Assert.IsTrue(w.Knowledge.Knows(new Fact(FactKind.Ability, "orthodox-water:ford-watcher")));
        }

        [Test]
        public void NewGame_KnowsItsMethodsFoundations_ButNotTheirPartners()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            Assert.IsTrue(s.Knowledge.Knows(Sea));
            Assert.IsFalse(s.Knowledge.Knows(new Fact(FactKind.DaoPartners, "orthodox-water:boundless-sea")));
            Assert.IsFalse(s.Knowledge.Knows(new Fact(FactKind.Ability, "orthodox-water:ford-watcher")));
        }

        // ---- Sources ----

        [Test]
        public void FormingAFoundation_RevealsIt()
        {
            var w = new TestWorld(new SequenceRandom(0.0));
            var c = w.Join(Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 9));
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Resources.AddQi(Fixtures.ClanQi, 1);

            w.Breakthroughs.AttemptBreakthrough(c);

            Assert.IsTrue(w.Knowledge.Knows(Sea));
        }

        [Test]
        public void Study_CanRevealTheScholarsDaoPartners()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var scholar = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            scholar.FoundationId = "orthodox-water:boundless-sea";
            scholar.CurrentTask = TaskType.Study;

            w.Tasks.ProcessYearlyTasks();

            Assert.IsTrue(w.Knowledge.Knows(new Fact(FactKind.DaoPartners, "orthodox-water:boundless-sea")));
        }

        // ---- Gates ----

        [Test]
        public void Pursuit_NeedsTheAbilityKnown()
        {
            var w = new TestWorld();
            var c = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 1);
            c.FoundationId = Sea.Subject;
            c.DivineAbilities.Add(Sea.Subject);
            w.Join(c);

            Assert.IsFalse(w.Abilities.Pursue(c, Farewell.Subject));
            w.Techniques.Learn("upstream-brook-method"); // its manual names the River Farewell
            Assert.IsTrue(w.Abilities.Pursue(c, Farewell.Subject));
        }

        [Test]
        public void DevouringADaoPartner_NeedsThePartnersKnown()
        {
            // §5.3.3: understanding one's Dao Partners is vital before entering the Dao
            var w = new TestWorld();
            var consumer = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var donor = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            consumer.FoundationId = "mutable-metal:engraved-stele";
            donor.FoundationId = "mutable-metal:dawn-helm";

            Assert.IsFalse(w.Foundations.ConsumeDaoPartner(consumer, donor));
            w.Knowledge.Reveal(new Fact(FactKind.DaoPartners, consumer.FoundationId), KnowledgeSource.Mirror);
            Assert.IsTrue(w.Foundations.ConsumeDaoPartner(consumer, donor));
        }

        // ---- Saves and data ----

        [Test]
        public void RoundTrip_KeepsWhatTheClanKnows()
        {
            var s = GameSession.NewGame(Fixtures.Setup(2));
            var partners = new Fact(FactKind.DaoPartners, "orthodox-water:boundless-sea");
            s.Knowledge.Reveal(partners, KnowledgeSource.Mirror);

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.IsTrue(reloaded.Knowledge.Knows(partners));
        }

        [Test]
        public void OlderSave_RebuildsTheKnowledge_FromTheTechniquesAndFoundations()
        {
            var data = GameSession.NewGame(Fixtures.Setup(2)).ToSaveData();
            data.Knowledge = null; // saved before L4.6
            data.KnownTechniqueIds = new System.Collections.Generic.List<string> { "clear-spring-sutra" };

            var s = GameSession.FromSaveData(data, Fixtures.Setup());

            Assert.IsTrue(s.Techniques.Knows("clear-spring-sutra") && s.Knowledge.Knows(Sea));
        }

        [Test]
        public void Load_Refuses_AStartingFactOfAnUnknownKind()
        {
            var clan = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.ClanFile));
            clan["knowledge"] = new JArray("Rumour:somewhere");
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.ClanFile ? clan.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.ClanFile, error.Message);
        }
    }
}
