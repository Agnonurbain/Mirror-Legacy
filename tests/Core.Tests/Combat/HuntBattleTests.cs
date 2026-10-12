using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>
    /// A hunt's capture fought on the grid (G6; the user's choices of 2026-10-12): once the team got close, its strikers alone
    /// face the beast, weakened by the lures and the timing. Won, the beast is taken; lost, it breaks free and only a beast
    /// far stronger may kill a fallen striker. The clan may let chance settle it instead, and headless runs always do.
    /// </summary>
    [TestFixture]
    public class HuntBattleTests
    {
        private const string Ruan = "Famille Ruan";
        private static HuntSettings Settings => Fixtures.Content.Balance.Hunt;

        /// <summary>A world in the hunt's window, one scouted beast of the Ruan, and a player there to fight.</summary>
        private static (TestWorld w, WorldBeast beast) World(Random rng, CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 2)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Talismans.RestoreCalendar(w.Ctx.Clock.Year);
            var beast = new WorldBeast("iron-boar-heshan-1", "iron-boar", "heshan", realm, stage, Ruan);
            w.Bestiary.Restore(new[] { beast });
            w.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);
            w.Hunts.Spring = w.HuntBattles.Spring;
            return (w, beast);
        }

        private static CharacterData Hunter(TestWorld w, int stage = 7) =>
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: stage));

        private static HuntPlan Plan(WorldBeast beast, HuntTiming timing, params (CharacterData Member, HuntRole Role)[] team) => new HuntPlan
        {
            TargetBeastId = beast.Id,
            Team = team.ToDictionary(t => t.Member.ID, t => t.Role),
            Timing = timing,
            Cover = CoverStory.None,
            Aid = MirrorAid.None
        };

        private static (TestWorld w, WorldBeast beast, CharacterData striker) Sprung(Random rng,
            CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 2, int strikerStage = 7)
        {
            var (w, beast) = World(rng, realm, stage);
            var striker = Hunter(w, strikerStage);
            w.HuntBattles.Spring(Plan(beast, HuntTiming.Dawn, (striker, HuntRole.Striker)), beast);
            return (w, beast, striker);
        }

        private static void Win(TestWorld w)
        {
            foreach (var foe in w.HuntBattles.Current.Enemies) foe.TakeDamage(foe.MaxVitality);
            w.HuntBattles.Current.EndTurn();
        }

        private static void Lose(TestWorld w)
        {
            foreach (var striker in w.HuntBattles.Current.Allies) striker.TakeDamage(100000);
            w.HuntBattles.Current.EndTurn();
        }

        // ---- The capture waits for the strikers ----

        [Test]
        public void Execute_WaitsForTheBattle_WhenAPlayerIsThere()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            var outcome = w.Hunts.Execute(Plan(beast, HuntTiming.Dawn, (Hunter(w), HuntRole.Striker)));
            Assert.IsTrue(outcome.AwaitingBattle && w.HuntBattles.Pending != null);
        }

        [Test]
        public void Execute_RollsTheCapture_WhenNobodyIsThereToFight()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            w.Hunts.Spring = null;
            var outcome = w.Hunts.Execute(Plan(beast, HuntTiming.Dawn, (Hunter(w), HuntRole.Striker)));
            Assert.IsTrue(outcome.Captured);
        }

        [Test]
        public void Execute_StillFailsTheApproach_ByChance()
        {
            var (w, beast) = World(new FixedRandom(0.995));
            w.Hunts.Execute(Plan(beast, HuntTiming.Dawn, (Hunter(w), HuntRole.Striker)));
            Assert.IsNull(w.HuntBattles.Pending, "seen before it gets close: no battle");
        }

        [Test]
        public void Strikers_AreOnlyTheStrikers()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            var striker = Hunter(w);
            var lure = Hunter(w);
            w.HuntBattles.Spring(Plan(beast, HuntTiming.Dawn, (striker, HuntRole.Striker), (lure, HuntRole.Lure)), beast);
            CollectionAssert.AreEqual(new[] { striker }, w.HuntBattles.Strikers(w.HuntBattles.Pending));
        }

        [Test]
        public void Beast_StandsAtItsRealmAndStage()
        {
            var (w, _, _) = Sprung(new FixedRandom(0.0), CultivationRealm.Foundation, 4);
            var beast = w.HuntBattles.Beast(w.HuntBattles.Pending);
            Assert.AreEqual((CultivationRealm.Foundation, 4), (beast.Realm, beast.RealmStage));
        }

        // ---- The preparation weakens the beast ----

        [Test]
        public void Weakening_GrowsWithTheLures()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            var plan = Plan(beast, HuntTiming.Dawn, (Hunter(w), HuntRole.Striker), (Hunter(w), HuntRole.Lure), (Hunter(w), HuntRole.Lure));
            Assert.AreEqual(2 * Settings.BattleLureWeaken, w.HuntBattles.Weakening(plan), 1e-9);
        }

        [Test]
        public void Weakening_NeverFallsBelowNothing_ByNight()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            Assert.AreEqual(0, w.HuntBattles.Weakening(Plan(beast, HuntTiming.Night, (Hunter(w), HuntRole.Striker))));
        }

        [Test]
        public void Weakening_IsAtMost_TheSettingsShare()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            var team = new[] { (Hunter(w), HuntRole.Striker) }.Concat(Enumerable.Range(0, 20).Select(_ => (Hunter(w), HuntRole.Lure))).ToArray();
            Assert.AreEqual(Settings.BattleMaxWeaken, w.HuntBattles.Weakening(Plan(beast, HuntTiming.Dawn, team)), 1e-9);
        }

        [Test]
        public void Fight_OpensTheBattle_TheBeastWeakened()
        {
            var (w, beast) = World(new FixedRandom(0.0));
            var plan = Plan(beast, HuntTiming.Dawn, (Hunter(w), HuntRole.Striker), (Hunter(w), HuntRole.Lure));
            w.HuntBattles.Spring(plan, beast);
            Assert.IsNull(w.HuntBattles.Fight());
            var foe = w.HuntBattles.Current.Enemies.Single();
            Assert.AreEqual(foe.MaxVitality - (int)Math.Round(foe.MaxVitality * Settings.BattleLureWeaken), foe.CurrentVitality);
        }

        [Test]
        public void Fight_IsRefused_WithoutAHuntWaiting()
        {
            var (w, _) = World(new FixedRandom(0.0));
            Assert.IsNotNull(w.HuntBattles.Fight());
        }

        // ---- The battle's end ----

        [Test]
        public void Conclude_IsRefused_WhileTheBattleGoesOn()
        {
            var (w, _, _) = Sprung(new FixedRandom(0.0));
            w.HuntBattles.Fight();
            StringAssert.Contains("pas finie", w.HuntBattles.Conclude());
        }

        [Test]
        public void Conclude_TakesTheBeast_WhenTheStrikersWin()
        {
            var (w, beast, _) = Sprung(new FixedRandom(0.0));
            w.HuntBattles.Fight();
            Win(w);
            w.HuntBattles.Conclude();
            Assert.IsTrue(w.Resources.Beasts.Any(b => b.Id == beast.Id));
        }

        [Test]
        public void Conclude_LetsTheBeastGo_WhenTheStrikersLose()
        {
            var (w, beast, _) = Sprung(new FixedRandom(0.999));
            w.HuntBattles.Fight();
            Lose(w);
            w.HuntBattles.Conclude();
            Assert.IsTrue(w.Bestiary.Beasts.Any(b => b.Id == beast.Id));
        }

        [Test]
        public void Conclude_SparesAFallenStriker_WhenTheBeastWasNotFarStronger()
        {
            var (w, _, striker) = Sprung(new FixedRandom(0.0));
            w.HuntBattles.Fight();
            Lose(w);
            w.HuntBattles.Conclude();
            Assert.IsTrue(striker.IsAlive);
        }

        [Test]
        public void Conclude_MayKillAFallenStriker_WhenTheBeastWasFarStronger()
        {
            var (w, _, striker) = Sprung(new FixedRandom(0.0), CultivationRealm.Foundation, 9, strikerStage: 1);
            w.HuntBattles.Fight();
            Lose(w);
            w.HuntBattles.Conclude();
            Assert.IsFalse(striker.IsAlive);
        }

        [Test]
        public void Conclude_ClosesTheHunt()
        {
            var (w, _, _) = Sprung(new FixedRandom(0.0));
            w.HuntBattles.Fight();
            Win(w);
            w.HuntBattles.Conclude();
            Assert.IsNull(w.HuntBattles.Current);
        }

        [Test]
        public void LetBe_RollsTheCapture()
        {
            var (w, beast, _) = Sprung(new FixedRandom(0.0)); // the roll always takes it
            Assert.IsNull(w.HuntBattles.LetBe());
            Assert.IsTrue(w.Resources.Beasts.Any(b => b.Id == beast.Id) && w.HuntBattles.Pending == null);
        }

        // ---- In the session ----

        [Test]
        public void TheYear_WaitsForAHuntsCapture()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1) with { InteractiveBattles = true });
            s.HuntBattles.Restore(new PendingHunt(new HuntPlan { TargetBeastId = "x" }, s.Clock.Year));
            var (year, phase) = (s.Clock.Year, s.Clock.Phase);
            s.AdvancePhase();
            Assert.AreEqual((year, phase), (s.Clock.Year, s.Clock.Phase), "a hunt's capture is answered first");
        }

        [Test]
        public void RoundTrip_KeepsThePendingHunt()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(1));
            var plan = new HuntPlan { TargetBeastId = "x", Team = { ["a"] = HuntRole.Striker, ["b"] = HuntRole.Lure }, Timing = HuntTiming.Night };
            s.HuntBattles.Restore(new PendingHunt(plan, 3));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.VeteranSetup());
            var back = reloaded.HuntBattles.Pending;
            Assert.AreEqual((3, "x", HuntTiming.Night, 2), (back.Year, back.Plan.TargetBeastId, back.Plan.Timing, back.Plan.Team.Count));
        }
    }
}
