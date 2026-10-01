using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The other paths to an ascent method (LORE.md §11.10, B; 2026-10-01): a disciple sent to a power that holds one, the loot of
    /// a yielding enemy, a Purple Mansion's tomb, a stolen manual, a defector.
    /// </summary>
    [TestFixture]
    public class AscentPathsTests
    {
        private const string Holder = "Famille Bai";
        private const string Method = "silent-tide-sutra";

        private static AscentPathSettings Settings => Fixtures.Content.Balance.AscentPaths;

        private static TestWorld World(System.Random rng, int relation = 60)
        {
            var w = new TestWorld(rng);
            w.Factions.AddFaction(new FactionData { Name = Holder, Kind = FactionKind.Sect, RegionId = "linxi", RelationWithPlayer = relation,
                HighestRealm = CultivationRealm.PurpleMansion, PowerLevel = 1000, Techniques = { Method } });
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator()));
            return w;
        }

        [Test]
        public void ADisciple_LeavesForYears_AndComesBackWithTheMethod()
        {
            var w = World(new FixedRandom(0.99)); // never harvested, never caught copying, no defector
            var disciple = w.Join(Fixtures.Cultivator(stage: 5));
            Assert.IsNull(w.Paths.SendAsDisciple(disciple.ID, Holder));
            Assert.IsTrue(disciple.DiscipleOf == Holder && !MirrorChronicles.Characters.TaskRules.AllowedTasks(disciple).Skip(1).Any(), "away: no task");

            w.Ctx.Clock.Restore(1 + Settings.DiscipleYears, GamePhase.Management);
            w.Paths.ProcessYear();

            Assert.IsTrue(disciple.DiscipleOf == null && w.Techniques.Knows(Method));
            Assert.AreEqual(0, w.Suspicion.Evidence(Holder));
        }

        [Test]
        public void ADisciple_NeedsTheHoldersTrust()
        {
            var w = World(new FixedRandom(0.99), relation: Settings.DiscipleMinRelation - 1);
            var disciple = w.Join(Fixtures.Cultivator());
            StringAssert.Contains("relation", w.Paths.SendAsDisciple(disciple.ID, Holder));
        }

        [Test]
        public void ADisciple_MayBeHarvested()
        {
            var w = World(new FixedRandom(0.0));
            var disciple = w.Join(Fixtures.Cultivator());
            w.Paths.SendAsDisciple(disciple.ID, Holder);
            w.Paths.ProcessYear();
            Assert.IsFalse(disciple.IsAlive);
        }

        [Test]
        public void AYieldingEnemy_HandsOverItsAscentMethod()
        {
            var w = World(new FixedRandom(0.99));
            w.Ctx.Events.TriggerClanWarWon(Holder);
            Assert.IsTrue(w.Techniques.Knows(Method));
        }

        [Test]
        public void AManual_MayBeStolen()
        {
            var w = World(new FixedRandom(0.0));
            var thief = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            var outcome = w.Paths.StealManual(Holder, Method, new[] { thief.ID });
            Assert.IsTrue(outcome.Launched && outcome.Taken && w.Techniques.Knows(Method));
        }

        [Test]
        public void ACaughtThief_LeavesProof()
        {
            var w = World(new SequenceRandom(0.99, 0.0)); // the theft fails; the thief is caught
            var thief = w.Join(Fixtures.Cultivator());
            var outcome = w.Paths.StealManual(Holder, Method, new[] { thief.ID });
            Assert.IsTrue(outcome.Caught && !w.Techniques.Knows(Method) && w.Suspicion.Evidence(Holder) > 0);
        }

        [Test]
        public void ADiscoveryOfRuins_MayRevealATomb_ThatAnExpeditionRobs()
        {
            var w = World(new FixedRandom(0.0));
            w.Ctx.Events.TriggerRandomEventOccurred(new RandomEventData { Name = "Ruines", EventType = RandomEventType.RuinsDiscovery });
            string method = w.Paths.Tomb;
            Assert.IsNotNull(method);
            var explorer = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            Assert.IsTrue(w.Paths.ExploreTomb(new[] { explorer.ID }).Found);
            Assert.IsTrue(w.Techniques.Knows(method) && w.Paths.Tomb == null);
        }

        [Test]
        public void ADefector_JoinsWithHisMethod()
        {
            var w = World(new FixedRandom(0.0));
            int members = w.Clan.LivingMembers.Count;
            w.Paths.ProcessYear();
            var defector = w.Clan.LivingMembers.Skip(members).Single();
            Assert.IsTrue(defector.CultivationMethodId == Method && w.Techniques.Knows(Method));
            Assert.AreEqual(Holder, defector.SpyFor, "rolled low: a false defector");
        }

        [Test]
        public void TheTomb_SurvivesASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Paths.Restore("silent-tide-sutra");
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual("silent-tide-sutra", reloaded.Paths.Tomb);
        }
    }
}
