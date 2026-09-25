using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>Clan buildings: eight types, five levels, passive bonuses each new year.</summary>
    [TestFixture]
    public class BuildingSystemTests
    {
        private static TestWorld WithLevel(BuildingType type, int level)
        {
            var w = new TestWorld();
            w.Buildings.Restore(new[] { new BuildingData(type) { Level = level } });
            return w;
        }

        [Test]
        public void NewClan_HasEveryBuildingAtLevelZero()
        {
            var w = new TestWorld();
            Assert.IsTrue(w.Buildings.Buildings.Count == Enum.GetValues(typeof(BuildingType)).Length
                && w.Buildings.Buildings.All(b => b.Level == 0));
        }

        [Test]
        public void Upgrade_PaysAndRaisesTheLevel()
        {
            var w = new TestWorld();
            bool built = w.Buildings.Upgrade(BuildingType.Mine);
            Assert.IsTrue(built && w.Buildings.GetBuilding(BuildingType.Mine).Level == 1 && w.Resources.SpiritStones == 800);
        }

        [Test]
        public void Upgrade_Refuses_WhenTooPoor()
        {
            var w = new TestWorld();
            w.Resources.SetSpiritStones(100);
            Assert.IsFalse(w.Buildings.Upgrade(BuildingType.Mine));
        }

        [Test]
        public void Upgrade_Refuses_WithoutAMemberOfTheRequiredRealm()
        {
            var w = new TestWorld();
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement));
            Assert.IsFalse(w.Buildings.Upgrade(BuildingType.MeditationPagoda)); // needs the Purple Mansion
        }

        [Test]
        public void Upgrade_StopsAtLevelFive()
        {
            var w = WithLevel(BuildingType.Mine, 5);
            w.Resources.SetSpiritStones(100000);
            Assert.IsFalse(w.Buildings.Upgrade(BuildingType.Mine));
        }

        [Test]
        public void ApplyPassiveBonuses_MineYieldsTwentyStonesPerLevel()
        {
            var w = WithLevel(BuildingType.Mine, 2);
            w.Buildings.ApplyPassiveBonuses();
            Assert.AreEqual(1040, w.Resources.SpiritStones);
        }

        [Test]
        public void ApplyPassiveBonuses_TrainingHallTrainsTheCultivating()
        {
            var w = WithLevel(BuildingType.TrainingHall, 3);
            var disciple = w.Join(Fixtures.Cultivator());
            disciple.CurrentTask = TaskType.Cultivation;
            w.Buildings.ApplyPassiveBonuses();
            Assert.AreEqual(6, disciple.CultivationXP);
        }

        [Test]
        public void ApplyPassiveBonuses_MeditationPagodaSteadiesEveryone()
        {
            var w = WithLevel(BuildingType.MeditationPagoda, 2);
            var member = w.Join(Fixtures.Cultivator());
            w.Buildings.ApplyPassiveBonuses();
            Assert.AreEqual(72, member.MentalStability);
        }
    }
}
