using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// Greed (user decision 2026-09-29, D7: profit): a rich clan too weak to defend its wealth tempts a greedy power
    /// stronger than it, which demands a share of its stones « for its protection ». Paid, it keeps quiet a while;
    /// refused — or left unanswered — it makes war on the clan. Prudence protects the clan, not passivity.
    /// </summary>
    [TestFixture]
    public class GreedTests
    {
        private static IntrigueSettings Settings => Fixtures.Content.Balance.Intrigues;

        /// <summary>No theft: the hoard is only coveted here.</summary>
        private static GameContent NoThieves => Fixtures.Content with
        {
            Balance = Fixtures.Content.Balance with { Intrigues = Settings with { TheftChance = 0 } }
        };

        private static TestWorld World(System.Random rng, CultivationRealm strongest, int stones)
        {
            var w = new TestWorld(rng, NoThieves);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: strongest, stage: 1)));
            w.Resources.SetSpiritStones(stones);
            return w;
        }

        private static Demand Protection(TestWorld w) => w.Intrigues.Demands.SingleOrDefault(d => d.Kind == DemandKind.Protection);

        [Test]
        public void ARichWeakClan_TemptsAGreedyStrongerPower()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.QiRefinement, Settings.GreedStones * 4);
            w.Intrigues.ProcessYear();

            var demand = Protection(w);
            Assert.IsNotNull(demand);
            var power = w.Factions.GetFactionByName(demand.Faction);
            Assert.Greater(Settings.GreedTemper[power.Personality], 0, "a greedy temper");
            Assert.AreEqual((int)(Settings.GreedStones * 4 * Settings.ExtortionShare), demand.Stones);
        }

        [Test]
        public void APoorClan_TemptsNobody()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.QiRefinement, Settings.GreedStones - 1);
            w.Intrigues.ProcessYear();
            Assert.IsNull(Protection(w));
        }

        [Test]
        public void AClanStrongerThanThem_IsLeftAlone()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.GoldenCore, Settings.GreedStones * 4);
            var greedy = w.Factions.Factions.Where(f => Settings.GreedTemper.TryGetValue(f.Personality, out var t) && t > 0).ToList();
            foreach (var power in greedy)
            {
                power.HighestRealm = CultivationRealm.QiRefinement;
                power.PowerLevel = 0;
            }
            w.Intrigues.ProcessYear();
            Assert.IsNull(Protection(w));
        }

        [Test]
        public void Paying_BuysQuiet()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.QiRefinement, Settings.GreedStones * 4);
            w.Intrigues.ProcessYear();
            var demand = Protection(w);
            Assert.IsNull(w.Intrigues.Pay(demand.Faction));
            Assert.AreEqual(Settings.GreedStones * 4 - demand.Stones, w.Resources.SpiritStones);
            w.Intrigues.ProcessYear();
            Assert.IsFalse(w.Intrigues.Demands.Any(d => d.Faction == demand.Faction));
        }

        [Test]
        public void Refusing_OrSilence_BringsWar()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.QiRefinement, Settings.GreedStones * 4);
            w.Intrigues.ProcessYear();
            var demand = Protection(w);
            Assert.IsNull(w.Intrigues.Refuse(demand.Faction));
            Assert.AreEqual(demand.Faction, w.Wars.ClanWars.Single().Enemy);
        }

        [Test]
        public void TheDiplomacyScreen_TellsProtection_FromSilence()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Ruan", 500, 1, DemandKind.Protection) }, null);
            StringAssert.Contains("protection", DiplomacyView.Demands(s).Single().Reason);
        }

        [Test]
        public void RoundTrip_KeepsWhatIsDemanded()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Intrigues.RestoreDemands(new[] { new Demand("Famille Ruan", 500, 1, DemandKind.Protection) }, null);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(DemandKind.Protection, reloaded.Intrigues.Demands.Single().Kind);
        }
    }
}
