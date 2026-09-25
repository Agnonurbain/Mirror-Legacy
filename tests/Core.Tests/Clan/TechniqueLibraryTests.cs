using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>What the clan knows (catalog techniques and the mirror's deductions) and who practises which method.</summary>
    [TestFixture]
    public class TechniqueLibraryTests
    {
        private static TechniqueLibrary Library(params string[] known)
        {
            var library = new TechniqueLibrary(Fixtures.Context());
            foreach (var id in known) library.Learn(id);
            return library;
        }

        [Test]
        public void Learn_MakesACatalogTechniqueKnown()
        {
            var library = Library("clear-spring-sutra");
            Assert.IsTrue(library.Knows("clear-spring-sutra") && !library.Knows("seven-terraces-canon"));
        }

        [Test]
        public void Learn_Refuses_AnIdTheCatalogLacks()
        {
            var library = Library();
            Assert.IsFalse(library.Learn("lost-scroll"));
            Assert.IsFalse(library.Knows("lost-scroll"));
        }

        [Test]
        public void Find_ReachesTheWholeCatalogAndTheDeductions()
        {
            var library = Library();
            var deduced = new TechniqueData { ID = "deduced-1", Kind = TechniqueKind.Weapon, Grade = 2 };
            library.AddDeduced(deduced);

            Assert.IsNotNull(library.Find("seven-terraces-canon")); // known or not: the world's techniques exist
            Assert.AreSame(deduced, library.Find("deduced-1"));
            Assert.IsTrue(library.Knows("deduced-1"));
        }

        [Test]
        public void MethodsFor_ListsTheKnownMethodsAMemberCanPractise_BestGradeFirst()
        {
            var library = Library("clear-spring-sutra", "common-breath-method", "white-lotus-intuition");
            var child = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 3);

            CollectionAssert.AreEqual(new[] { "white-lotus-intuition", "clear-spring-sutra", "common-breath-method" },
                library.MethodsFor(child).Select(t => t.ID));
        }

        [Test]
        public void AssignMethod_Refuses_AMethodTheClanDoesNotKnow()
        {
            var library = Library("clear-spring-sutra");
            var child = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 3);
            Assert.IsFalse(library.AssignMethod(child, "upstream-brook-method"));
            Assert.IsNull(child.CultivationMethodId);
        }

        [Test]
        public void AssignMethod_Refuses_AMethodOfAnotherQi()
        {
            var library = Library("clear-spring-sutra", "upstream-brook-method");
            var member = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 2); // absorbed Clear Spring Qi
            Assert.IsFalse(library.AssignMethod(member, "upstream-brook-method"));
            Assert.AreEqual("clear-spring-sutra", member.CultivationMethodId);
        }

        [Test]
        public void AssignMethod_SetsTheMethodAndTeachesIt()
        {
            var library = Library("clear-spring-sutra");
            var child = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 3);

            Assert.IsTrue(library.AssignMethod(child, "clear-spring-sutra"));
            Assert.IsTrue(child.CultivationMethodId == "clear-spring-sutra" && child.KnownTechniqueIDs.Contains("clear-spring-sutra"));
        }

        [Test]
        public void AssignMethod_OfAFlawedMethod_ShortensTheLifespanOnlyOnce()
        {
            var library = Library("path-watcher", "white-lotus-intuition");
            var child = Fixtures.Cultivator(age: 12, realm: CultivationRealm.Embryonic, stage: 3); // 120 years

            library.AssignMethod(child, "path-watcher");
            library.AssignMethod(child, "white-lotus-intuition");
            library.AssignMethod(child, "path-watcher");

            Assert.AreEqual(114, child.MaxLifespan); // 120 × 0.95, not compounded
        }
    }
}
