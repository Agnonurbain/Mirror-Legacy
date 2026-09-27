using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Open wars (user decision 2026-09-27; D7). Between powers: each side drags its allies (a coalition); a battle a year,
    /// weighed by the sides' war strength; the loser bleeds strength and wealth, the winner loots; a side fallen below its
    /// surrender ratio yields — its leader becomes the victor's vassal when it can. A power towering over the rest sees
    /// its wary neighbours band against it. An ally of the clan caught in a war calls it to arms. The clan's own war: a
    /// battle a year (its defensive allies may join), loot or losses — stones, a life; it may sue for peace with a
    /// tribute, and an exhausted enemy yields and pays.
    /// </summary>
    [TestFixture]
    public class WarTests
    {
        private const string Ruan = "Famille Ruan";   // Purple Mansion, 400
        private const string Fang = "Famille Fang";   // Foundation, 200
        private const string Tao = "Famille Tao";
        private const string Peak = "Secte du Pic des Nuées"; // Golden Core, 10000
        private static WarSettings Settings => Fixtures.Content.Balance.Wars;

        private static TestWorld World(System.Random rng, CultivationRealm strongest = CultivationRealm.Foundation)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: strongest, stage: 5)));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 3));
            return w;
        }

        private static FactionData Power(TestWorld w, string name) => w.Factions.GetFactionByName(name);

        [Test]
        public void WarStrength_GrowsWithRealm_AndSize()
        {
            var small = new FactionData { HighestRealm = CultivationRealm.Foundation, PowerLevel = 200 };
            var big = new FactionData { HighestRealm = CultivationRealm.Foundation, PowerLevel = 5000 };
            var high = new FactionData { HighestRealm = CultivationRealm.GoldenCore, PowerLevel = 200 };
            Assert.Greater(WarRules.Strength(big, Settings), WarRules.Strength(small, Settings));
            Assert.Greater(WarRules.Strength(high, Settings), WarRules.Strength(small, Settings));
        }

        [Test]
        public void AWar_DragsEachSidesAllies()
        {
            var w = World(new FixedRandom(0.999));
            w.Politics.RestoreBonds(new[] { new PowerBond("a", BondKind.Alliance, Fang, Tao, 1, false) });
            var war = w.Wars.Start(Power(w, Ruan), Power(w, Fang));
            CollectionAssert.AreEquivalent(new[] { Fang, Tao }, war.SideB);
            CollectionAssert.AreEquivalent(new[] { Ruan }, war.SideA);
        }

        [Test]
        public void ABattle_BleedsTheLoser_AndTheWinnerLoots()
        {
            var w = World(new FixedRandom(0.0)); // side A wins
            var war = w.Wars.Start(Power(w, Peak), Power(w, Fang));
            int fangPower = Power(w, Fang).PowerLevel, fangWealth = Power(w, Fang).Wealth, peakWealth = Power(w, Peak).Wealth;

            w.Wars.Battle(war);

            Assert.Less(Power(w, Fang).PowerLevel, fangPower);
            Assert.Less(Power(w, Fang).Wealth, fangWealth);
            Assert.Greater(Power(w, Peak).Wealth, peakWealth);
        }

        [Test]
        public void AnExhaustedSide_Yields_AndItsLeaderServesTheVictor()
        {
            var w = World(new FixedRandom(0.0));
            var war = w.Wars.Start(Power(w, Peak), Power(w, Fang));
            Power(w, Fang).PowerLevel = (int)(war.InitialB * Settings.SurrenderRatio) - 1;

            w.Wars.ProcessYear();

            Assert.AreEqual(0, w.Wars.Wars.Count);
            Assert.AreEqual(Peak, w.Politics.SuzerainOf(Fang));
        }

        [Test]
        public void AnAllyOfTheClan_AtWar_CallsItToArms()
        {
            var w = World(new FixedRandom(0.0));
            w.Factions.ChangeRelation(Power(w, Tao).ID, 30);
            Assert.IsNull(w.Treaties.Propose(Tao, TreatyKind.Defence));
            var war = w.Wars.Start(Power(w, Ruan), Power(w, Tao));

            w.Wars.Battle(war);

            Assert.AreEqual(Tao, w.Politics.PendingCall?.Ally);
        }

        [Test]
        public void AHegemon_SeesItsWaryNeighboursBandAgainstIt()
        {
            var w = World(new FixedRandom(0.0));
            const string iron = "Porte du Fer Ardent"; // four neighbouring powers
            var hegemon = Power(w, iron);
            hegemon.PowerLevel = 100000;
            foreach (var other in w.Factions.Factions.Where(f => f != hegemon && w.Factions.AreNeighbours(f, hegemon)))
                w.Suspicion.AddDistrust(other.Name, iron, Settings.WarDistrust);

            w.Wars.ProcessYear();

            var war = w.Wars.Wars.Single(x => x.SideB.Contains(iron));
            Assert.Greater(war.SideA.Count, 1, "its wary neighbours band together");
        }

        // ---- The clan's own war ----

        [Test]
        public void DeclaringWar_OpensTheClansWar()
        {
            var w = World(new FixedRandom(0.999));
            Assert.IsNull(w.Wars.DeclareOn(Fang));
            Assert.AreEqual(Fang, w.Wars.ClanWars.Single().Enemy);
            Assert.AreEqual(-100 + 5, Power(w, Fang).RelationWithPlayer);
        }

        [Test]
        public void AWonBattle_TakesFromTheEnemy_ALostOne_CostsTheClan()
        {
            var win = World(new FixedRandom(0.0), CultivationRealm.GoldenCore);
            win.Wars.DeclareOn(Fang);
            int stones = win.Resources.SpiritStones, fang = Power(win, Fang).PowerLevel;
            win.Wars.ProcessYear();
            Assert.Greater(win.Resources.SpiritStones, stones);
            Assert.Less(Power(win, Fang).PowerLevel, fang);

            var lose = World(new FixedRandom(0.999), CultivationRealm.QiRefinement);
            lose.Wars.DeclareOn(Peak);
            int before = lose.Resources.SpiritStones;
            lose.Wars.ProcessYear();
            Assert.Less(lose.Resources.SpiritStones, before);
        }

        [Test]
        public void SuingForPeace_CostsATribute_AndEndsTheWar()
        {
            var w = World(new FixedRandom(0.999));
            w.Wars.DeclareOn(Ruan);
            int stones = w.Resources.SpiritStones;
            Assert.IsNull(w.Wars.SuePeace(Ruan));
            Assert.AreEqual(stones - (int)(stones * Settings.PeaceTributeShare), w.Resources.SpiritStones);
            Assert.AreEqual(0, w.Wars.ClanWars.Count);
            Assert.AreEqual(Settings.PeaceRelation, Power(w, Ruan).RelationWithPlayer);
        }

        [Test]
        public void AnExhaustedEnemy_YieldsToTheClan_AndPays()
        {
            var w = World(new FixedRandom(0.999));
            w.Wars.DeclareOn(Fang);
            var war = w.Wars.ClanWars.Single();
            Power(w, Fang).PowerLevel = (int)(war.EnemyInitial * Settings.SurrenderRatio) - 1;
            int stones = w.Resources.SpiritStones;

            w.Wars.ProcessYear();

            Assert.AreEqual(0, w.Wars.ClanWars.Count);
            Assert.Greater(w.Resources.SpiritStones, stones);
        }

        [Test]
        public void RoundTrip_KeepsTheWars()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Wars.DeclareOn(Fang);
            s.Wars.Start(s.Factions.GetFactionByName(Ruan), s.Factions.GetFactionByName(Tao));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(s.Wars.ClanWars.Single(), reloaded.Wars.ClanWars.Single());
            CollectionAssert.AreEqual(s.Wars.Wars.Single().SideA, reloaded.Wars.Wars.Single().SideA);
        }

        [Test]
        public void TheDiplomacyScreen_ShowsTheWars_AndThePriceOfPeace()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Wars.DeclareOn(Fang);
            s.Wars.Start(s.Factions.GetFactionByName(Ruan), s.Factions.GetFactionByName(Tao));

            var wars = MirrorChronicles.Presentation.DiplomacyView.Wars(s);

            var ours = wars.Single(x => x.ClansWar);
            Assert.AreEqual(Fang, ours.Enemy);
            Assert.AreEqual((int)(s.Resources.SpiritStones * Settings.PeaceTributeShare), ours.PeaceCost);
            StringAssert.Contains(Ruan, wars.Single(x => !x.ClansWar).Description);
            Assert.IsFalse(wars.Any(x => x.Description.Contains("@")), "the clan is named, never coded");
        }
    }
}
