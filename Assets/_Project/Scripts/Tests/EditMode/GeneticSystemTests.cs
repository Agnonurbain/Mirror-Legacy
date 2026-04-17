using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    [TestFixture]
    public class GeneticSystemTests
    {
        private CharacterData MakeParent(int root, Element affinity)
        {
            return new CharacterData { SpiritualRoot = root, Affinity = affinity };
        }

        [Test]
        public void GenerateSpiritualRoot_AlwaysClampedBetween1And100()
        {
            var father = MakeParent(50, Element.Fire);
            var mother = MakeParent(50, Element.Water);

            for (int i = 0; i < 500; i++)
            {
                int root = GeneticSystem.GenerateSpiritualRoot(father, mother);
                Assert.That(root, Is.InRange(1, 100),
                    $"Iteration {i}: root {root} out of bounds");
            }
        }

        [Test]
        public void GenerateSpiritualRoot_ExtremeParents_StillClamped()
        {
            var low = MakeParent(1, Element.None);
            var high = MakeParent(100, Element.None);

            for (int i = 0; i < 200; i++)
            {
                int rootLow = GeneticSystem.GenerateSpiritualRoot(low, low);
                int rootHigh = GeneticSystem.GenerateSpiritualRoot(high, high);
                Assert.That(rootLow, Is.InRange(1, 100));
                Assert.That(rootHigh, Is.InRange(1, 100));
            }
        }

        [Test]
        public void GenerateSpiritualRoot_AverageNearParentMean()
        {
            var father = MakeParent(60, Element.Fire);
            var mother = MakeParent(40, Element.Water);
            int expectedMean = 50;

            long sum = 0;
            int n = 2000;
            for (int i = 0; i < n; i++)
                sum += GeneticSystem.GenerateSpiritualRoot(father, mother);

            float average = sum / (float)n;
            Assert.That(average, Is.InRange(expectedMean - 5f, expectedMean + 5f),
                $"Average {average:F1} should be near {expectedMean} (±5)");
        }

        [Test]
        public void GenerateSpiritualRoot_NullParent_UsesDefault10()
        {
            var mother = MakeParent(90, Element.Fire);

            long sum = 0;
            int n = 2000;
            for (int i = 0; i < n; i++)
                sum += GeneticSystem.GenerateSpiritualRoot(null, mother);

            float average = sum / (float)n;
            float expectedMean = (10 + 90) / 2f; // null father defaults to 10
            Assert.That(average, Is.InRange(expectedMean - 5f, expectedMean + 5f));
        }

        [Test]
        public void GenerateAffinity_NeverReturnsNone()
        {
            var father = MakeParent(50, Element.Fire);
            var mother = MakeParent(50, Element.Water);

            for (int i = 0; i < 500; i++)
            {
                Element aff = GeneticSystem.GenerateAffinity(father, mother);
                Assert.That(aff, Is.Not.EqualTo(Element.None),
                    $"Iteration {i}: got Element.None");
            }
        }

        [Test]
        public void GenerateAffinity_InheritsFromParents_MostOfTheTime()
        {
            var father = MakeParent(50, Element.Lightning);
            var mother = MakeParent(50, Element.Metal);

            int inherited = 0;
            int n = 1000;
            for (int i = 0; i < n; i++)
            {
                Element aff = GeneticSystem.GenerateAffinity(father, mother);
                if (aff == Element.Lightning || aff == Element.Metal)
                    inherited++;
            }

            float rate = inherited / (float)n;
            Assert.That(rate, Is.GreaterThan(0.50f),
                $"Inheritance rate {rate:P0} should be > 50% (expected ~70%)");
        }
    }
}
