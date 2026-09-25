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
        private const int Adult = 30;

        [Test]
        public void GatherQi_OpensWithTheSummitEye()
        {
            // LORE.md §5.1: the fifth chakra perceives spiritual Qi
            var blind = new CharacterData { Age = Adult, HasSpiritualOrifice = true, RealmStage = 4 };
            var seeing = new CharacterData { Age = Adult, HasSpiritualOrifice = true, RealmStage = 5 };
            Assert.IsFalse(TaskRules.IsAllowed(blind, TaskType.GatherQi));
            Assert.IsTrue(TaskRules.IsAllowed(seeing, TaskType.GatherQi));
        }

        [Test]
        public void GatherQi_IsBeyondAMortal()
        {
            Assert.IsFalse(TaskRules.IsAllowed(new CharacterData { Age = Adult }, TaskType.GatherQi));
        }

        [Test]
        public void AllowedTasks_ExcludesCultivation_WhenMortal()
        {
            var tasks = TaskRules.AllowedTasks(new CharacterData { Age = Adult });
            Assert.IsTrue(!tasks.Contains(TaskType.Cultivation) && tasks.Contains(TaskType.Mine));
        }

        [Test]
        public void AllowedTasks_AreCultivationAndRest_WhenEmbryonicCultivator()
        {
            var c = new CharacterData { Age = Adult, HasSpiritualOrifice = true, RealmStage = 2 };
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Cultivation, TaskType.Rest }, TaskRules.AllowedTasks(c));
        }

        [Test]
        public void AllowedTasks_IncludesEveryTask_WhenQiCultivator()
        {
            var c = new CharacterData { Age = Adult, HasSpiritualOrifice = true, Realm = CultivationRealm.QiRefinement, RealmStage = 1 };
            Assert.AreEqual(System.Enum.GetValues(typeof(TaskType)).Length, TaskRules.AllowedTasks(c).Count);
        }

        [Test]
        public void IsAllowed_ReturnsFalse_WhenMortalAssignedToCultivation()
        {
            Assert.IsFalse(TaskRules.IsAllowed(new CharacterData { Age = Adult }, TaskType.Cultivation));
        }

        [Test]
        public void AllowedTasks_IsNothing_BeforeTheAgeOfSix()
        {
            var infant = new CharacterData { Age = 5, HasSpiritualOrifice = true, OrificeKnown = true };
            CollectionAssert.AreEquivalent(new[] { TaskType.None }, TaskRules.AllowedTasks(infant));
        }

        [Test]
        public void AllowedTasks_LetsAMortalChildOnlyRest()
        {
            var child = new CharacterData { Age = 10 };
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Rest }, TaskRules.AllowedTasks(child));
        }

        [Test]
        public void AllowedTasks_LetsAGiftedChildCultivate()
        {
            var child = new CharacterData { Age = 8, HasSpiritualOrifice = true, OrificeKnown = true };
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Cultivation, TaskType.Rest }, TaskRules.AllowedTasks(child));
        }
    }
}
