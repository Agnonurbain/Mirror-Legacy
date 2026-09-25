using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.EditMode
{
    /// <summary>
    /// Business rule (MEMORY.md): marriage is forbidden between relatives
    /// sharing an ancestor within 3 generations.
    /// </summary>
    [TestFixture]
    public class KinshipRulesTests
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

        private CharacterData FindById(string id)
        {
            return registry.TryGetValue(id, out var person) ? person : null;
        }

        private bool Related(CharacterData a, CharacterData b)
        {
            return KinshipRules.AreCloseKin(a, b, KinshipRules.MarriageForbiddenGenerations, FindById);
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenSiblings()
        {
            var father = Person();
            var mother = Person();
            Assert.IsTrue(Related(Person(father, mother), Person(father, mother)));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenHalfSiblings()
        {
            var father = Person();
            Assert.IsTrue(Related(Person(father, Person()), Person(father, Person())));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenParentAndChild()
        {
            var father = Person();
            Assert.IsTrue(Related(father, Person(father)));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenGrandparentAndGrandchild()
        {
            var grandmother = Person();
            var grandchild = Person(Person(null, grandmother));
            Assert.IsTrue(Related(grandchild, grandmother));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenUncleAndNiece()
        {
            var grandfather = Person();
            var uncle = Person(grandfather);
            var niece = Person(Person(grandfather));
            Assert.IsTrue(Related(uncle, niece));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenFirstCousins()
        {
            var grandfather = Person();
            var cousinA = Person(Person(grandfather));
            var cousinB = Person(null, Person(grandfather));
            Assert.IsTrue(Related(cousinA, cousinB));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenSecondCousins()
        {
            var greatGrandfather = Person();
            var cousinA = Person(Person(Person(greatGrandfather)));
            var cousinB = Person(Person(Person(greatGrandfather)));
            Assert.IsTrue(Related(cousinA, cousinB));
        }

        [Test]
        public void AreCloseKin_ReturnsFalse_WhenThirdCousins()
        {
            var ancestor = Person();
            var cousinA = Person(Person(Person(Person(ancestor))));
            var cousinB = Person(Person(Person(Person(ancestor))));
            Assert.IsFalse(Related(cousinA, cousinB));
        }

        [Test]
        public void AreCloseKin_ReturnsFalse_WhenGreatGreatGrandparent()
        {
            var ancestor = Person();
            var descendant = Person(Person(Person(Person(ancestor))));
            Assert.IsFalse(Related(ancestor, descendant));
        }

        [Test]
        public void AreCloseKin_ReturnsFalse_WhenUnrelated()
        {
            Assert.IsFalse(Related(Person(Person(), Person()), Person(Person(), Person())));
        }

        [Test]
        public void AreCloseKin_ReturnsTrue_WhenParentAndChildWithoutRegistryLookup()
        {
            var mother = Person();
            var child = Person(null, mother);
            Assert.IsTrue(KinshipRules.AreCloseKin(mother, child, KinshipRules.MarriageForbiddenGenerations, null));
        }

        [Test]
        public void AreCloseKin_ReturnsFalse_WhenCharacterIsNull()
        {
            Assert.IsFalse(Related(Person(), null));
        }
    }
}
