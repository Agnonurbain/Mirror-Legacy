using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Mirror
{
    /// <summary>
    /// The mirror's seven shards (LORE.md §11.5, B3c1; the user's decisions of 2026-09-30): one in the lake, two in ruins,
    /// three held by powers, one in the Great Void. Each shard recovered restores the mirror, gives back a memory, and puts
    /// the mirror's spirit to sleep while it integrates it (one year for the first, up to three for the last).
    /// </summary>
    [TestFixture]
    public class ShardTests
    {
        private const string LakeShard = "lake-jade";
        private static GameSession Quiet(int seed = 1) => GameSession.NewGame(new GameSetup { Seed = seed, Content = Fixtures.QuietContent });

        // ---- The content ----

        [Test]
        public void TheGameShips_SevenShards_OneLake_TwoRuins_ThreePowers_OneVoid()
        {
            var shards = Fixtures.Content.Shards;
            Assert.AreEqual(7, shards.Count);
            Assert.AreEqual(new[] { 1, 2, 3, 1 }, new[] { ShardSource.Lake, ShardSource.Ruins, ShardSource.Power, ShardSource.GreatVoid }
                .Select(source => shards.Count(s => s.Source == source)).ToArray());
        }

        [Test]
        public void TheFirstShard_TakesThreeYearsToDigest()
        {
            // 📚 Lu Jiangxian: « it then took him three years to digest the information in the jade » (the user's choice, 2026-10-04)
            var sleeps = Fixtures.Content.Shards.Select(s => s.SleepYears).ToList();
            Assert.AreEqual(3, sleeps.First());
            Assert.IsTrue(sleeps.All(y => y >= 1));
        }

        [Test]
        public void TheLakeShard_ComesFirst_AndHoldsTheSupremeYinSutra()
        {
            var first = Fixtures.Content.Shards.First();
            Assert.IsTrue(first.Id == LakeShard && first.Source == ShardSource.Lake && first.Memory.TechniqueId == "supreme-yin-sutra");
            Assert.IsTrue(Fixtures.Content.Techniques.Any(t => t.ID == "supreme-yin-sutra"));
        }

        // ---- Recovering a shard ----

        [Test]
        public void ARecoveredShard_RestoresTheMirror_AndGivesBackItsMemory()
        {
            var s = Quiet();
            Assert.IsFalse(s.Techniques.Knows("supreme-yin-sutra"));

            Assert.IsTrue(s.Shards.Recover(LakeShard));

            Assert.IsTrue(s.Mirror.RestoredFragments == 1 && s.Shards.IsRecovered(LakeShard) && s.Techniques.Knows("supreme-yin-sutra"));
        }

        [Test]
        public void AShard_IsRecoveredOnlyOnce()
        {
            var s = Quiet();
            s.Shards.Recover(LakeShard);
            Assert.IsFalse(s.Shards.Recover(LakeShard));
            Assert.AreEqual(1, s.Mirror.RestoredFragments);
        }

        [Test]
        public void ARecoveredShard_IsTold_InTheAnnals()
        {
            var s = Quiet();
            string told = null;
            s.Events.OnShardRecovered += shard => told = shard.Id;
            s.Shards.Recover(LakeShard);

            var entry = s.Annals.Entries.Single(e => e.Kind == AnnalKind.ShardRecovered);
            Assert.IsTrue(told == LakeShard && entry.Ref == LakeShard && entry.Value == 1);
            StringAssert.Contains("Jade du Lac", AnnalsView.Describe(entry, s.Context.Content));
        }

        // ---- The integration sleep ----

        [Test]
        public void TheMirror_SleepsWhileItIntegratesAShard_AndCannotIntervene()
        {
            var s = Quiet();
            s.Mirror.AddPower(MirrorSystem.MaxMirrorPower);
            s.Shards.Recover(LakeShard);

            Assert.IsTrue(s.Mirror.IsAsleep);
            Assert.IsFalse(s.Mirror.ConsumePower(1));
            Assert.AreEqual("le miroir dort : il intègre un éclat", s.Mirror.PayRefusal(1));
            Assert.IsNull(s.Mirror.PayRefusal(0), "what costs nothing needs no mirror");
        }

        [Test]
        public void TheMirror_WakesOnceTheSleepIsOver()
        {
            var s = Quiet();
            s.Shards.Recover(LakeShard); // the first: three years (📚 three years to digest the first jade)
            s.Clock.Restore(s.Clock.Year + Fixtures.Content.Shards.First().SleepYears, GamePhase.Management);
            Assert.IsFalse(s.Mirror.IsAsleep);
        }

        [Test]
        public void TheTalismanRitual_WaitsForTheMirrorToWake()
        {
            var s = Quiet();
            var bearer = s.Clan.GetPatriarch();
            var beast = new CapturedBeast("b", CultivationRealm.QiRefinement, 5);
            s.Resources.AddBeast(beast);
            s.Resources.RestorePrayers(1_000_000);
            s.Talismans.RestoreCalendar(s.Clock.Year);
            s.Shards.Recover(LakeShard);

            Assert.IsFalse(s.Talismans.PerformRitual(bearer, beast));
        }

        // ---- The lake ----

        [Test]
        public void SearchingTheLake_IsOpenToQiCultivators_UntilItsShardIsFound()
        {
            var qi = Fixtures.Cultivator();
            var breathing = Fixtures.Cultivator(realm: CultivationRealm.Embryonic);
            Assert.IsTrue(TaskRules.IsAllowed(qi, TaskType.SearchLake, lakeOpen: true));
            Assert.IsFalse(TaskRules.IsAllowed(qi, TaskType.SearchLake, lakeOpen: false));
            Assert.IsFalse(TaskRules.IsAllowed(breathing, TaskType.SearchLake, lakeOpen: true));
        }

        [Test]
        public void ASearcherOfTheLake_MayFindItsShard()
        {
            var w = new TestWorld(new FixedRandom(0.0)); // every roll succeeds
            var searcher = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignTask(searcher, TaskType.SearchLake);

            w.Tasks.ProcessYearlyTasks();

            Assert.IsTrue(w.Shards.IsRecovered(LakeShard) && !w.Shards.LakeSearchOpen);
        }

        [Test]
        public void ASearcherOfTheLake_UsuallyFindsNothingInAYear()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var searcher = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignTask(searcher, TaskType.SearchLake);
            w.Tasks.ProcessYearlyTasks();
            Assert.IsFalse(w.Shards.IsRecovered(LakeShard));
        }

        // ---- The game ----

        [Test]
        public void TheShardsAndTheSleep_SurviveASave()
        {
            var s = Quiet();
            s.Shards.Recover(LakeShard);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.IsTrue(reloaded.Shards.IsRecovered(LakeShard) && reloaded.Mirror.IsAsleep && reloaded.Mirror.RestoredFragments == 1);
        }

        [Test]
        public void SevenShards_RestoreTheMirror_AndTellItsEnding()
        {
            var s = Quiet();
            foreach (var shard in Fixtures.Content.Shards) s.Shards.Recover(shard.Id);
            s.Events.TriggerYearStarted(s.Clock.Year);
            Assert.IsTrue(s.Endings.IsReached("mirror-restored"));
        }
    }
}
