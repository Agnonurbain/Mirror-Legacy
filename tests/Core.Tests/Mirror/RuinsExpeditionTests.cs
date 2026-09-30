using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The ruins' shards (LORE.md §11.5, B3c2): a discovery of ruins may reveal ancient ruins holding a shard; an expedition
    /// of one to three cultivators, measured against the ruins' guardian, brings it back — or wounds and kills.
    /// </summary>
    [TestFixture]
    public class RuinsExpeditionTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.99;
        private static readonly RandomEventData Ruins = new RandomEventData { Name = "Ruines", EventType = RandomEventType.RuinsDiscovery };

        private static string FirstRuins => Fixtures.Content.Shards.First(s => s.Source == ShardSource.Ruins).Id;
        private static string SecondRuins => Fixtures.Content.Shards.Where(s => s.Source == ShardSource.Ruins).ElementAt(1).Id;

        [Test]
        public void TheRuinsShards_HaveAGuardian_ThatGrowsFromOneRuinsToTheNext()
        {
            var ruins = Fixtures.Content.Shards.Where(s => s.Source == ShardSource.Ruins).ToList();
            Assert.IsTrue(ruins[0].GuardRealm == CultivationRealm.QiRefinement && ruins[1].GuardRealm == CultivationRealm.Foundation);
        }

        [Test]
        public void ADiscoveryOfRuins_MayRevealTheNextRuinsShard()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Ctx.Events.TriggerRandomEventOccurred(Ruins);
            CollectionAssert.AreEqual(new[] { FirstRuins }, w.Shards.RevealedRuins);
        }

        [Test]
        public void ADiscoveryOfRuins_UsuallyRevealsNothing()
        {
            var w = new TestWorld(new FixedRandom(Fail));
            w.Ctx.Events.TriggerRandomEventOccurred(Ruins);
            Assert.IsEmpty(w.Shards.RevealedRuins);
        }

        [Test]
        public void AnExpedition_IsRefused_ToRuinsNotYetFound()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var member = w.Join(Fixtures.Cultivator(stage: 9));
            var outcome = w.Shards.Expedition(FirstRuins, new[] { member.ID });
            Assert.IsFalse(outcome.Launched);
            StringAssert.Contains("ruines", outcome.Refusal);
        }

        [Test]
        public void AnExpedition_NeedsOneToThreeFreeQiCultivators()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Shards.RestoreRuins(new[] { FirstRuins });
            var breathing = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Embryonic));
            var many = Enumerable.Range(0, 4).Select(_ => w.Join(Fixtures.Cultivator()).ID).ToList();

            Assert.IsFalse(w.Shards.Expedition(FirstRuins, new[] { breathing.ID }).Launched);
            Assert.IsFalse(w.Shards.Expedition(FirstRuins, many).Launched);
            Assert.IsFalse(w.Shards.Expedition(FirstRuins, new string[0]).Launched);
        }

        [Test]
        public void ASuccessfulExpedition_BringsTheShardBack()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Shards.RestoreRuins(new[] { FirstRuins });
            var member = w.Join(Fixtures.Cultivator(stage: 9));

            var outcome = w.Shards.Expedition(FirstRuins, new[] { member.ID });

            Assert.IsTrue(outcome.Launched && outcome.Found && w.Shards.IsRecovered(FirstRuins) && member.LastOperationYear == w.Ctx.Clock.Year);
            Assert.IsEmpty(w.Shards.RevealedRuins, "ruins emptied of their shard are no longer a target");
        }

        [Test]
        public void AFailedExpedition_LeavesTheShard_AndMayCostTheWeakest()
        {
            var w = new TestWorld(new SequenceRandom(Fail, Pass)); // the search fails; the ruins take the weakest
            w.Shards.RestoreRuins(new[] { FirstRuins });
            var strong = w.Join(Fixtures.Cultivator(stage: 9));
            var weak = w.Join(Fixtures.Cultivator(stage: 1));

            var outcome = w.Shards.Expedition(FirstRuins, new[] { strong.ID, weak.ID });

            Assert.IsTrue(outcome.Launched && !outcome.Found && !w.Shards.IsRecovered(FirstRuins));
            Assert.IsTrue(!weak.IsAlive && strong.IsAlive);
        }

        [Test]
        public void AMember_GoesOnOneOperationAYear()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            w.Shards.RestoreRuins(new[] { FirstRuins, SecondRuins });
            var member = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            w.Shards.Expedition(FirstRuins, new[] { member.ID });
            Assert.IsFalse(w.Shards.Expedition(SecondRuins, new[] { member.ID }).Launched);
        }

        [Test]
        public void TheKnownRuins_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Shards.RestoreRuins(new[] { FirstRuins });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            CollectionAssert.AreEqual(new[] { FirstRuins }, reloaded.Shards.RevealedRuins);
        }
    }
}
