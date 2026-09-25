using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Which tasks a member may take: mortals work, cultivators cultivate (LORE.md §4).
    /// </summary>
    [TestFixture]
    public class TaskRulesTests
    {
        [Test]
        public void AllowedTasks_ExcludesCultivation_WhenMortal()
        {
            var tasks = TaskRules.AllowedTasks(new CharacterData());
            Assert.IsTrue(!tasks.Contains(TaskType.Cultivation) && tasks.Contains(TaskType.Mine));
        }

        [Test]
        public void AllowedTasks_AreCultivationAndRest_WhenEmbryonicCultivator()
        {
            var c = new CharacterData { HasSpiritualOrifice = true, RealmStage = 2 };
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Cultivation, TaskType.Rest }, TaskRules.AllowedTasks(c));
        }

        [Test]
        public void AllowedTasks_IncludesEveryTask_WhenQiCultivator()
        {
            var c = new CharacterData { HasSpiritualOrifice = true, Realm = CultivationRealm.QiRefinement, RealmStage = 1 };
            Assert.AreEqual(System.Enum.GetValues(typeof(TaskType)).Length, TaskRules.AllowedTasks(c).Count);
        }

        [Test]
        public void IsAllowed_ReturnsFalse_WhenMortalAssignedToCultivation()
        {
            Assert.IsFalse(TaskRules.IsAllowed(new CharacterData(), TaskType.Cultivation));
        }
    }
}
