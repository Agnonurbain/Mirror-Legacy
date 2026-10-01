using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' economy and growth (the living world, step B, 2026-10-01): a yearly income by size (more for a merchant),
    /// an upkeep of its disciples, and a size that tends toward what its elders can lead — the world as drawn at first,
    /// more when an elder rises, less when a great one dies; an expansionist grows faster, never without bound.
    /// </summary>
    [TestFixture]
    public class PowerEconomyTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static PowerEconomySettings Settings => Fixtures.QuietContent.Balance.PowerEconomy;

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++) s.PowerEconomy.ProcessYear();
        }

        [Test]
        public void APower_EarnsByItsSize_AndPaysItsDisciples()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Famille Lou");
            int wealth = power.Wealth, size = power.PowerLevel;
            Years(s, 1);
            Assert.AreEqual(wealth + (int)(size * (Settings.IncomePerPower - Settings.UpkeepPerPower)), power.Wealth, 1);
        }

        [Test]
        public void AMerchant_EarnsMore()
        {
            var s = Session();
            var merchant = s.Factions.Factions.First(f => f.Personality == FactionPersonality.Merchant);
            var other = s.Factions.Factions.First(f => f.Personality == FactionPersonality.Isolationist);
            merchant.PowerLevel = other.PowerLevel = 1000;
            merchant.Wealth = other.Wealth = 0;
            Years(s, 1);
            Assert.Greater(merchant.Wealth, other.Wealth);
        }

        [Test]
        public void TheWorld_AsDrawn_StaysAtRest()
        {
            var s = Session();
            var before = s.Factions.Factions.ToDictionary(f => f.Name, f => f.PowerLevel);
            Years(s, 10);
            foreach (var f in s.Factions.Factions) Assert.AreEqual(before[f.Name], f.PowerLevel, before[f.Name] * 0.02 + 1, f.Name);
        }

        [Test]
        public void APower_Shrinks_WhenItsGreatElderDies()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Secte du Pic des Nuées");
            Years(s, 1); // its baseline taken
            int size = power.PowerLevel;
            power.Elders.RemoveAll(e => e.Realm >= CultivationRealm.PurpleMansion);
            Years(s, 20);
            Assert.Less(power.PowerLevel, size, "fewer elders lead fewer disciples");
        }

        [Test]
        public void APower_Grows_WhenAnElderRises()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Famille Lou");
            Years(s, 1);
            int size = power.PowerLevel;
            power.Elders.Add(new FactionElder { Id = "risen", Name = "Risen", Realm = CultivationRealm.GoldenCore, Stage = 1, BornYear = 0, MaxLifespan = 1000 });
            Years(s, 20);
            Assert.Greater(power.PowerLevel, size);
        }

        [Test]
        public void AnExpansionist_GrowsFaster_ButNeverWithoutBound()
        {
            var s = Session();
            var power = s.Factions.Factions.First(f => f.Personality == FactionPersonality.Expansionist);
            Years(s, 1);
            int size = power.PowerLevel;
            Years(s, 300);
            Assert.That(power.PowerLevel, Is.LessThanOrEqualTo(size * 1.2), "a bound: what its elders can lead");
        }

        [Test]
        public void APowerInDebt_Declines()
        {
            var s = Session();
            var power = s.Factions.GetFactionByName("Famille Lou");
            Years(s, 1);
            int size = power.PowerLevel;
            power.Wealth = -100000;
            Years(s, 5);
            Assert.Less(power.PowerLevel, size, "it cannot keep its disciples");
        }

        [Test]
        public void TheBaseline_SurvivesASave()
        {
            var s = Session();
            Years(s, 1);
            var power = s.Factions.GetFactionByName("Secte du Pic des Nuées");
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            var again = reloaded.Factions.GetFactionByName(power.Name);
            Assert.AreEqual(power.BaselinePower, again.BaselinePower);
            Assert.AreEqual(power.BaselineWeight, again.BaselineWeight, 1e-9);
        }
    }
}
