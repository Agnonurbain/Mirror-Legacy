using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The clan absorbs its vassals (the user's decision, 2026-09-30; P1: the rules a suzerain holds over the clan, turned
    /// the other way): its grip on a vassal grows each year and takes a share of its wealth and an art at each fill; once full
    /// enough, the clan may absorb it — its wealth, its arts, a few cultivators who take the clan's name, what it knew of
    /// others' secrets, its shard — while every other power watches with a silent distrust.
    /// </summary>
    [TestFixture]
    public class ClanAbsorptionTests
    {
        private const string Vassal = "Famille Lou";

        private static GameSession Quiet() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static TreatySettings Settings => Fixtures.Content.Balance.Treaties;

        private static void MakeVassal(GameSession s, string power = Vassal, int absorptions = 0) =>
            s.Treaties.Conclude(new Treaty($"v-{power}", TreatyKind.Vassalage, power, s.Clock.Year, null, false, false, true) { Absorptions = absorptions });

        private static int Steps => Fixtures.Content.Balance.Politics.ClanAbsorptionSteps;

        [Test]
        public void TheClansGrip_OnAVassal_GrowsEachYear_AndTakesAtEachFill()
        {
            var s = Quiet();
            MakeVassal(s);
            var power = s.Factions.GetFactionByName(Vassal);
            power.Wealth = 10_000;
            int fill = (Settings.GripThreshold + Settings.GripPerYear - 1) / Settings.GripPerYear;

            for (int y = 0; y < fill; y++) s.Treaties.ProcessYear();

            var treaty = s.Treaties.All.Single(t => t.Faction == Vassal);
            Assert.AreEqual(1, treaty.Absorptions);
            Assert.Less(power.Wealth, 10_000);
        }

        [Test]
        public void AVassal_CanBeAbsorbed_OnlyOnceTheGripHasFilledEnough()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps - 1);
            StringAssert.Contains("emprise", s.Absorption.Refusal(Vassal));
            Assert.IsNotNull(s.Absorption.Refusal("Famille Tao"), "not a vassal");
        }

        [Test]
        public void AbsorbingAVassal_BringsItsWealthArtsAndCultivators_IntoTheClan()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps);
            var power = s.Factions.GetFactionByName(Vassal);
            power.Wealth = 4000;
            var arts = power.Techniques.ToList();
            int stones = s.Resources.SpiritStones;
            int members = s.Clan.LivingMembers.Count;

            Assert.IsNull(s.Absorption.Absorb(Vassal));

            Assert.IsNull(s.Factions.GetFactionByName(Vassal));
            Assert.AreEqual(stones + 4000, s.Resources.SpiritStones);
            Assert.IsTrue(arts.All(s.Techniques.Knows));
            var joiners = s.Clan.LivingMembers.Skip(members).ToList();
            Assert.AreEqual(Settings.AbsorbedJoiners, joiners.Count);
            Assert.IsTrue(joiners.All(m => m.LastName == s.Clan.ClanName && m.Realm >= CultivationRealm.QiRefinement));
            Assert.IsFalse(s.Treaties.All.Any(t => t.Faction == Vassal));
            CollectionAssert.Contains(s.Absorption.Absorbed.Select(a => a.Name), Vassal);
        }

        [Test]
        public void AnAbsorbedVassal_GivesUpItsShard()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps);
            string shard = Fixtures.Content.Shards.First(x => x.Source == ShardSource.Power).Id;
            s.PowerShards.Hide(shard, Vassal);

            s.Absorption.Absorb(Vassal);

            Assert.IsTrue(s.Shards.IsRecovered(shard));
        }

        [Test]
        public void EveryOtherPower_WatchesTheClanGrow()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps);
            var witness = s.Factions.Factions.First(f => f.Name != Vassal).Name;
            int suspicion = s.Suspicion.OfClan(witness);

            s.Absorption.Absorb(Vassal);

            Assert.Greater(s.Suspicion.OfClan(witness), suspicion);
            Assert.Greater(s.Suspicion.Distrust(witness, SecretBook.ClanHolder), 0);
        }

        [Test]
        public void TheAbsorption_IsToldAndKept()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps);
            s.Absorption.Absorb(Vassal);
            Assert.IsTrue(s.Annals.Entries.Any(e => e.Kind == AnnalKind.PowerAbsorbed && e.Ref == Vassal));
            StringAssert.Contains($"le clan absorbe {Vassal}", string.Join("\n", AnnalsView.Lines(s)));
        }

        [Test]
        public void AnAbsorbedFamily_CountsAsBowed_ForTheLakesUnity()
        {
            var s = Quiet();
            var lake = new[] { "Famille Lü", "Famille Tao", "Famille Lou", "Famille Fang", "Famille Kang" };
            foreach (var family in lake) MakeVassal(s, family, family == Vassal ? Steps : 0);
            s.Absorption.Absorb(Vassal);
            s.Events.TriggerYearStarted(s.Clock.Year);
            Assert.IsTrue(s.Endings.IsReached("lake-unified"));
        }

        [Test]
        public void TheAbsorbed_SurviveASave()
        {
            var s = Quiet();
            MakeVassal(s, absorptions: Steps);
            s.Absorption.Absorb(Vassal);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            CollectionAssert.AreEqual(s.Absorption.Absorbed, reloaded.Absorption.Absorbed);
        }
    }
}
