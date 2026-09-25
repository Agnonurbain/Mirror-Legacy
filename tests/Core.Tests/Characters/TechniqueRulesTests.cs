using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>The grade rules of LORE.md §2: speed, ceiling, secret of ascent, the Qi to enter, flaws, deductions.</summary>
    [TestFixture]
    public class TechniqueRulesTests
    {
        private static readonly IReadOnlyList<double> Speeds = new[] { 0.6, 0.8, 1.0, 1.2, 1.5, 1.8, 2.2 };

        private static TechniqueData Catalog(string id) => Fixtures.Content.Techniques.Single(t => t.ID == id);

        private static QiDefinition Qi(string id) => Fixtures.Content.Qi.Single(q => q.Id == id);

        private static TechniqueData Method(int grade, CultivationRealm first = CultivationRealm.QiRefinement, string qi = "clear-spring-qi") =>
            new TechniqueData { ID = $"g{grade}", Kind = TechniqueKind.Cultivation, Grade = grade, RequiredRealm = first, RequiredQiId = qi };

        // ---- Speed (§2.1: a higher grade cultivates faster at the same realm) ----

        [TestCase(1, 0.6)]
        [TestCase(3, 1.0)]
        [TestCase(5, 1.5)]
        [TestCase(7, 2.2)]
        public void Speed_FollowsTheGrade(int grade, double speed)
        {
            Assert.AreEqual(speed, TechniqueRules.CultivationSpeed(Method(grade), CultivationRealm.QiRefinement, Speeds), 1e-9);
        }

        [Test]
        public void Speed_OfAGradeFiveBreathing_MatchesAPurpleMansionManual()
        {
            // §2.1: the White Lotus Intuition breathes as fast as a Purple Mansion technique cultivates
            double lotus = TechniqueRules.CultivationSpeed(Catalog("white-lotus-intuition"), CultivationRealm.Embryonic, Speeds);
            double mansion = TechniqueRules.CultivationSpeed(Catalog("night-frost-canon"), CultivationRealm.QiRefinement, Speeds);
            Assert.AreEqual(mansion, lotus, 1e-9);
        }

        [Test]
        public void Speed_WithoutAManual_IsTheCommonBreathingInEmbryonicAndNothingBeyond()
        {
            Assert.AreEqual(1.0, TechniqueRules.CultivationSpeed(null, CultivationRealm.Embryonic, Speeds), 1e-9);
            Assert.AreEqual(0.0, TechniqueRules.CultivationSpeed(null, CultivationRealm.QiRefinement, Speeds), 1e-9);
        }

        [Test]
        public void Speed_OfAMethodOutsideItsRealms_IsNothing()
        {
            Assert.AreEqual(0.0, TechniqueRules.CultivationSpeed(Method(3), CultivationRealm.PurpleMansion, Speeds), 1e-9);
        }

        [Test]
        public void Speed_OfTheVeilleurDuSentier_IsFastInBreathingAndSlowInQi()
        {
            // §2.4: rapid in Embryonic Breathing, very slow in Qi Cultivation
            var veilleur = Catalog("path-watcher");
            Assert.AreEqual(0.8 * 1.5, TechniqueRules.CultivationSpeed(veilleur, CultivationRealm.Embryonic, Speeds), 1e-9);
            Assert.AreEqual(0.8 * 0.5, TechniqueRules.CultivationSpeed(veilleur, CultivationRealm.QiRefinement, Speeds), 1e-9);
        }

        // ---- Ceiling (§2.2) ----

        [TestCase(1, CultivationRealm.QiRefinement)]
        [TestCase(2, CultivationRealm.QiRefinement)]
        [TestCase(3, CultivationRealm.Foundation)]
        [TestCase(4, CultivationRealm.Foundation)]
        [TestCase(5, CultivationRealm.PurpleMansion)]
        [TestCase(6, CultivationRealm.GoldenCore)]
        [TestCase(7, CultivationRealm.GoldenImmortal)]
        public void Ceiling_FollowsTheGradeTable(int grade, CultivationRealm ceiling)
        {
            Assert.AreEqual(ceiling, TechniqueRules.Ceiling(Method(grade)));
        }

        [Test]
        public void Ceiling_OfTheVeilleurDuSentier_IsTheFoundationDespiteItsGrade()
        {
            Assert.AreEqual(CultivationRealm.Foundation, TechniqueRules.Ceiling(Catalog("path-watcher")));
        }

        [Test]
        public void Advance_ToTheFoundation_IsBarredToTheSouffleCommun()
        {
            // §11.2: the Souffle Commun stops at Qi Cultivation
            Assert.IsFalse(TechniqueRules.AllowsAdvance(Catalog("common-breath-method"), CultivationRealm.QiRefinement, CultivationRealm.Foundation));
            Assert.IsTrue(TechniqueRules.AllowsAdvance(Catalog("clear-spring-sutra"), CultivationRealm.QiRefinement, CultivationRealm.Foundation));
        }

        [Test]
        public void Advance_WithinARealm_NeedsAMethodCoveringIt()
        {
            Assert.IsTrue(TechniqueRules.AllowsAdvance(Catalog("clear-spring-sutra"), CultivationRealm.QiRefinement, CultivationRealm.QiRefinement));
            Assert.IsFalse(TechniqueRules.AllowsAdvance(null, CultivationRealm.QiRefinement, CultivationRealm.QiRefinement));
            Assert.IsTrue(TechniqueRules.AllowsAdvance(null, CultivationRealm.Embryonic, CultivationRealm.Embryonic));
        }

        // ---- Secret of ascent (§2.3) ----

        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(6, true)]
        public void PurpleMansionSecret_ComesWithGradeFive(int grade, bool secret)
        {
            Assert.AreEqual(secret, TechniqueRules.HasPurpleMansionSecret(Method(grade)));
        }

        [Test]
        public void PurpleMansionSecret_CanBeGivenByTheLore()
        {
            Assert.IsTrue(TechniqueRules.HasPurpleMansionSecret(Catalog("elder-knocking-sutra")));
        }

        // ---- Entering Qi Cultivation (§5.2) ----

        [Test]
        public void EnterQi_NeedsAMethodAndAPortionOfItsQi()
        {
            var clearSpring = Catalog("clear-spring-sutra");
            Assert.IsTrue(TechniqueRules.CanEnterQiCultivation(clearSpring, Qi("clear-spring-qi"), portions: 1));
            Assert.IsFalse(TechniqueRules.CanEnterQiCultivation(clearSpring, Qi("clear-spring-qi"), portions: 0));
            Assert.IsFalse(TechniqueRules.CanEnterQiCultivation(Catalog("white-lotus-intuition"), null, portions: 5));
        }

        [Test]
        public void EnterQi_WithTheSouffleCommun_NeedsNoPortion()
        {
            var commonQi = Qi("common-breath-qi");
            Assert.AreEqual(0, TechniqueRules.QiPortionsToEnter(commonQi));
            Assert.IsTrue(TechniqueRules.CanEnterQiCultivation(Catalog("common-breath-method"), commonQi, portions: 0));
        }

        // ---- Who may practise a method ----

        [Test]
        public void Practise_ABreathingMember_MayPrepareAQiMethod()
        {
            var child = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 5);
            Assert.IsTrue(TechniqueRules.CanPractise(child, Catalog("clear-spring-sutra")));
            Assert.IsTrue(TechniqueRules.CanPractise(child, Catalog("white-lotus-intuition")));
        }

        [Test]
        public void Practise_AQiCultivator_IsBoundToTheQiTheyAbsorbed()
        {
            // §2.5: Qi are never interchangeable
            var member = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 4);
            member.QiId = "clear-spring-qi";
            Assert.IsTrue(TechniqueRules.CanPractise(member, Catalog("clear-spring-sutra")));
            Assert.IsFalse(TechniqueRules.CanPractise(member, Catalog("upstream-brook-method")));
            Assert.IsFalse(TechniqueRules.CanPractise(member, Catalog("white-lotus-intuition")));
        }

        [Test]
        public void Practise_IsBeyondAMortal()
        {
            // LORE.md §4: without an orifice (or a Talisman Seed) one stays mortal and cultivates nothing
            Assert.IsFalse(TechniqueRules.CanPractise(Fixtures.Mortal(), Catalog("white-lotus-intuition")));
            Assert.IsFalse(TechniqueRules.CanPractise(Fixtures.Mortal(), Catalog("common-breath-method")));
        }

        [Test]
        public void Practise_NeverAnArt()
        {
            var member = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 3);
            Assert.IsFalse(TechniqueRules.CanPractise(member, new TechniqueData { Kind = TechniqueKind.Weapon, Grade = 3 }));
        }

        // ---- Flaws ----

        [Test]
        public void Lifespan_IsShortenedByAFlawedMethod()
        {
            Assert.AreEqual(114, TechniqueRules.LifespanWithMethod(120, Catalog("path-watcher")));
            Assert.AreEqual(120, TechniqueRules.LifespanWithMethod(120, Catalog("clear-spring-sutra")));
            Assert.AreEqual(PowerLadder.Unbounded, TechniqueRules.LifespanWithMethod(PowerLadder.Unbounded, Catalog("path-watcher")));
        }

        [Test]
        public void Counter_LeavesTheVeilleurPowerlessAgainstTheOriginalSutra()
        {
            Assert.IsTrue(TechniqueRules.IsPowerlessAgainst(Catalog("path-watcher"), "elder-knocking-sutra"));
            Assert.IsFalse(TechniqueRules.IsPowerlessAgainst(Catalog("path-watcher"), "clear-spring-sutra"));
            Assert.IsFalse(TechniqueRules.IsPowerlessAgainst(Catalog("clear-spring-sutra"), "elder-knocking-sutra"));
        }

        // ---- Deduction (fragments of quality 1-5 → grade) ----

        [TestCase(new[] { 1, 1 }, 1)]
        [TestCase(new[] { 3, 3 }, 3)]
        [TestCase(new[] { 3, 4, 4, 3 }, 4)]
        [TestCase(new[] { 5, 5, 5, 5 }, 6)]
        [TestCase(new[] { 5, 5, 5, 5, 4 }, 5)]
        [TestCase(new[] { 5, 5, 5, 5, 5 }, 7)]
        public void Deduction_GradeFollowsTheFragments(int[] qualities, int grade)
        {
            Assert.AreEqual(grade, TechniqueRules.DeductionGrade(qualities));
        }
    }
}
