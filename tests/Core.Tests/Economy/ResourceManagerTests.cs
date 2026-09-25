using NUnit.Framework;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Economy
{
    /// <summary>The clan's treasury: spirit stones, herbs, ores, prestige and technique fragments.</summary>
    [TestFixture]
    public class ResourceManagerTests
    {
        private GameContext ctx;
        private ResourceManager resources;

        [SetUp]
        public void SetUp()
        {
            ctx = Fixtures.Context();
            resources = new ResourceManager(ctx);
        }

        [Test]
        public void NewTreasury_StartsWithAThousandStones()
        {
            Assert.AreEqual(1000, resources.SpiritStones);
        }

        [Test]
        public void AddSpiritStones_RaisesTheNewTotal()
        {
            int reported = -1;
            ctx.Events.OnSpiritStonesChanged += total => reported = total;
            resources.AddSpiritStones(50);
            Assert.AreEqual(1050, reported);
        }

        [Test]
        public void AddSpiritStones_IgnoresNonPositiveAmounts()
        {
            resources.AddSpiritStones(-40);
            Assert.AreEqual(1000, resources.SpiritStones);
        }

        [Test]
        public void ConsumeSpiritStones_RefusesAndKeepsTheTotal_WhenShort()
        {
            bool paid = resources.ConsumeSpiritStones(5000);
            Assert.IsTrue(!paid && resources.SpiritStones == 1000);
        }

        [Test]
        public void ConsumeSpiritStones_Deducts_WhenAffordable()
        {
            resources.ConsumeSpiritStones(300);
            Assert.AreEqual(700, resources.SpiritStones);
        }

        [Test]
        public void SetSpiritStones_NeverGoesBelowZero()
        {
            resources.SetSpiritStones(-10);
            Assert.AreEqual(0, resources.SpiritStones);
        }

        [Test]
        public void ConsumeHerbs_Refuses_WhenShort()
        {
            Assert.IsFalse(resources.ConsumeHerbs(51));
        }

        [Test]
        public void ConsumeOres_Refuses_WhenShort()
        {
            Assert.IsFalse(resources.ConsumeOres(31));
        }

        [Test]
        public void ConsumeTechniqueFragments_Refuses_WhenNoneHeld()
        {
            Assert.IsFalse(resources.ConsumeTechniqueFragments(1));
        }

        [Test]
        public void AddPrestige_AcceptsLosses()
        {
            resources.AddPrestige(-15);
            Assert.AreEqual(-5, resources.Prestige);
        }
    }
}
