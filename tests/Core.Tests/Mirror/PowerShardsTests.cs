using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The shards held by powers (LORE.md §11.5, B3c3; the user's decisions of 2026-09-30): three shards lie with three
    /// powers under their « hidden treasure » secret, which the clan must pierce. Every means is good — steal it, take it by
    /// war, demand it of a vassal, trade for it — so long as no one suspects anything: every taking weighs on the mirror's
    /// secret, and a power that knows the mirror understands at once.
    /// </summary>
    [TestFixture]
    public class PowerShardsTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.99;
        private const string Holder = "Porte du Givre Blanc";

        private static string AShard => Fixtures.Content.Shards.First(x => x.Source == ShardSource.Power).Id;

        private static GameSession Quiet(int seed = 1) => GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });

        /// <summary>A world where the shard lies with <see cref="Holder"/> and the clan has pierced that secret.</summary>
        private static TestWorld Known(System.Random rng, string holder = Holder, int relation = 0)
        {
            var w = new TestWorld(rng);
            w.Factions.AddFaction(new FactionData { Name = holder, Kind = FactionKind.Gate, RegionId = "white-frost-lands",
                HighestRealm = CultivationRealm.Foundation, PowerLevel = 1200, Wealth = 5000, RelationWithPlayer = relation });
            var secret = w.PowerShards.Hide(AShard, holder);
            w.SecretBook.Grant(SecretBook.ClanHolder, secret.Id);
            return w;
        }

        // ---- Where they lie ----

        [Test]
        public void ThreeShards_LieWithThreeDifferentPowers_UnderTheirHiddenTreasure()
        {
            var s = Quiet();
            var held = Fixtures.Content.Shards.Where(x => x.Source == ShardSource.Power)
                .Select(x => s.SecretBook.All.Single(secret => secret.Subject == x.Id)).ToList();
            Assert.AreEqual(3, held.Select(x => x.Holder).Distinct().Count());
            Assert.IsTrue(held.All(x => x.KindId == "hidden-treasure"));
        }

        [Test]
        public void TheSameSeed_PlacesTheShardsWithTheSamePowers()
        {
            Assert.AreEqual(Quiet(7).PowerShards.HolderOf(AShard), Quiet(7).PowerShards.HolderOf(AShard));
        }

        [Test]
        public void TheClan_DoesNotKnowWhereTheyLie_UntilItPiercesTheSecret()
        {
            var s = Quiet();
            Assert.IsFalse(s.PowerShards.KnownByClan(AShard));
            s.SecretBook.Grant(SecretBook.ClanHolder, s.SecretBook.All.Single(x => x.Subject == AShard).Id);
            Assert.IsTrue(s.PowerShards.KnownByClan(AShard));
        }

        // ---- Stealing it ----

        [Test]
        public void AShard_CannotBeStolen_FromWhereTheClanDoesNotKnowItLies()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Factions.AddFaction(new FactionData { Name = Holder, Kind = FactionKind.Gate, RegionId = "white-frost-lands" });
            w.PowerShards.Hide(AShard, Holder);
            var thief = w.Join(Fixtures.Cultivator(stage: 9));
            Assert.IsFalse(w.PowerShards.Steal(AShard, new[] { thief.ID }).Launched);
        }

        [Test]
        public void ATheftUnseen_BringsTheShardBack_AndLeavesNoClue()
        {
            var w = Known(new FixedRandom(Pass));
            var thief = w.Join(Fixtures.Cultivator(stage: 9));

            var outcome = w.PowerShards.Steal(AShard, new[] { thief.ID });

            Assert.IsTrue(outcome.Launched && outcome.Taken && !outcome.Caught && w.Shards.IsRecovered(AShard));
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Holder));
        }

        [Test]
        public void ATheftCaught_LeavesProof_AndMakesTheHolderWonder()
        {
            var w = Known(new SequenceRandom(Fail, Pass)); // the theft fails; the thief is caught
            var thief = w.Join(Fixtures.Cultivator(stage: 9));

            var outcome = w.PowerShards.Steal(AShard, new[] { thief.ID });

            Assert.IsTrue(outcome.Launched && !outcome.Taken && outcome.Caught && !w.Shards.IsRecovered(AShard));
            Assert.IsTrue(w.Suspicion.Evidence(Holder) > 0 && w.Suspicion.MirrorClues(Holder) > 0);
        }

        // ---- Demanding it of a vassal ----

        [Test]
        public void AVassal_HandsTheShardOver_ButWondersWhy()
        {
            var w = Known(new FixedRandom(Pass), relation: 20);
            w.Treaties.Conclude(new Treaty("v", TreatyKind.Vassalage, Holder, 1, null, false, false, true));

            Assert.IsNull(w.PowerShards.DemandOfVassal(AShard));

            Assert.IsTrue(w.Shards.IsRecovered(AShard) && w.Suspicion.MirrorClues(Holder) > 0);
            Assert.Less(w.Factions.GetFactionByName(Holder).RelationWithPlayer, 20);
        }

        [Test]
        public void OnlyAVassal_CanBeMadeToHandItOver()
        {
            var w = Known(new FixedRandom(Pass));
            Assert.IsNotNull(w.PowerShards.DemandOfVassal(AShard));
            Assert.IsFalse(w.Shards.IsRecovered(AShard));
        }

        // ---- Trading for it ----

        [Test]
        public void AFriendlyHolder_TradesTheShard_ForStones()
        {
            var w = Known(new FixedRandom(Pass), relation: 60);
            int price = w.PowerShards.TradePrice(AShard);
            w.Resources.AddSpiritStones(price);
            int stones = w.Resources.SpiritStones;

            Assert.IsNull(w.PowerShards.Trade(AShard));

            Assert.IsTrue(w.Shards.IsRecovered(AShard) && w.Resources.SpiritStones == stones - price && w.Suspicion.MirrorClues(Holder) > 0);
        }

        [Test]
        public void AColdHolder_RefusesToTrade()
        {
            var w = Known(new FixedRandom(Pass), relation: -20);
            w.Resources.AddSpiritStones(1_000_000);
            Assert.IsNotNull(w.PowerShards.Trade(AShard));
            Assert.IsFalse(w.Shards.IsRecovered(AShard));
        }

        // ---- Taking it by war ----

        [Test]
        public void AnEnemyThatYields_HandsOverTheShard_DrownedInTheLoot()
        {
            var w = Known(new FixedRandom(Pass));
            w.Ctx.Events.TriggerClanWarWon(Holder);
            Assert.IsTrue(w.Shards.IsRecovered(AShard));
            Assert.Less(w.Suspicion.MirrorClues(Holder), Fixtures.Content.Balance.Shards.VassalClues, "war loot hides the shard better than a demand");
        }

        [Test]
        public void AnEnemyThatYields_KeepsAShardTheClanDoesNotKnowOf()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Factions.AddFaction(new FactionData { Name = Holder, Kind = FactionKind.Gate, RegionId = "white-frost-lands" });
            w.PowerShards.Hide(AShard, Holder);
            w.Ctx.Events.TriggerClanWarWon(Holder);
            Assert.IsFalse(w.Shards.IsRecovered(AShard));
        }

        // ---- The mirror's secret ----

        [Test]
        public void AHolderThatKnowsTheMirror_UnderstandsAtOnce()
        {
            var probe = new TestWorld();
            var knower = Fixtures.Content.Factions.Select(f => f.Name).FirstOrDefault(name => { probe.Factions.AddFaction(Fixtures.Content.Factions.First(f => f.Name == name).Clone()); return probe.Lore.Knows(name); });
            Assume.That(knower, Is.Not.Null, "the fixture's world seed must hold a power that knows the mirror");

            var w = new TestWorld(new FixedRandom(Pass));
            foreach (var f in Fixtures.Content.Factions) w.Factions.AddFaction(f.Clone());
            w.SecretBook.Grant(SecretBook.ClanHolder, w.PowerShards.Hide(AShard, knower).Id);
            w.Treaties.Conclude(new Treaty("v", TreatyKind.Vassalage, knower, 1, null, false, false, true));

            w.PowerShards.DemandOfVassal(AShard);

            Assert.GreaterOrEqual(w.Suspicion.MirrorClues(knower), Fixtures.Content.Balance.Plots.DoubtClues);
        }

        [Test]
        public void AnAbsorbedHolder_PassesTheShardToItsSuzerain_UnderASecretTheClanMustPierceAgain()
        {
            var w = Known(new FixedRandom(Pass));
            w.Factions.AddFaction(new FactionData { Name = "Secte du Pic des Nuées", Kind = FactionKind.Sect, RegionId = "mount-yunfeng" });

            w.Ctx.Events.TriggerPowerAbsorbed(Holder, "Secte du Pic des Nuées");

            Assert.IsTrue(w.PowerShards.HolderOf(AShard) == "Secte du Pic des Nuées" && !w.PowerShards.KnownByClan(AShard));
        }

        [Test]
        public void WhereTheShardsLie_SurvivesASave()
        {
            var s = Quiet();
            s.SecretBook.Grant(SecretBook.ClanHolder, s.SecretBook.All.Single(x => x.Subject == AShard).Id);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.IsTrue(reloaded.PowerShards.HolderOf(AShard) == s.PowerShards.HolderOf(AShard) && reloaded.PowerShards.KnownByClan(AShard));
        }
    }
}
