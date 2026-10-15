using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The lore audit's basic cultivation (AUDIT_LORE.md §7, 2026-10-15): an orifice nobody examined does not cultivate; only a
    /// free examiner at the domain examines; a breathing cultivator does at least what a mortal does; a spouse comes with a method
    /// and its Qi.
    /// </summary>
    [TestFixture]
    public class BasicCultivationAuditTests
    {
        private static TestWorld World(double sample = 0.99) => new TestWorld(new FixedRandom(sample));

        private static CharacterData Breathing(int age, int chakra) =>
            Fixtures.Cultivator(age: age, realm: CultivationRealm.Embryonic, stage: chakra);

        // ---- 7.1 An orifice nobody examined ----

        [Test]
        public void AnUnexaminedOrifice_DoesNotCultivate()
        {
            var w = World();
            var child = w.Join(Breathing(age: 10, chakra: 0));
            child.OrificeKnown = false;
            child.CurrentTask = TaskType.Cultivation;
            CollectionAssert.DoesNotContain(TaskRules.AllowedTasks(child), TaskType.Cultivation, "the task list must not betray it");
            w.Cultivation.ProcessYearlyCultivation(child, 1.0);
            Assert.AreEqual(0, child.CultivationXP);
        }

        [Test]
        public void ATalismanSeed_Cultivates_ThoughNobodyExaminedIt()
        {
            var seeded = Breathing(age: 20, chakra: 1);
            seeded.HasSpiritualOrifice = false;
            seeded.HasTalismanSeed = true;
            seeded.OrificeKnown = false;
            CollectionAssert.Contains(TaskRules.AllowedTasks(seeded), TaskType.Cultivation);
        }

        // ---- 7.2 The examiner ----

        private static (TestWorld w, CharacterData examiner, CharacterData newborn) Examination()
        {
            var w = World();
            var examiner = w.Join(Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 2));
            var newborn = w.Join(Breathing(age: 1, chakra: 0));
            newborn.OrificeKnown = false;
            return (w, examiner, newborn);
        }

        [Test]
        public void AFreeExaminerAtTheDomain_ExaminesTheNewborns()
        {
            var (w, _, newborn) = Examination();
            Assert.AreEqual(1, w.Clan.ExamineOrifices());
            Assert.IsTrue(newborn.OrificeKnown);
        }

        [Test]
        public void ACaptiveExaminer_ExaminesNobody()
        {
            var (w, examiner, newborn) = Examination();
            examiner.CaptorFaction = "Secte du Pic des Nuées";
            Assert.AreEqual(0, w.Clan.ExamineOrifices());
            Assert.IsFalse(newborn.OrificeKnown);
        }

        [Test]
        public void AnExaminerAway_ExaminesNobody()
        {
            var (w, examiner, newborn) = Examination();
            examiner.CurrentTask = TaskType.Diplomacy;
            Assert.AreEqual(0, w.Clan.ExamineOrifices());
            Assert.IsFalse(newborn.OrificeKnown);
        }

        // ---- 7.4 The breathing cultivator's tasks ----

        [Test]
        public void ABreathingAdult_DoesWhatAMortalDoes()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 20, chakra: 1));
            CollectionAssert.IsSubsetOf(TaskRules.AllowedTasks(Fixtures.Mortal(age: 20)), tasks);
            CollectionAssert.Contains(tasks, TaskType.Cultivation);
        }

        [Test]
        public void ABreathingCultivator_Spies_FromTheThirdChakra()
        {
            CollectionAssert.DoesNotContain(TaskRules.AllowedTasks(Breathing(age: 20, chakra: 2)), TaskType.Espionage);
            CollectionAssert.Contains(TaskRules.AllowedTasks(Breathing(age: 20, chakra: 3)), TaskType.Espionage);
        }

        [Test]
        public void ABreathingChild_StillOnlyCultivatesAndRests()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 10, chakra: 3));
            CollectionAssert.DoesNotContain(tasks, TaskType.Mine);
            CollectionAssert.DoesNotContain(tasks, TaskType.Espionage);
            CollectionAssert.Contains(tasks, TaskType.Cultivation);
        }

        [Test]
        public void TheSummitEye_StillGathersQi_AndDoesTheRest()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 20, chakra: TaskRules.QiPerceptionChakra));
            CollectionAssert.Contains(tasks, TaskType.GatherQi);
            CollectionAssert.Contains(tasks, TaskType.Espionage);
            CollectionAssert.Contains(tasks, TaskType.Mine);
        }

        // ---- 7.5 A spouse comes with its method ----

        [Test]
        public void ASpouseOfAPower_PractisesAMethodOfThatPower_OnItsQi()
        {
            var w = World();
            w.Factions.InitializeFactions();
            var power = w.Factions.Factions.First(f => f.FamilyName != null
                && f.Techniques.Select(id => Fixtures.Content.Techniques.FirstOrDefault(t => t.ID == id))
                    .Any(t => t != null && TechniqueRules.Covers(t, CultivationRealm.QiRefinement)));
            var member = w.Join(Fixtures.Cultivator(age: 25));
            Assert.IsTrue(w.Marriages.HandleArrangedMarriage(member, power.ID, isForced: false));
            var spouse = w.Clan.FindById(member.SpouseID);
            CollectionAssert.Contains(power.Techniques, spouse.CultivationMethodId);
            var method = Fixtures.Content.Techniques.Single(t => t.ID == spouse.CultivationMethodId);
            Assert.AreEqual(method.RequiredQiId, spouse.QiId);
            Assert.IsTrue(TechniqueRules.Covers(method, spouse.Realm));
        }

        [Test]
        public void AWanderingSpouse_PractisesTheClansMethod()
        {
            var w = World(0.0);
            w.Techniques.Learn(Fixtures.ClanMethod);
            w.Resources.AddSpiritStones(10000);
            var member = w.Join(Fixtures.Cultivator(age: 25));
            Assert.IsNull(w.Marriages.SeekCultivatorSpouse(member.ID));
            var spouse = w.Clan.FindById(member.SpouseID);
            Assert.AreEqual(Fixtures.ClanMethod, spouse.CultivationMethodId);
            Assert.AreEqual(Fixtures.ClanQi, spouse.QiId);
        }
    }
}
