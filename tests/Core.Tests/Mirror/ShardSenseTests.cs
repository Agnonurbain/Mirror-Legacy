using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The mirror senses a shard near enough (user decision 2026-10-01): its holder seated in the domain's region or a
    /// neighbouring one, or a bearer of a Talisman Seed come to the holder (a probe, an embassy, a disciple, a captive). It
    /// speaks through the seeds: with none, it is mute. It gives only a direction — the region where the shard lies.
    /// </summary>
    [TestFixture]
    public class ShardSenseTests
    {
        private const string Shard = "pale-seal-jade";
        private const string Near = "Famille Ruan";   // seated at Heshan, a neighbour of the lake
        private const string Far = "Empire de Kun";   // far beyond

        private static GameContent Certain => Fixtures.QuietContent with
        {
            Balance = Fixtures.QuietContent.Balance with
            {
                Shards = Fixtures.QuietContent.Balance.Shards with { SenseChance = 1.0, SenseChancePerSeed = 0, SenseMaxChance = 1.0 }
            }
        };

        private static GameSession Session(string holder, bool seedBearer = true)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Certain });
            s.PowerShards.Hide(Shard, holder);
            if (seedBearer)
            {
                var bearer = Fixtures.Mortal(age: 20);
                bearer.HasTalismanSeed = true;
                s.Clan.AddMember(bearer);
            }
            return s;
        }

        private static string RegionOf(GameSession s, string power) => s.Factions.GetFactionByName(power).RegionId;

        [Test]
        public void AShard_HeldNearby_IsSensed_ThroughTheSeeds()
        {
            var s = Session(Near);
            s.ShardSense.ProcessYear();
            Assert.AreEqual(RegionOf(s, Near), s.ShardSense.Directions[Shard], "the mirror points to the region where it lies");
            Assert.IsFalse(s.PowerShards.KnownByClan(Shard), "a direction, not the holder");
        }

        [Test]
        public void WithoutASeedBearer_TheMirrorIsMute()
        {
            var s = Session(Near, seedBearer: false);
            s.ShardSense.ProcessYear();
            Assert.IsEmpty(s.ShardSense.Directions);
        }

        [Test]
        public void AShard_HeldFarAway_IsNotSensed()
        {
            var s = Session(Far);
            s.ShardSense.ProcessYear();
            Assert.IsFalse(s.ShardSense.Directions.ContainsKey(Shard));
        }

        [Test]
        public void ASeedBearer_SentToTheHolder_SensesItAnywhere()
        {
            var s = Session(Far);
            var envoy = Fixtures.Cultivator(age: 30);
            envoy.HasTalismanSeed = true;
            envoy.DiplomacyTarget = Far;
            envoy.CurrentTask = TaskType.Diplomacy;
            s.Clan.AddMember(envoy);
            s.ShardSense.ProcessYear();
            Assert.AreEqual(RegionOf(s, Far), s.ShardSense.Directions[Shard]);
        }

        [Test]
        public void ASeedBearer_ProbingTheHolder_SensesItAnywhere()
        {
            var s = Session(Far, seedBearer: false);
            var prober = Fixtures.Cultivator(age: 30);
            prober.HasTalismanSeed = true;
            s.Clan.AddMember(prober);
            s.Events.TriggerClanProbe(Far, new[] { prober.ID });
            Assert.AreEqual(RegionOf(s, Far), s.ShardSense.Directions[Shard]);
        }

        [Test]
        public void TheMirrorAsleep_SensesNothing()
        {
            var s = Session(Near);
            s.Mirror.Restore(s.Mirror.MirrorPower, s.Mirror.RestoredFragments, asleepUntil: s.Clock.Year + 2);
            s.ShardSense.ProcessYear();
            Assert.IsEmpty(s.ShardSense.Directions);
        }

        [Test]
        public void ARecoveredShard_NeedsNoSense()
        {
            var s = Session(Near);
            s.Shards.Recover(Shard);
            s.ShardSense.ProcessYear();
            Assert.IsFalse(s.ShardSense.Directions.ContainsKey(Shard));
        }

        [Test]
        public void TheDirections_SurviveASave()
        {
            var s = Session(Near);
            s.ShardSense.ProcessYear();
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Certain });
            Assert.AreEqual(RegionOf(s, Near), reloaded.ShardSense.Directions[Shard]);
        }

        [Test]
        public void TheChronicle_TellsWhatTheMirrorSensed()
        {
            var s = Session(Near);
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            s.ShardSense.ProcessYear();
            StringAssert.Contains("miroir", chronicle.Entries.Last());
        }

        [Test]
        public void ThePilot_ProbesThePowersOfASensedRegion_First()
        {
            var s = Session(Near);
            for (int i = 0; i < 4; i++) s.Clan.AddMember(Fixtures.Cultivator(age: 30, realm: CultivationRealm.Foundation, stage: 3));
            s.ShardSense.ProcessYear();
            var plan = BalanceRun.NextProbe(s);
            Assert.AreEqual(RegionOf(s, Near), s.Factions.GetFactionByName(plan.Target).RegionId);
        }
    }
}
