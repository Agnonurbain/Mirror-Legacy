using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>Every member who ever lived, for the family tree and the Annals.</summary>
    [TestFixture]
    public class BloodRegistryTests
    {
        [Test]
        public void Register_IgnoresDuplicates()
        {
            var registry = new BloodRegistry();
            var member = new CharacterData();
            registry.Register(member);
            registry.Register(member);
            Assert.AreEqual(1, registry.Records.Count);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown")]
        public void FindById_ReturnsNull_WhenTheIdIsMissingOrUnknown(string id)
        {
            Assert.IsNull(new BloodRegistry().FindById(id));
        }

        [Test]
        public void ChildrenOf_ReturnsTheChildrenOfEitherParent()
        {
            var registry = new BloodRegistry();
            var mother = new CharacterData();
            var child = new CharacterData { MotherID = mother.ID };
            registry.Register(mother);
            registry.Register(child);
            registry.Register(new CharacterData());
            CollectionAssert.AreEqual(new[] { child }, registry.ChildrenOf(mother.ID));
        }
    }
}
