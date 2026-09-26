using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The immortal foundation (LORE.md §5.3): formed from one's Qi, its Dao Partners, consuming one, and the
    /// alignment of the Dao Heart (decision of 2026-09-25: a hereditary temperament).
    /// </summary>
    [TestFixture]
    public class FoundationTests
    {
        private static FruitionDefinition Fruition(string id) => Fixtures.Content.Fruitions.Single(f => f.Id == id);

        private static CharacterData AtTheFoundation(string foundation, int stage = 1, Temperament temperament = Temperament.None)
        {
            var c = Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: stage);
            c.FoundationId = foundation;
            c.Temperament = temperament;
            c.CurrentTask = TaskType.Cultivation;
            return c;
        }

        // ---- Rules ----

        [Test]
        public void DaoPartners_AreTheOtherFoundationsOfTheLineage()
        {
            // §6.5: the Engraved Stele and the Dawn Helm are Dao Partners of each other
            var partners = FoundationRules.DaoPartners(Fruition("mutable-metal"), "engraved-stele").Select(a => a.Id).ToList();
            CollectionAssert.Contains(partners, "dawn-helm");
            CollectionAssert.DoesNotContain(partners, "engraved-stele");
            Assert.AreEqual(Fruition("mutable-metal").Abilities.Count - 1, partners.Count);
        }

        [TestCase(Temperament.Dominant, 1.2)]
        [TestCase(Temperament.Solitary, 0.9)]
        [TestCase(Temperament.None, 1.0)]
        public void HeartAlignment_QuickensAnAlignedHeart_AndHindersAnother(Temperament temperament, double speed)
        {
            // §5.3.2: a domineering leader cultivates the fiery Bright Yang far better than a loner
            Assert.AreEqual(speed, FoundationRules.HeartAlignmentSpeed(temperament, Fruition("bright-yang"), Fixtures.Content.Balance), 1e-9);
        }

        [Test]
        public void HeartAlignment_IsNeutral_WithoutAFoundation()
        {
            Assert.AreEqual(1.0, FoundationRules.HeartAlignmentSpeed(Temperament.Solitary, null, Fixtures.Content.Balance), 1e-9);
        }

        [Test]
        public void ShippedFruitions_EachFavourATemperament()
        {
            Assert.IsTrue(Fixtures.Content.Fruitions.All(f => f.Temperament != Temperament.None));
            Assert.AreEqual(Temperament.Dominant, Fruition("bright-yang").Temperament);
        }

        // ---- Forming the foundation ----

        [Test]
        public void FoundationWall_WaitsForAPortionOfTheQi()
        {
            // §2.5: at least one portion for the immortal foundation
            var w = new TestWorld();
            var c = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9));
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);

            Assert.IsFalse(w.Cultivation.IsReadyForTrial(c));
            w.Resources.AddQi(Fixtures.ClanQi, 1);
            Assert.IsTrue(w.Cultivation.IsReadyForTrial(c));
        }

        [Test]
        public void FoundationWall_SpendsThePortion_AndFormsTheFoundationOfTheQi()
        {
            var w = new TestWorld(new SequenceRandom(0.0)); // the roll succeeds
            var c = w.Join(Fixtures.Cultivator(age: 30, realm: CultivationRealm.QiRefinement, stage: 9));
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Resources.AddQi(Fixtures.ClanQi, 1);

            w.Breakthroughs.AttemptBreakthrough(c);

            Assert.AreEqual(CultivationRealm.Foundation, c.Realm);
            Assert.AreEqual("orthodox-water:boundless-sea", c.FoundationId); // the Clear Spring Sutra's foundation
            Assert.AreEqual(0, w.Resources.QiPortions(Fixtures.ClanQi));
        }

        [Test]
        public void Cultivation_AtTheFoundation_FollowsTheHeartAlignment()
        {
            var w = new TestWorld();
            var aligned = w.Join(AtTheFoundation("orthodox-water:boundless-sea", temperament: Temperament.Cunning));
            var hindered = w.Join(AtTheFoundation("orthodox-water:boundless-sea", temperament: Temperament.Dominant));
            aligned.SpiritualRoot = hindered.SpiritualRoot = 40;

            w.Cultivation.ProcessYearlyCultivation(aligned);
            w.Cultivation.ProcessYearlyCultivation(hindered);

            Assert.AreEqual(36, aligned.CultivationXP);  // 30 × 1.2 (the Orthodox Water favours a cunning heart)
            Assert.AreEqual(27, hindered.CultivationXP); // 30 × 0.9
        }

        // ---- Consuming a Dao Partner (§5.3.3) ----

        [Test]
        public void ConsumeDaoPartner_RaisesTheConsumerAStage_SealsTheirProgression_AndKillsTheDonor()
        {
            var w = new TestWorld();
            var consumer = w.Join(AtTheFoundation("mutable-metal:engraved-stele", stage: 1));
            var donor = w.Join(AtTheFoundation("mutable-metal:dawn-helm", stage: 2));
            w.Knowledge.Reveal(new MirrorChronicles.World.Fact(MirrorChronicles.World.FactKind.DaoPartners, consumer.FoundationId),
                MirrorChronicles.World.KnowledgeSource.Studied); // one devours only a partner one knows (L4.6c)

            Assert.IsTrue(w.Foundations.ConsumeDaoPartner(consumer, donor));

            Assert.IsTrue(consumer.RealmStage == 2 && consumer.ProgressionSealed);
            Assert.IsTrue(!donor.IsAlive && donor.CauseOfDeath == DeathCause.FoundationDevoured); // decision of the user, 2026-09-25
        }

        [Test]
        public void ConsumeDaoPartner_Refuses_AFoundationOfAnotherLineage()
        {
            var w = new TestWorld();
            var consumer = w.Join(AtTheFoundation("mutable-metal:engraved-stele"));
            var donor = w.Join(AtTheFoundation("orthodox-water:boundless-sea"));
            Assert.IsFalse(w.Foundations.ConsumeDaoPartner(consumer, donor));
            Assert.AreEqual(1, consumer.RealmStage);
        }

        [Test]
        public void ConsumeDaoPartner_Refuses_ASealedConsumer()
        {
            var w = new TestWorld();
            var consumer = w.Join(AtTheFoundation("mutable-metal:engraved-stele"));
            consumer.ProgressionSealed = true;
            Assert.IsFalse(w.Foundations.ConsumeDaoPartner(consumer, w.Join(AtTheFoundation("mutable-metal:dawn-helm"))));
        }

        [Test]
        public void SealedProgression_GainsNothingMore()
        {
            var w = new TestWorld();
            var sealedOne = w.Join(AtTheFoundation("orthodox-water:boundless-sea"));
            sealedOne.ProgressionSealed = true;

            w.Cultivation.ProcessYearlyCultivation(sealedOne);

            Assert.AreEqual(0, sealedOne.CultivationXP);
        }

        // ---- The Dao Heart drifts towards its foundation (§5.3.2) ----

        [Test]
        public void DaoHeart_DriftsTowardsTheFoundationsTemperament()
        {
            var w = new TestWorld(new FixedRandom(0.0)); // the yearly chance hits
            var c = w.Join(AtTheFoundation("orthodox-water:boundless-sea", temperament: Temperament.Dominant));

            w.Ctx.Events.TriggerYearStarted(2);

            Assert.AreEqual(Temperament.Cunning, c.Temperament);
        }

        // ---- Temperament: inherited, drawn for founders and older saves ----

        [Test]
        public void GenerateChild_InheritsAParentsTemperament()
        {
            var w = new TestWorld(new FixedRandom(0.0)); // inherits, from the father
            var father = w.Join(Fixtures.Cultivator(isMale: true));
            var mother = w.Join(Fixtures.Cultivator(isMale: false));
            father.Temperament = Temperament.Dominant;
            mother.Temperament = Temperament.Serene;

            Assert.AreEqual(Temperament.Dominant, w.Clan.GenerateChild(father, mother).Temperament);
        }

        [Test]
        public void NewGame_GivesEveryFounderATemperament()
        {
            var s = MirrorChronicles.Session.GameSession.NewGame(Fixtures.Setup(1));
            Assert.IsTrue(s.Clan.LivingMembers.All(m => m.Temperament != Temperament.None));
        }

        [Test]
        public void OlderSave_DrawsTheTemperamentsItLacks()
        {
            var data = MirrorChronicles.Session.GameSession.NewGame(Fixtures.Setup(2)).ToSaveData();
            foreach (var r in data.HistoricalRecords) r.Temperament = Temperament.None; // saved before phase L4

            var s = MirrorChronicles.Session.GameSession.FromSaveData(data, Fixtures.Setup());

            Assert.IsTrue(s.Clan.LivingMembers.All(m => m.Temperament != Temperament.None));
        }

        [Test]
        public void DaoHeart_KeepsItsTemperament_WhenTheChanceMisses()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var c = w.Join(AtTheFoundation("orthodox-water:boundless-sea", temperament: Temperament.Dominant));

            w.Ctx.Events.TriggerYearStarted(2);

            Assert.AreEqual(Temperament.Dominant, c.Temperament);
        }

        // ---- An inhuman body in the Dao's image (LORE.md §5.3.2, §11.6) ----

        [Test]
        public void FormingAMetalFoundation_GivesGoldenBlood()
        {
            var w = new TestWorld();
            var c = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9));
            c.QiId = "clear-edge-qi"; // builds the Engraved Stone of the Mutable Metal

            w.Cultivation.ApplyStep(c, PowerLadder.Next(c.Realm, c.RealmStage));

            Assert.AreEqual("Sang doré", c.BodyTrait);
        }

        [Test]
        public void ALineageTheLoreGivesNoBody_LeavesTheBodyHuman()
        {
            var w = new TestWorld();
            var c = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 9)); // the clan's Clear Spring: Orthodox Water

            w.Cultivation.ApplyStep(c, PowerLadder.Next(c.Realm, c.RealmStage));

            Assert.IsNull(c.BodyTrait);
        }

        [TestCase(0.0, "Sang doré")]
        [TestCase(0.999, null)]
        public void AParentsInhumanBody_PassesToTheChildren_Sometimes(double roll, string trait)
        {
            var w = new TestWorld(new FixedRandom(roll));
            var father = w.Join(Fixtures.Cultivator());
            father.BodyTrait = "Sang doré";
            var child = w.Clan.GenerateChild(father, w.Join(Fixtures.Mortal(isMale: false)));
            Assert.AreEqual(trait, child.BodyTrait);
        }
    }
}
