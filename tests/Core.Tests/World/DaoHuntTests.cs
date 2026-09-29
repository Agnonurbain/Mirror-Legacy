using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// A ripe Dao is prey (LORE.md §5.3.3; user decision 2026-09-29: a plot like the others). Only the powers of a higher
    /// realm hunt it, and first they must learn it is ripe — a member away is seen, one in seclusion hardly. One who knows
    /// strikes in time; the clan's defences — a Purple Mansion guardian, patrols, the Protective Formation, a defensive
    /// ally, seclusion — may foil the blow, and a foiled hunter is remembered. Devoured, the Dao dies and nobody is named.
    /// </summary>
    [TestFixture]
    public class DaoHuntTests
    {
        private static DaoHuntSettings Settings => Fixtures.Content.Balance.DaoHunts;

        private static TestWorld World(System.Random rng, GameContent content = null)
        {
            var w = new TestWorld(rng, content);
            w.Factions.InitializeFactions();
            return w;
        }

        private static CharacterData Ripe(TestWorld w, string foundation = "orthodox-water:boundless-sea")
        {
            var c = Fixtures.Cultivator(age: 150, realm: CultivationRealm.Foundation, stage: 4);
            c.FoundationId = foundation;
            return w.Join(c);
        }

        [Test]
        public void OnlyARipeDao_WhosePartnersTheWorldKnows_IsPrey()
        {
            var w = World(new FixedRandom(0.0));
            var ripe = Ripe(w);
            var young = Ripe(w);
            young.RealmStage = 3;
            var safe = Ripe(w, "dawnlight:universal-dawn-mist"); // none of its Dao Partners is known to the world (§2.4)
            Assert.IsTrue(FoundationRules.IsPrey(ripe, w.Ctx.Content));
            Assert.IsFalse(FoundationRules.IsPrey(young, w.Ctx.Content));
            Assert.IsFalse(FoundationRules.IsPrey(safe, w.Ctx.Content));
        }

        [Test]
        public void AHunter_MustFirstLearn_ThatADaoIsRipe()
        {
            var w = World(new FixedRandom(0.999));
            var prey = Ripe(w);
            w.DaoHunts.ProcessYear();
            Assert.IsTrue(prey.IsAlive);
            Assert.IsEmpty(w.DaoHunts.Known);
        }

        [Test]
        public void AHunterWhoKnows_Strikes_AndAnUnguardedDaoIsDevoured()
        {
            var w = World(new FixedRandom(0.0));
            var prey = Ripe(w);
            w.DaoHunts.ProcessYear(); // learnt
            Assert.IsTrue(prey.IsAlive, "a blow takes planning");
            Assert.IsTrue(w.DaoHunts.Known.All(k => w.Factions.GetFactionByName(k.Faction).HighestRealm >= Settings.HunterMinRealm));
            w.DaoHunts.ProcessYear();
            Assert.IsFalse(prey.IsAlive);
            Assert.AreEqual(DeathCause.FoundationDevoured, prey.CauseOfDeath);
            Assert.IsFalse(w.DaoHunts.Known.Any(k => k.MemberId == prey.ID));
        }

        [Test]
        public void OnlyThePowersOfAHigherRealm_Hunt()
        {
            var w = World(new FixedRandom(0.0));
            foreach (var power in w.Factions.Factions) power.HighestRealm = CultivationRealm.Foundation;
            Ripe(w);
            w.DaoHunts.ProcessYear();
            Assert.IsEmpty(w.DaoHunts.Known);
        }

        [Test]
        public void ADaoAway_IsSeen_OneInSeclusion_Hardly()
        {
            var w = World(new FixedRandom(0.0));
            var prey = Ripe(w);
            double home = DaoHuntRules.LearnChance(prey, Settings);
            prey.CurrentTask = TaskType.Diplomacy;
            double away = DaoHuntRules.LearnChance(prey, Settings);
            prey.CurrentTask = TaskType.Seclusion;
            double secluded = DaoHuntRules.LearnChance(prey, Settings);
            Assert.Greater(away, home);
            Assert.Less(secluded, home);
        }

        [Test]
        public void EveryDefence_LowersTheBlow()
        {
            var s = Settings;
            double bare = DaoHuntRules.StrikeSuccess(false, 0, 0, false, false, s, Fixtures.Content.Balance.RipeDaoGuardedFactor);
            Assert.Less(DaoHuntRules.StrikeSuccess(true, 0, 0, false, false, s, Fixtures.Content.Balance.RipeDaoGuardedFactor), bare, "a Purple Mansion guardian");
            Assert.Less(DaoHuntRules.StrikeSuccess(false, 3, 0, false, false, s, Fixtures.Content.Balance.RipeDaoGuardedFactor), bare, "patrols");
            Assert.Less(DaoHuntRules.StrikeSuccess(false, 0, 3, false, false, s, Fixtures.Content.Balance.RipeDaoGuardedFactor), bare, "the Protective Formation");
            Assert.Less(DaoHuntRules.StrikeSuccess(false, 0, 0, true, false, s, Fixtures.Content.Balance.RipeDaoGuardedFactor), bare, "a defensive ally");
            Assert.Less(DaoHuntRules.StrikeSuccess(false, 0, 0, false, true, s, Fixtures.Content.Balance.RipeDaoGuardedFactor), bare, "seclusion");
        }

        [Test]
        public void AFoiledHunter_IsTold_AndRemembered()
        {
            var foiled = Fixtures.Content with { Balance = Fixtures.Content.Balance with { DaoHunts = Settings with { StrikeSuccess = 0 } } };
            var w = World(new FixedRandom(0.0), foiled);
            var prey = Ripe(w);
            string told = null;
            w.Ctx.Events.OnDaoHuntFoiled += power => told = power;
            w.DaoHunts.ProcessYear();
            w.DaoHunts.ProcessYear();
            Assert.IsTrue(prey.IsAlive);
            Assert.IsNotNull(told);
            Assert.AreEqual(Fixtures.Content.Balance.ClanWatch.DaoHuntFoiled, w.Suspicion.ClanDistrust(told));
        }

        [Test]
        public void Seclusion_IsATaskOfTheCultivators_ThatDoesNothingElse()
        {
            var w = World(new FixedRandom(0.0));
            var prey = Ripe(w);
            Assert.IsTrue(w.Tasks.AssignTask(prey, TaskType.Seclusion));
            Assert.IsFalse(TaskRules.IsAllowed(Fixtures.Mortal(), TaskType.Seclusion));
            int xp = prey.CultivationXP;
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(xp, prey.CultivationXP);
            Assert.AreEqual("Réclusion", ClanDomainView.TaskLabel(TaskType.Seclusion));
        }

        [Test]
        public void TheDomain_TellsARipeDao()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var ripe = Fixtures.Cultivator(age: 150, realm: CultivationRealm.Foundation, stage: 4);
            ripe.FoundationId = "orthodox-water:boundless-sea";
            s.Clan.AddMember(ripe);
            StringAssert.Contains("Dao mûr", ClanDomainView.Roster(s).Single(r => r.Id == ripe.ID).Foundation);
        }

        [Test]
        public void RoundTrip_KeepsWhatTheHuntersKnow()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.DaoHunts.Restore(new[] { new DaoPrey("Secte du Pic des Nuées", "someone") });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(new DaoPrey("Secte du Pic des Nuées", "someone"), reloaded.DaoHunts.Known.Single());
        }
    }
}
