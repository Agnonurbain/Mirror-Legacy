using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The powers' blackmail and spies against each other (P1; the user's choices of 2026-10-12): a weaker victim pays and buys
    /// quiet, a stronger one refuses — the others distrust it and it strikes the blackmailer; a planted spy brings clues on its
    /// host's secrets each year until it is unmasked, its host distrusting and striking back. The clan is never their target.
    /// </summary>
    [TestFixture]
    public class PowerIntrigueTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Lou = "Famille Lou";
        private const string Fang = "Famille Fang";

        private static PowerIntrigueSettings Settings(double unmask = 0) => new PowerIntrigueSettings
        {
            BlackmailChance = 0, BlackmailWealthShare = 0.15, QuietYears = 5, RefuseStrengthRatio = 1.0, RefusalDistrust = 20,
            SpyChance = 0, SpyCluesPerYear = 1000, SpyUnmaskChance = unmask, UnmaskDistrust = 20
        };

        private static GameSession World(double unmask = 0)
        {
            var c = Fixtures.VeteranQuietContent;
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = c with { Balance = c.Balance with { PowerIntrigues = Settings(unmask) } } });
            Power(s, Ruan).PowerLevel = 2000;
            Power(s, Ruan).Wealth = 1000;
            Power(s, Lou).PowerLevel = 1000;
            Power(s, Lou).Wealth = 1000;
            return s;
        }

        private static FactionData Power(GameSession s, string name) => s.Factions.GetFactionByName(name);

        /// <summary>The watcher comes to know a secret of the holder.</summary>
        private static void Learns(GameSession s, string watcher, string holder)
        {
            s.SecretBook.Create(s.Context.Content.SecretKinds[0].Id, holder, null);
            s.SecretBook.AddClues(watcher, holder, 100000);
        }

        // ---- Blackmail ----

        [Test]
        public void Blackmail_TakesAShareOfTheWealth_FromAWeakerVictim()
        {
            var s = World();
            Learns(s, Ruan, Lou);
            Assert.IsTrue(s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou)));
            Assert.AreEqual((850, 1150), (Power(s, Lou).Wealth, Power(s, Ruan).Wealth));
        }

        [Test]
        public void Blackmail_BuysQuiet_ForSomeYears()
        {
            var s = World();
            Learns(s, Ruan, Lou);
            s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou));
            Assert.IsFalse(s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou)), "paid: it keeps quiet");
        }

        [Test]
        public void Blackmail_NeedsAKnownSecret()
        {
            var s = World();
            Assert.IsFalse(s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void Blackmail_IsRefused_ByAVictimAtLeastAsStrong()
        {
            var s = World();
            Learns(s, Lou, Ruan);
            s.PowerIntrigues.Blackmail(Power(s, Lou), Power(s, Ruan));
            Assert.GreaterOrEqual(Power(s, Ruan).Wealth, 1000, "the stronger refuses to pay");
        }

        [Test]
        public void ARefusal_SpreadsTheSecret_TheOthersDistrustTheVictim()
        {
            var s = World();
            Learns(s, Lou, Ruan);
            int before = s.Suspicion.Distrust(Fang, Ruan);
            s.PowerIntrigues.Blackmail(Power(s, Lou), Power(s, Ruan));
            Assert.AreEqual(before + 20, s.Suspicion.Distrust(Fang, Ruan));
        }

        [Test]
        public void ARefusal_SetsTheVictimStriking_TheBlackmailer()
        {
            var s = World();
            Learns(s, Lou, Ruan);
            s.PowerIntrigues.Blackmail(Power(s, Lou), Power(s, Ruan));
            Assert.Less(Power(s, Lou).PowerLevel, 1000);
        }

        [Test]
        public void ARefusal_IsNotAskedAgain_ForSomeYears()
        {
            var s = World();
            Learns(s, Lou, Ruan);
            s.PowerIntrigues.Blackmail(Power(s, Lou), Power(s, Ruan));
            Assert.IsFalse(s.PowerIntrigues.Blackmail(Power(s, Lou), Power(s, Ruan)), "refused: the matter is out");
        }

        [Test]
        public void Blackmail_SparesAPauper()
        {
            var s = World();
            Learns(s, Ruan, Lou);
            Power(s, Lou).Wealth = 0;
            Assert.IsFalse(s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou)));
        }

        // ---- Spies ----

        [Test]
        public void ASpy_IsWithdrawn_OnceItsHostIsOutOfReach()
        {
            var s = World();
            s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Lou));
            Power(s, Lou).HighestRealm = Power(s, Ruan).HighestRealm + 1; // now out of its master's reach
            s.PowerIntrigues.ProcessYear();
            Assert.AreEqual(0, s.PowerIntrigues.Spies.Count);
        }

        [Test]
        public void Plant_HidesAnAgent_OnceAHost()
        {
            var s = World();
            Assert.IsTrue(s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Lou)));
            Assert.IsFalse(s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Lou)));
        }

        [Test]
        public void ASpy_BringsCluesOnItsHostsSecrets_EachYear()
        {
            var s = World();
            var secret = s.SecretBook.Create(s.Context.Content.SecretKinds[0].Id, Lou, null);
            s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Lou));
            s.PowerIntrigues.ProcessYear();
            Assert.IsTrue(s.SecretBook.Knows(Ruan, secret.Id));
        }

        [Test]
        public void AnUnmaskedSpy_IsGone_AndItsHostDistrustsItsMaster()
        {
            var s = World(unmask: 1);
            int before = s.Suspicion.Distrust(Lou, Ruan);
            s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Lou));
            s.PowerIntrigues.ProcessYear();
            Assert.AreEqual((0, before + 20), (s.PowerIntrigues.Spies.Count, s.Suspicion.Distrust(Lou, Ruan)));
        }

        [Test]
        public void ASpy_Leaves_WhenItsHostIsGone()
        {
            var s = World();
            s.PowerIntrigues.Restore(new[] { new PowerSpy(Ruan, "Puissance disparue", 0) }, null);
            s.PowerIntrigues.ProcessYear();
            Assert.AreEqual(0, s.PowerIntrigues.Spies.Count);
        }

        // ---- In the session ----

        [Test]
        public void RoundTrip_KeepsTheSpies_AndTheQuietBought()
        {
            var s = World();
            Learns(s, Ruan, Lou);
            s.PowerIntrigues.Blackmail(Power(s, Ruan), Power(s, Lou));
            s.PowerIntrigues.Plant(Power(s, Ruan), Power(s, Fang));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            Assert.AreEqual((1, 1), (reloaded.PowerIntrigues.Spies.Count, reloaded.PowerIntrigues.QuietUntil.Count));
        }
    }
}
