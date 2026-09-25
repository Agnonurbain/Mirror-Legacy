using System;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>Spiritual root and elemental affinity of newborns (ported from the Unity EditMode tests).</summary>
    [TestFixture]
    public class GeneticSystemTests
    {
        private const int Samples = 2000;
        private Random rng;

        [SetUp]
        public void SetUp() => rng = new Random(7);

        private static CharacterData Parent(int root, Element affinity = Element.Fire) =>
            new CharacterData { SpiritualRoot = root, Affinity = affinity };

        [Test]
        public void GenerateSpiritualRoot_AlwaysStaysBetweenOneAndHundred()
        {
            for (int i = 0; i < Samples; i++)
                Assert.That(GeneticSystem.GenerateSpiritualRoot(Parent(50), Parent(60), rng), Is.InRange(1, 100));
        }

        [Test]
        public void GenerateSpiritualRoot_ClampsExtremeParents()
        {
            for (int i = 0; i < Samples; i++)
            {
                Assert.That(GeneticSystem.GenerateSpiritualRoot(Parent(1), Parent(1), rng), Is.InRange(1, 100));
                Assert.That(GeneticSystem.GenerateSpiritualRoot(Parent(100), Parent(100), rng), Is.InRange(1, 100));
            }
        }

        [Test]
        public void GenerateSpiritualRoot_AveragesNearTheParentsMean()
        {
            double total = 0;
            for (int i = 0; i < Samples; i++) total += GeneticSystem.GenerateSpiritualRoot(Parent(40), Parent(60), rng);
            Assert.That(total / Samples, Is.InRange(45.0, 55.0));
        }

        [Test]
        public void GenerateSpiritualRoot_UsesTen_ForAMissingParent()
        {
            double total = 0;
            for (int i = 0; i < Samples; i++) total += GeneticSystem.GenerateSpiritualRoot(Parent(50), null, rng);
            Assert.That(total / Samples, Is.InRange(25.0, 35.0));
        }

        [Test]
        public void GenerateAffinity_NeverReturnsNone()
        {
            for (int i = 0; i < Samples; i++)
                Assert.AreNotEqual(Element.None, GeneticSystem.GenerateAffinity(Parent(50, Element.None), null, rng));
        }

        [Test]
        public void GenerateAffinity_InheritsFromAParent_MostOfTheTime()
        {
            int inherited = 0;
            for (int i = 0; i < Samples; i++)
            {
                var affinity = GeneticSystem.GenerateAffinity(Parent(50, Element.Light), Parent(50, Element.Darkness), rng);
                if (affinity == Element.Light || affinity == Element.Darkness) inherited++;
            }
            Assert.That((double)inherited / Samples, Is.GreaterThan(0.5));
        }
    }
}
