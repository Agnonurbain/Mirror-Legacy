using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The kingdoms' Imperial Way (the user's rule, 2026-10-03: what befalls the clan befalls the world): a kingdom's
    /// sovereign — its highest elder — at the Purple Mansion cultivates by governing: its odds of the Golden Core grow each
    /// year with its vassals, to the clan's own cap; a new sovereign starts anew; forged so, its core is imperial — no
    /// Realization asked of Heaven.
    /// </summary>
    [TestFixture]
    public class WorldImperialTests
    {
        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static (FactionData Kingdom, FactionElder Sovereign) Kingdom(GameSession s, int vassals = 3)
        {
            var kingdom = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            kingdom.Kind = FactionKind.State;
            kingdom.Elders.Clear();
            var sovereign = new FactionElder { Id = "sov", Name = "sov", Realm = CultivationRealm.PurpleMansion, Stage = 4, Perfected = true, MaxLifespan = 2000, GoldenCoreOdds = 0.1 };
            kingdom.Elders.Add(sovereign);
            var bonds = s.Factions.Factions.Where(f => f != kingdom && f.Kind == FactionKind.Family).Take(vassals)
                .Select((f, i) => new PowerBond($"b{i}", BondKind.Vassalage, kingdom.Name, f.Name, 0, false)).ToList();
            s.Politics.RestoreBonds(bonds);
            return (kingdom, sovereign);
        }

        [Test]
        public void AKingdomsPurpleMansionSovereign_GovernsItsWayToTheGoldenCore()
        {
            var s = Session();
            var (kingdom, sovereign) = Kingdom(s);
            s.Imperial.ProcessYear();
            Assert.Greater(s.Imperial.WorldMerit(kingdom), 0);
            Assert.Greater(s.Imperial.GoverningBonus(kingdom, sovereign), 0);
            for (int i = 0; i < 200; i++) s.Imperial.ProcessYear();
            Assert.LessOrEqual(s.Imperial.WorldMerit(kingdom), s.Context.Content.Balance.ImperialWay.MaxMerit);
        }

        [Test]
        public void ASectDoesNotGovern()
        {
            var s = Session();
            var sect = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            s.Imperial.ProcessYear();
            Assert.AreEqual(0, s.Imperial.WorldMerit(sect));
        }

        [Test]
        public void ANewSovereign_StartsAnew()
        {
            var s = Session();
            var (kingdom, sovereign) = Kingdom(s);
            for (int i = 0; i < 5; i++) s.Imperial.ProcessYear();
            kingdom.Elders.Remove(sovereign);
            kingdom.Elders.Add(new FactionElder { Id = "heir", Name = "heir", Realm = CultivationRealm.PurpleMansion, Stage = 4, Perfected = true, MaxLifespan = 2000 });
            s.Imperial.ProcessYear();
            Assert.LessOrEqual(s.Imperial.WorldMerit(kingdom), s.Context.Content.Balance.ImperialWay.MeritPerYear + 3 * s.Context.Content.Balance.ImperialWay.MeritPerVassal);
        }

        [Test]
        public void ForgedByGoverning_ItsCoreIsImperial_AndAsksNoRealization()
        {
            var s = Session();
            var (kingdom, sovereign) = Kingdom(s);
            for (int i = 0; i < 40; i++) s.Imperial.ProcessYear();
            sovereign.GoldenCoreOdds = 0.99; // ready
            sovereign.RealmSinceYear = -1000;
            for (int i = 0; i < 5 && sovereign.Realm != CultivationRealm.GoldenCore; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Elders.ProcessYear();
            }
            Assume.That(sovereign.Realm, Is.EqualTo(CultivationRealm.GoldenCore));
            Assert.IsTrue(sovereign.ImperialCore);
            Assert.IsNull(sovereign.FruitionId, "an imperial core: no Realization asked of Heaven");
        }
    }
}
