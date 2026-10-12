using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Marriages between powers (L6; the user's choice of 2026-10-12): an elder of each weds; while it lasts the two neither
    /// scheme, blackmail nor spy on each other, and their affinity grows toward an alliance; it ends with either spouse.
    /// </summary>
    [TestFixture]
    public class PowerMarriageTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Lou = "Famille Lou";

        private static GameSession World(double marriageChance = 0)
        {
            var c = Fixtures.VeteranQuietContent;
            return GameSession.NewGame(new GameSetup
            {
                Seed = 1,
                Content = c with { Balance = c.Balance with { Politics = c.Balance.Politics with { MarriageChance = marriageChance } } }
            });
        }

        private static FactionData Power(GameSession s, string name) => s.Factions.GetFactionByName(name);

        private static GameSession Wed()
        {
            var s = World();
            Assert.IsTrue(s.Politics.Wed(Power(s, Ruan), Power(s, Lou)), "both have elders to wed");
            return s;
        }

        [Test]
        public void Wed_BindsTheTwoPowers()
        {
            var s = Wed();
            Assert.IsTrue(s.Politics.Married(Lou, Ruan));
        }

        [Test]
        public void Wed_Refuses_APowerWithoutAnElder()
        {
            var s = World();
            Power(s, Lou).Elders.Clear();
            Assert.IsFalse(s.Politics.Wed(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void Wed_Refuses_PowersAlreadyMarried()
        {
            var s = Wed();
            Assert.IsFalse(s.Politics.Wed(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void MarriedPowers_DoNotSchemeAgainstEachOther()
        {
            var s = Wed();
            Power(s, Lou).HighestRealm = Power(s, Ruan).HighestRealm;
            Assert.IsFalse(s.PowerSchemes.MayScheme(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void MarriedPowers_DoNotBlackmailEachOther()
        {
            var s = Wed();
            s.SecretBook.Create(s.Context.Content.SecretKinds[0].Id, Lou, null);
            s.SecretBook.AddClues(Ruan, Lou, 100000);
            Power(s, Lou).Wealth = 1000;
            Assert.IsFalse(s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void MarriedPowers_DoNotWarOnEachOther()
        {
            var c = Fixtures.VeteranQuietContent;
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = c with { Balance = c.Balance with { Wars = c.Balance.Wars with { WarChance = 1, HegemonChance = 0 } } } });
            s.Politics.Wed(Power(s, Ruan), Power(s, Lou));
            Power(s, Ruan).Personality = FactionPersonality.Aggressive;
            s.Suspicion.AddDistrust(Ruan, Lou, 1000);
            s.Wars.ProcessYear();
            Assert.IsFalse(s.Wars.Wars.Any(w => w.SideA.Contains(Ruan) && w.SideB.Contains(Lou)));
        }

        [Test]
        public void AMarriage_WarmsTheirAffinity()
        {
            var s = World();
            int before = s.Politics.AffinityBetween(Power(s, Ruan), Power(s, Lou));
            s.Politics.Wed(Power(s, Ruan), Power(s, Lou));
            Assert.AreEqual(before + s.Context.Content.Balance.Politics.MarriageAffinity, s.Politics.AffinityBetween(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void AMarriage_Ends_WithASpouse()
        {
            var s = Wed();
            var spouse = s.Politics.Marriages.Single().ElderB;
            Power(s, Lou).Elders.RemoveAll(e => e.Id == spouse);
            s.Politics.ProcessYear();
            Assert.IsFalse(s.Politics.Married(Ruan, Lou));
        }

        [Test]
        public void ThePowers_Wed_OverTheYears()
        {
            var s = World(marriageChance: 1);
            s.Politics.ProcessYear();
            Assert.Greater(s.Politics.Marriages.Count, 0);
        }

        [Test]
        public void TheChronicle_TellsAMarriage_OfThePowersNearby()
        {
            var s = World();
            var home = s.Context.Content.Clan.HomeRegion;
            var near = s.Factions.Factions.Where(f => f.RegionId == home && f.Elders.Count > 0).Take(2).ToList();
            Assume.That(near.Count, Is.EqualTo(2), "two powers by the lake");
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.Politics.Wed(near[0], near[1]);
            Assert.That(chronicle.Entries.Last(), Does.Contain("mariage").And.Not.Contain("ourdit"), "a union, not a plot");
        }

        [Test]
        public void RoundTrip_KeepsTheMarriages()
        {
            var s = Wed();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            Assert.IsTrue(reloaded.Politics.Married(Ruan, Lou));
        }
    }
}
