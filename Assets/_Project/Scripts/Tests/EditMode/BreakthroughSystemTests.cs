using NUnit.Framework;
using UnityEngine;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    [TestFixture]
    public class BreakthroughSystemTests
    {
        private BreakthroughSystem system;
        private GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("TestBreakthroughSystem");
            system = go.AddComponent<BreakthroughSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        private CharacterData MakeCharacter(
            CultivationRealm realm = CultivationRealm.Embryonic,
            int spiritualRoot = 50,
            int mentalStability = 70,
            int age = 20,
            int maxLifespan = 100)
        {
            return new CharacterData
            {
                Realm = realm,
                SpiritualRoot = spiritualRoot,
                MentalStability = mentalStability,
                Age = age,
                MaxLifespan = maxLifespan,
                IsAlive = true
            };
        }

        [Test]
        public void CalculateSuccessRate_EmbryonicRealm_HighSuccess()
        {
            var ch = MakeCharacter(CultivationRealm.Embryonic, spiritualRoot: 50);
            int rate = system.CalculateSuccessRate(ch);
            // Embryonic base risk = 5 → base success = 95
            // root 50 > minRoot 10 → bonus (50-10)/5 = 8 → 95+8 = capped at 99
            Assert.That(rate, Is.GreaterThanOrEqualTo(90));
        }

        [Test]
        public void CalculateSuccessRate_GoldenCore_LowSuccess()
        {
            var ch = MakeCharacter(CultivationRealm.GoldenCore, spiritualRoot: 60);
            int rate = system.CalculateSuccessRate(ch);
            // Golden Core base risk = 70 → base success = 30
            // root 60 < minRoot 90 → no bonus
            Assert.That(rate, Is.LessThanOrEqualTo(35));
        }

        [Test]
        public void CalculateSuccessRate_LowMentalStability_Penalty()
        {
            var normal = MakeCharacter(CultivationRealm.QiRefinement, mentalStability: 70);
            var low = MakeCharacter(CultivationRealm.QiRefinement, mentalStability: 30);

            int rateNormal = system.CalculateSuccessRate(normal);
            int rateLow = system.CalculateSuccessRate(low);

            Assert.That(rateLow, Is.LessThan(rateNormal),
                $"Low MS rate {rateLow} should be < normal rate {rateNormal}");
        }

        [Test]
        public void CalculateSuccessRate_OldAge_Penalty()
        {
            var young = MakeCharacter(CultivationRealm.Foundation, age: 20, maxLifespan: 100);
            var old = MakeCharacter(CultivationRealm.Foundation, age: 90, maxLifespan: 100);

            int rateYoung = system.CalculateSuccessRate(young);
            int rateOld = system.CalculateSuccessRate(old);

            Assert.That(rateOld, Is.LessThan(rateYoung),
                $"Old rate {rateOld} should be < young rate {rateYoung}");
        }

        [Test]
        public void CalculateSuccessRate_AlwaysClampedBetween1And99()
        {
            // Best case: Embryonic, high root, good MS, young
            var best = MakeCharacter(CultivationRealm.Embryonic, spiritualRoot: 100, mentalStability: 100, age: 15);
            // Worst case: GoldenCore, low root, low MS, old
            var worst = MakeCharacter(CultivationRealm.GoldenCore, spiritualRoot: 1, mentalStability: 10, age: 95, maxLifespan: 100);

            int rateBest = system.CalculateSuccessRate(best);
            int rateWorst = system.CalculateSuccessRate(worst);

            Assert.That(rateBest, Is.InRange(1, 99), $"Best rate {rateBest}");
            Assert.That(rateWorst, Is.InRange(1, 99), $"Worst rate {rateWorst}");
        }
    }
}
