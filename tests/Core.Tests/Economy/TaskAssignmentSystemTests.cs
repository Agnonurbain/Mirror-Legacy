using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>Assigning the year's tasks and resolving them when the Management phase ends.</summary>
    [TestFixture]
    public class TaskAssignmentSystemTests
    {
        private static CharacterData Working(TestWorld w, TaskType task, CharacterData member = null)
        {
            member = w.Join(member ?? Fixtures.Cultivator());
            member.CurrentTask = task;
            return member;
        }

        [Test]
        public void GatherQi_HarvestsTheQiOfTheHarvestersMethod()
        {
            var w = new TestWorld();
            w.Techniques.Learn(Fixtures.ClanMethod);
            Working(w, TaskType.GatherQi); // a Qi cultivator of the Clear Spring

            var report = w.Tasks.ProcessYearlyTasks();

            Assert.IsTrue(report.QiPortionsGathered == 1 && w.Resources.QiPortions(Fixtures.ClanQi) == 1);
        }

        [Test]
        public void GatherQi_OfABreathingHarvester_TakesTheBestQiTheClanCanHarvest()
        {
            var w = new TestWorld();
            w.Techniques.Learn("common-breath-method"); // its Qi is everywhere: nothing to harvest
            w.Techniques.Learn("measured-rain-method");
            w.Techniques.Learn(Fixtures.ClanMethod);
            Working(w, TaskType.GatherQi, Fixtures.Cultivator(age: 20, realm: CultivationRealm.Embryonic, stage: 5));

            w.Tasks.ProcessYearlyTasks();

            Assert.AreEqual(1, w.Resources.QiPortions("measured-rain-qi"));
        }

        [Test]
        public void AssignTask_Refuses_CultivationForAMortal()
        {
            var w = new TestWorld();
            var mortal = w.Join(Fixtures.Mortal());
            bool assigned = w.Tasks.AssignTask(mortal, TaskType.Cultivation);
            Assert.IsTrue(!assigned && mortal.CurrentTask == TaskType.None);
        }

        [Test]
        public void AssignTask_SetsAnAllowedTask()
        {
            var w = new TestWorld();
            var member = w.Join(Fixtures.Cultivator());
            Assert.IsTrue(w.Tasks.AssignTask(member, TaskType.Patrol) && member.CurrentTask == TaskType.Patrol);
        }

        [Test]
        public void ProcessYearlyTasks_MinesFiftyPlusTwentyFivePerRealm()
        {
            var w = new TestWorld();
            Working(w, TaskType.Mine);
            var report = w.Tasks.ProcessYearlyTasks();
            Assert.IsTrue(report.StonesMined == 75 && w.Resources.SpiritStones == 1075);
        }

        [Test]
        public void ProcessYearlyTasks_MinesMore_WithAForge()
        {
            var w = new TestWorld();
            w.Buildings.Restore(new[] { new BuildingData(BuildingType.Forge) { Level = 1 } });
            Working(w, TaskType.Mine);
            Assert.AreEqual(79, w.Tasks.ProcessYearlyTasks().StonesMined); // 75 × 1.05
        }

        [Test]
        public void ProcessYearlyTasks_RestSteadiesTheMind()
        {
            var w = new TestWorld();
            var member = Working(w, TaskType.Rest);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(75, member.MentalStability);
        }

        [Test]
        public void ProcessYearlyTasks_StudyGivesXp_WhenNothingIsFound()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var scholar = Working(w, TaskType.Study);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(10, scholar.CultivationXP);
        }

        [Test]
        public void ProcessYearlyTasks_StudyFindsAFragment_WhenLucky()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Working(w, TaskType.Study);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(1, w.Deduction.Fragments.Count);
        }

        [Test]
        public void ProcessYearlyTasks_TeachingBoostsACultivatingStudent()
        {
            var w = new TestWorld();
            Working(w, TaskType.Teaching, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement));
            var student = Working(w, TaskType.Cultivation, Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 0));
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(35 + 30, student.CultivationXP); // own cultivation + teacher (20 + Qi realm × 10)
        }

        [Test]
        public void ProcessYearlyTasks_DiplomacyWarmsAFaction_MoreWithACouncilRoom()
        {
            var w = new TestWorld();
            w.Buildings.Restore(new[] { new BuildingData(BuildingType.CouncilRoom) { Level = 2 } });
            var faction = new FactionData { Name = "Neighbour" };
            w.Factions.AddFaction(faction);
            Working(w, TaskType.Diplomacy);
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(9, faction.RelationWithPlayer);
        }

        [Test]
        public void ProcessYearlyTasks_ClearsATaskTheMemberCannotPerform()
        {
            var w = new TestWorld();
            var mortal = Working(w, TaskType.Cultivation, Fixtures.Mortal());
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(TaskType.None, mortal.CurrentTask);
        }

        // ---- Hunting spirit beasts (user decision, 2026-09-26: the talisman ritual sacrifices beasts) ----

        // ---- The hunt (L2c.2): only a beast the clan knows, on its hunting ground, no stronger than the hunter ----

        /// <summary>A world with the hunt open and one beast on the hunting ground, known to the clan or not.</summary>
        private static (TestWorld w, WorldBeast beast) Ground(double roll, CultivationRealm realm = CultivationRealm.QiRefinement,
            int stage = 3, bool known = true, string owner = null)
        {
            var w = new TestWorld(new FixedRandom(roll));
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year);
            var beast = new WorldBeast("mist-wolf-1", "mist-wolf", w.Tasks.HuntingGround, realm, stage, owner);
            w.Bestiary.Restore(new[] { beast });
            if (known) w.Knowledge.Reveal(MirrorChronicles.World.FactKind.Beast, beast.Id, MirrorChronicles.World.KnowledgeSource.Studied);
            return (w, beast);
        }

        [Test]
        public void HuntBeast_OutsideTheWindow_CatchesNothing()
        {
            var (w, _) = Ground(0.0);
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year + 10);
            Working(w, TaskType.HuntBeast, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(0, w.Resources.Beasts.Count);
        }

        [Test]
        public void HuntBeast_CapturesAKnownBeast_OfTheGround()
        {
            var (w, beast) = Ground(0.0, owner: "Famille Lou");
            Working(w, TaskType.HuntBeast, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));

            w.Tasks.ProcessYearlyTasks();

            Assert.AreEqual(new CapturedBeast(beast.Id, beast.Realm, beast.Stage, "Famille Lou"), w.Resources.Beasts.Single());
            Assert.IsFalse(w.Bestiary.Beasts.Any(b => b.Id == beast.Id), "taken from the world");
        }

        [Test]
        public void HuntBeast_IgnoresABeastTheClanHasNotScouted()
        {
            var (w, _) = Ground(0.0, known: false);
            Working(w, TaskType.HuntBeast, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(0, w.Resources.Beasts.Count);
        }

        [Test]
        public void HuntBeast_CannotTakeAStrongerBeast()
        {
            var (w, _) = Ground(0.0, CultivationRealm.QiRefinement, stage: 8);
            Working(w, TaskType.HuntBeast, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(0, w.Resources.Beasts.Count);
        }

        [Test]
        public void HuntBeast_MayComeBackEmptyHanded()
        {
            var (w, _) = Ground(0.999);
            Working(w, TaskType.HuntBeast, Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5));
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(0, w.Resources.Beasts.Count);
        }

        [Test]
        public void ScoutBeasts_RevealsABeastOfTheGround()
        {
            var (w, beast) = Ground(0.0, known: false);
            Working(w, TaskType.ScoutBeasts);

            w.Tasks.ProcessYearlyTasks();

            Assert.IsTrue(w.Knowledge.Knows(MirrorChronicles.World.FactKind.Beast, beast.Id));
        }

        [Test]
        public void SetHuntingGround_Refuses_APlaceOffTheMap()
        {
            var w = new TestWorld();
            Assert.IsFalse(w.Tasks.SetHuntingGround("atlantis"));
            Assert.IsFalse(w.Tasks.SetHuntingGround("linxi"), "a whole state is no hunting ground");
            Assert.AreEqual(Fixtures.Content.Clan.HomeRegion, w.Tasks.HuntingGround);
        }
    }
}
