using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>
    /// The clan's upkeep (user decision 2026-09-29, after the long games): every member costs stones each year — a mortal
    /// little, a cultivator more by its realm. A clan that cannot pay is impoverished: its stones gone, its members
    /// shaken, few children born that year. The veins of the mine are few (more with the Mine building): the miners beyond
    /// them yield little. So the clan grows only as far as it can feed itself.
    /// </summary>
    [TestFixture]
    public class UpkeepTests
    {
        private static UpkeepSettings Settings => Fixtures.Content.Balance.Upkeep;

        [Test]
        public void EveryMember_CostsItsUpkeep_ByRealm()
        {
            var w = new TestWorld();
            var mortal = w.Join(Fixtures.Mortal());
            mortal.HasSpiritualOrifice = false;
            var cultivator = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            Assert.AreEqual(Settings.MortalStones, w.Upkeep.UpkeepOf(mortal));
            Assert.AreEqual(Settings.CultivatorStones + (int)CultivationRealm.Foundation * Settings.StonesPerRealm, w.Upkeep.UpkeepOf(cultivator));
        }

        [Test]
        public void AClanThatCanPay_Pays()
        {
            var w = new TestWorld();
            w.Join(Fixtures.Cultivator());
            int stones = w.Resources.SpiritStones, due = w.Upkeep.YearlyUpkeep;
            w.Upkeep.PayUpkeep();
            Assert.AreEqual(stones - due, w.Resources.SpiritStones);
            Assert.IsFalse(w.Upkeep.Impoverished);
            Assert.Greater(w.Upkeep.BirthFactor, Settings.PovertyBirthFactor);
        }

        [Test]
        public void AClanThatCannotPay_IsImpoverished()
        {
            var w = new TestWorld();
            var member = w.Join(Fixtures.Cultivator());
            int stability = member.MentalStability;
            w.Resources.SetSpiritStones(w.Upkeep.YearlyUpkeep - 1);

            w.Upkeep.PayUpkeep();

            Assert.AreEqual(0, w.Resources.SpiritStones);
            Assert.IsTrue(w.Upkeep.Impoverished);
            Assert.AreEqual(Settings.PovertyBirthFactor, w.Upkeep.BirthFactor);
            Assert.Less(member.MentalStability, stability);
        }

        [Test]
        public void APoorYear_BringsFewChildren()
        {
            var w = new TestWorld(new FixedRandom(0.0)); // every couple would have a child
            var father = w.Join(Fixtures.Cultivator(age: 25));
            var mother = w.Join(Fixtures.Cultivator(isMale: false, age: 25));
            father.SpouseID = mother.ID;
            mother.SpouseID = father.ID;
            Assert.AreEqual(0, w.Clan.ProcessAnnualBirths(birthFactor: 0));
            Assert.AreEqual(1, w.Clan.ProcessAnnualBirths(birthFactor: 1));
        }

        [Test]
        public void AThinReserve_BringsFewerChildren_BeforeAnyPoorYear()
        {
            var w = new TestWorld();
            w.Join(Fixtures.Cultivator());
            int due = w.Upkeep.YearlyUpkeep;
            w.Resources.SetSpiritStones(due * Settings.ProsperityYears * 10);
            w.Upkeep.PayUpkeep();
            Assert.AreEqual(1.0, w.Upkeep.BirthFactor, "a prosperous clan");

            w.Resources.SetSpiritStones(due + due * Settings.ProsperityYears / 2);
            w.Upkeep.PayUpkeep();
            Assert.IsFalse(w.Upkeep.Impoverished);
            Assert.AreEqual(0.5, w.Upkeep.BirthFactor, 0.05, "half the reserve, half the children");
        }

        [Test]
        public void TheMinersBeyondTheVeins_YieldLittle()
        {
            var w = new TestWorld();
            int veins = Settings.VeinMiners;
            for (int i = 0; i < veins + 10; i++) w.Tasks.AssignTask(w.Join(Fixtures.Mortal()), TaskType.Mine);
            var report = w.Tasks.ProcessYearlyTasks();
            int full = TaskAssignmentSystem.MineBaseYield;
            Assert.AreEqual(veins * full + 10 * (int)System.Math.Round(full * Settings.ExtraMinerShare), report.StonesMined);
        }

        [Test]
        public void TheMineBuilding_OpensMoreVeins()
        {
            var w = new TestWorld();
            w.Buildings.Restore(new[] { new BuildingData(BuildingType.Mine) { Level = 2 } });
            Assert.AreEqual(Settings.VeinMiners + 2 * Settings.VeinMinersPerMineLevel, w.Tasks.VeinSlots);
        }

        [Test]
        public void TheYearsIncome_PaysTheYearsUpkeep()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            for (int i = 0; i < Settings.VeinMiners; i++)
            {
                var miner = Fixtures.Mortal();
                s.Clan.AddMember(miner);
                s.Tasks.AssignTask(miner, TaskType.Mine);
            }
            while (s.Clock.Phase != GamePhase.Inheritance) s.AdvancePhase();
            s.Resources.SetSpiritStones(0); // the coffers empty as the year ends
            Assume.That(Settings.VeinMiners * TaskAssignmentSystem.MineBaseYield, Is.GreaterThan(s.Upkeep.YearlyUpkeep));

            int year = s.Clock.Year;
            while (s.Clock.Year == year || s.Clock.Phase != GamePhase.Breakthrough) s.AdvancePhase(); // next year's tasks are done

            Assert.IsFalse(s.Upkeep.Impoverished, "the mine of the year feeds the clan of the year");
        }

        [Test]
        public void RoundTrip_KeepsAPoorYear()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Resources.SetSpiritStones(0);
            s.Upkeep.PayUpkeep();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.IsTrue(reloaded.Upkeep.Impoverished);
        }

        [Test]
        public void ALongGame_KeepsThePopulation_AndTheStones_InBounds([Values(1, 2, 3)] int seed)
        {
            var run = BalanceRun.Play(Fixtures.Content, seed, years: 150, out _, autopilot: true);
            Assert.That(run.Members, Is.LessThanOrEqualTo(1000), "the clan grows only as far as it can feed itself (~7 600 before; its vassals' tribute feeds more since 2026-10-01)");
            Assert.That(run.Stones, Is.LessThanOrEqualTo(60000), "no hoard without end");
            Assert.That(run.PoorYears, Is.LessThanOrEqualTo(run.Years / 5), "a clan at work is seldom poor");
        }
    }
}
