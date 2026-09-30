using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The last shard, in the Great Void (LORE.md §11.5, §5.8, B3c4): the mirror senses it once the six others are
    /// integrated; a member of the Purple Mansion or above, guided by the waking mirror, searches the Void for it — where
    /// countless demons dwell. The Jade Buckle gives the mirror back the Great Void (📚).
    /// </summary>
    [TestFixture]
    public class VoidShardTests
    {
        private const double Pass = 0.0;
        private const double Fail = 0.99;

        private static string VoidShard => Fixtures.Content.Shards.Single(s => s.Source == ShardSource.GreatVoid).Id;

        /// <summary>The six other shards recovered, and the mirror awake again.</summary>
        private static TestWorld SixShards(System.Random rng)
        {
            var w = new TestWorld(rng);
            foreach (var shard in Fixtures.Content.Shards.Where(s => s.Source != ShardSource.GreatVoid)) w.Shards.Recover(shard.Id);
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 10, GamePhase.Management);
            return w;
        }

        [Test]
        public void TheJadeBuckle_GivesTheMirrorBackTheGreatVoid()
        {
            var w = new TestWorld();
            Assert.IsFalse(w.Shards.CanTraverseVoid);
            w.Shards.Recover("jade-buckle");
            Assert.IsTrue(w.Shards.CanTraverseVoid);
        }

        [Test]
        public void TheVoid_IsSearched_OnlyOnceTheSixOtherShardsAreIntegrated()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var seeker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            StringAssert.Contains("six autres", w.Shards.VoidSearch(seeker.ID).Refusal);
        }

        [Test]
        public void TheVoid_OpensOnlyFromThePurpleMansion()
        {
            var w = SixShards(new FixedRandom(Pass));
            var seeker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            StringAssert.Contains("Manoir Pourpre", w.Shards.VoidSearch(seeker.ID).Refusal);
        }

        [Test]
        public void TheVoid_IsSearched_WithTheMirrorAwake()
        {
            var w = SixShards(new FixedRandom(Pass));
            w.Mirror.Restore(w.Mirror.MirrorPower, w.Mirror.RestoredFragments, asleepUntil: w.Ctx.Clock.Year + 1);
            var seeker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            StringAssert.Contains("dort", w.Shards.VoidSearch(seeker.ID).Refusal);
        }

        [Test]
        public void ASearchInTheVoid_MayFindTheLastShard_AndRestoreTheMirror()
        {
            var w = SixShards(new FixedRandom(Pass));
            var seeker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));

            var outcome = w.Shards.VoidSearch(seeker.ID);

            Assert.IsTrue(outcome.Launched && outcome.Found && w.Shards.IsRecovered(VoidShard) && w.Mirror.RestoredFragments == 7);
        }

        [Test]
        public void ASeekerMayBeLostInTheVoid()
        {
            var w = SixShards(new SequenceRandom(Fail, Pass)); // nothing found; the demons of the Void take the seeker
            var seeker = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));

            var outcome = w.Shards.VoidSearch(seeker.ID);

            Assert.IsTrue(outcome.Launched && !outcome.Found && !seeker.IsAlive);
        }
    }
}
