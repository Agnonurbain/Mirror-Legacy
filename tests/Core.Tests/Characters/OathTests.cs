using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Oaths of the Dao (L4d, user request of 2026-09-26): two cultivators of any path swear on their path;
    /// the oath-breaker's path is slowed (a Heart Demon) or interrupted; loopholes exist — all from the data.
    /// </summary>
    [TestFixture]
    public class OathTests
    {
        private const double Slow = 0.99;  // the interruption roll misses: a Heart Demon
        private const double Cut = 0.0;    // the interruption roll hits

        private static (TestWorld w, CharacterData a, CharacterData b) Pact(double sample, params string[] clauses)
        {
            var w = new TestWorld(new FixedRandom(sample));
            var a = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var b = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            w.Oaths.Swear(a, b, clauses.Length > 0 ? clauses : new[] { "never-harm" });
            return (w, a, b);
        }

        [Test]
        public void Swear_BindsBothParties()
        {
            var (w, a, b) = Pact(Slow);
            Assert.AreEqual(1, w.Oaths.Pacts.Count);
            Assert.IsTrue(w.Oaths.PactsOf(a).Any() && w.Oaths.PactsOf(b).Any());
        }

        [Test]
        public void Breach_SlowsThePathWithAHeartDemon()
        {
            var (w, a, b) = Pact(Slow);
            int stability = a.MentalStability;

            Assert.IsTrue(w.Oaths.Transgress(a, b, OathAct.Harm));

            Assert.IsTrue(a.HeartDemonYearsLeft > 0 && !a.ProgressionSealed);
            Assert.AreEqual(stability - w.Ctx.Content.Balance.Oaths.HeartDemonStabilityLoss, a.MentalStability);
        }

        [Test]
        public void Breach_CanInterruptThePath()
        {
            var (w, a, b) = Pact(Cut);
            w.Oaths.Transgress(a, b, OathAct.Harm);
            Assert.IsTrue(a.ProgressionSealed);
        }

        [Test]
        public void HeartDemon_SlowsCultivation_AndFadesWithTheYears()
        {
            var (w, a, b) = Pact(Slow);
            var clean = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));
            var haunted = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));
            clean.CurrentTask = haunted.CurrentTask = TaskType.Cultivation;
            haunted.HeartDemonYearsLeft = 2;

            w.Cultivation.ProcessYearlyCultivation(clean);
            w.Cultivation.ProcessYearlyCultivation(haunted);
            Assert.Less(haunted.CultivationXP, clean.CultivationXP);

            w.Ctx.Events.TriggerYearStarted(2);
            w.Ctx.Events.TriggerYearStarted(3);
            Assert.AreEqual(0, haunted.HeartDemonYearsLeft);
        }

        [Test]
        public void AnActTheOathDoesNotName_IsNoBreach()
        {
            var (w, a, b) = Pact(Slow, "keep-secret");
            Assert.IsFalse(w.Oaths.Transgress(a, b, OathAct.Harm));
            Assert.AreEqual(0, a.HeartDemonYearsLeft);
        }

        [Test]
        public void DevouringASwornPartner_BreaksTheOath()
        {
            var (w, a, b) = Pact(Slow);
            a.FoundationId = "mutable-metal:engraved-stele";
            b.FoundationId = "mutable-metal:dawn-helm";
            w.Knowledge.Reveal(new MirrorChronicles.World.Fact(MirrorChronicles.World.FactKind.DaoPartners, a.FoundationId),
                MirrorChronicles.World.KnowledgeSource.Studied);

            w.Foundations.ConsumeDaoPartner(a, b);

            Assert.IsTrue(a.HeartDemonYearsLeft > 0);
        }

        // ---- Loopholes: nothing is perfect ----

        [Test]
        public void Loophole_TheLetterNotTheSpirit_AThirdPartyActs()
        {
            var (w, a, b) = Pact(Cut);
            Assert.IsFalse(w.Oaths.Transgress(a, b, OathAct.Harm, throughThirdParty: true));
            Assert.IsFalse(a.ProgressionSealed);
        }

        [Test]
        public void Loophole_AnOathSwornUnderAFalseNameBindsNothing()
        {
            var w = new TestWorld(new FixedRandom(Cut));
            var a = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var b = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            w.Oaths.Swear(a, b, new[] { "never-harm" }, falseNameA: true);

            Assert.IsFalse(w.Oaths.Transgress(a, b, OathAct.Harm));
            Assert.IsTrue(w.Oaths.Transgress(b, a, OathAct.Harm)); // the other swore truly
        }

        [Test]
        public void Loophole_AnOathExpires()
        {
            var w = new TestWorld(new FixedRandom(Cut));
            var a = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var b = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            w.Oaths.Swear(a, b, new[] { "never-harm" }, years: 1);

            w.Ctx.Clock.Restore(3, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(3);

            Assert.IsFalse(w.Oaths.Transgress(a, b, OathAct.Harm));
        }

        [Test]
        public void Loophole_TheMirrorVeilsABreach_AtACost()
        {
            var (w, a, b) = Pact(Cut);
            w.Mirror.Restore(100, 0);

            Assert.IsTrue(w.Oaths.VeilNextBreach(a));
            w.Oaths.Transgress(a, b, OathAct.Harm);

            Assert.IsFalse(a.ProgressionSealed);
            Assert.AreEqual(100 - w.Ctx.Content.Balance.Oaths.MirrorVeilCost, w.Mirror.MirrorPower);
        }

        [Test]
        public void Loophole_PurificationCanLiftAHeartDemon()
        {
            var (w, a, b) = Pact(0.0); // the purification succeeds
            a.HeartDemonYearsLeft = 10;
            w.Resources.AddHerbs(w.Ctx.Content.Balance.Oaths.PurificationHerbs);

            Assert.IsTrue(w.Oaths.Purify(a));
            Assert.AreEqual(0, a.HeartDemonYearsLeft);
        }

        // ---- A promised service ----

        [Test]
        public void Service_NotRenderedByItsDeadline_BreaksTheOath()
        {
            var w = new TestWorld(new FixedRandom(Slow));
            var a = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var b = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            w.Oaths.Swear(a, b, new[] { "render-service" }, years: 2);

            w.Ctx.Clock.Restore(4, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(4);

            Assert.IsTrue(a.HeartDemonYearsLeft > 0 && b.HeartDemonYearsLeft > 0);
        }

        [Test]
        public void Service_Rendered_KeepsThePathClear()
        {
            var w = new TestWorld(new FixedRandom(Slow));
            var a = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var b = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var pact = w.Oaths.Swear(a, b, new[] { "render-service" }, years: 2);
            w.Oaths.Fulfil(pact, a);
            w.Oaths.Fulfil(pact, b);

            w.Ctx.Clock.Restore(4, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(4);

            Assert.AreEqual(0, a.HeartDemonYearsLeft);
        }

        // ---- Data and saves ----

        [Test]
        public void ShippedOaths_MarkThemselvesAsInterpretation()
        {
            Assert.IsTrue(Fixtures.Content.Oaths.Clauses.Count > 0 && Fixtures.Content.Oaths.Clauses.All(c => c.Provenance == Provenance.Interpretation));
            Assert.IsTrue(Fixtures.Content.Oaths.Loopholes.Any(l => l.Kind == LoopholeKind.ThirdParty));
        }

        [Test]
        public void Load_Refuses_AClauseWithoutSeverity()
        {
            var oaths = JObject.Parse(Fixtures.ReadDataFile(GameContentLoader.OathsFile));
            oaths["clauses"][0]["severity"] = 0;
            var error = Assert.Throws<InvalidDataException>(() =>
                GameContentLoader.Load(name => name == GameContentLoader.OathsFile ? oaths.ToString() : Fixtures.ReadDataFile(name)));
            StringAssert.Contains(GameContentLoader.OathsFile, error.Message);
        }

        [Test]
        public void RoundTrip_KeepsThePactsAndTheHeartDemons()
        {
            var s = GameSession.NewGame(Fixtures.Setup(2));
            var members = s.Clan.LivingMembers;
            s.Oaths.Swear(members[0], members[1], new[] { "never-harm" });
            members[0].HeartDemonYearsLeft = 3;

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());

            Assert.AreEqual(1, reloaded.Oaths.Pacts.Count);
            Assert.AreEqual(3, reloaded.Clan.FindById(members[0].ID).HeartDemonYearsLeft);
        }
    }
}
