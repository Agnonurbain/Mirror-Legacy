using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.Tests.EditMode
{
    /// <summary>
    /// MarriageSystem.CanMarry applies the 3-generation kinship rule (needs the Unity editor).
    /// </summary>
    [TestFixture]
    public class MarriageSystemTests
    {
        private Dictionary<string, CharacterData> registry;

        [SetUp]
        public void SetUp()
        {
            registry = new Dictionary<string, CharacterData>();
        }

        private CharacterData Person(CharacterData father = null, CharacterData mother = null)
        {
            var person = new CharacterData { Age = 30, FatherID = father?.ID, MotherID = mother?.ID };
            registry[person.ID] = person;
            return person;
        }

        [Test]
        public void CanMarry_ReturnsFalse_WhenParentAndChild()
        {
            var go = new GameObject("TestMarriageSystem");
            try
            {
                var marriage = go.AddComponent<MarriageSystem>();
                var father = Person();
                var daughter = Person(father);
                Assert.IsFalse(marriage.CanMarry(father, daughter));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CanMarry_ReturnsTrue_WhenUnrelatedAdults()
        {
            var go = new GameObject("TestMarriageSystem");
            try
            {
                var marriage = go.AddComponent<MarriageSystem>();
                Assert.IsTrue(marriage.CanMarry(Person(Person(), Person()), Person(Person(), Person())));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
