using NUnit.Framework;
using UnityEngine;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    [TestFixture]
    public class MentalStabilitySystemTests
    {
        private MentalStabilitySystem system;
        private GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("TestMentalStabilitySystem");
            system = go.AddComponent<MentalStabilitySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyModifier_PositiveIncrease()
        {
            var ch = new CharacterData { MentalStability = 50, IsAlive = true };
            system.ApplyModifier(ch, 20);
            Assert.AreEqual(70, ch.MentalStability);
        }

        [Test]
        public void ApplyModifier_NegativeDecrease()
        {
            var ch = new CharacterData { MentalStability = 50, IsAlive = true };
            system.ApplyModifier(ch, -30);
            Assert.AreEqual(20, ch.MentalStability);
        }

        [Test]
        public void ApplyModifier_ClampsToZero()
        {
            var ch = new CharacterData { MentalStability = 10, IsAlive = true };
            system.ApplyModifier(ch, -50);
            Assert.AreEqual(0, ch.MentalStability);
        }

        [Test]
        public void ApplyModifier_ClampsTo100()
        {
            var ch = new CharacterData { MentalStability = 90, IsAlive = true };
            system.ApplyModifier(ch, 50);
            Assert.AreEqual(100, ch.MentalStability);
        }

        [Test]
        public void ApplyModifier_DeadCharacter_Ignored()
        {
            var ch = new CharacterData { MentalStability = 50, IsAlive = false };
            system.ApplyModifier(ch, 30);
            Assert.AreEqual(50, ch.MentalStability, "Dead character's stability should not change");
        }

        [Test]
        public void ApplyModifier_ZeroAmount_NoChange()
        {
            var ch = new CharacterData { MentalStability = 70, IsAlive = true };
            system.ApplyModifier(ch, 0);
            Assert.AreEqual(70, ch.MentalStability);
        }
    }
}
