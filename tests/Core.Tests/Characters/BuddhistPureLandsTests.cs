using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Pure Lands, the borrowing pyramid and the Revered One (R13-14c; LORE.md §3.7; the user's choices of 2026-10-12):
    /// a dead ancient monk may choose its rebirth in the clan; a modern one's soul goes to its temple's Pure Land, given back
    /// only if the clan stands well with it; the child regains the monk's rank once grown. A modern monk cultivates faster the
    /// stronger its temple, and falls back to Monk if its temple is no more. An ancient Dharma Master at its apogee may become
    /// a Revered One, who no longer dies of age.
    /// </summary>
    [TestFixture]
    public class BuddhistPureLandsTests
    {
        private const string Modern = "Temple du Lotus";
        private const string Ancient = "Temple de la Merveille Profonde";

        private static BuddhismSettings Settings => Fixtures.Content.Balance.Buddhism;

        private static TestWorld World(System.Random rng = null)
        {
            var w = new TestWorld(rng ?? new FixedRandom(0.0));
            w.Factions.InitializeFactions();
            return w;
        }

        private static CharacterData Monk(TestWorld w, string temple, CultivationRealm realm = CultivationRealm.Foundation, int stage = 2)
        {
            var c = w.Join(Fixtures.Cultivator(age: 280, realm: CultivationRealm.Foundation, stage: 2));
            c.MaxLifespan = c.Age + 5;
            Assert.IsTrue(w.Buddhism.Convert(c, temple));
            c.Realm = realm;
            c.RealmStage = stage;
            c.MaxLifespan = c.Age + 200;
            return c;
        }

        private static CharacterData Child(TestWorld w) => w.Join(Fixtures.Mortal(age: 0));

        // ---- The Pure Lands ----

        [Test]
        public void ADeadAncientMonk_MayChooseItsRebirth()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.PurpleMansion, 2);
            w.Clan.Kill(monk, DeathCause.OldAge);
            Assert.AreEqual(1, w.Buddhism.PendingSouls.Count);
            Assert.AreEqual(monk.FullName, w.Buddhism.PendingSouls[0].Name);
        }

        [Test]
        public void TheNextChild_CarriesTheMonksSoul()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.PurpleMansion, 2);
            w.Clan.Kill(monk, DeathCause.OldAge);
            var child = Child(w);
            Assert.IsNotNull(child.RebornMonk);
            Assert.AreEqual(monk.FullName, child.RebornMonk.Name);
            Assert.IsTrue(child.HasSpiritualOrifice);
            Assert.IsEmpty(w.Buddhism.PendingSouls);
        }

        [Test]
        public void TheGrownChild_RegainsTheMonksRank()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.PurpleMansion, 2);
            w.Clan.Kill(monk, DeathCause.OldAge);
            var child = Child(w);
            child.Age = Settings.RegainAge;
            child.OrificeKnown = true;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationPath.Buddhist, child.Path);
            Assert.AreEqual(CultivationSubPath.AncientBuddhism, child.SubPath);
            Assert.AreEqual(Ancient, child.Temple);
            Assert.AreEqual(CultivationRealm.PurpleMansion, child.Realm);
            Assert.AreEqual(2, child.RealmStage);
            Assert.IsNull(child.RebornMonk);
            Assert.GreaterOrEqual(child.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, 2));
        }

        [Test]
        public void TheYoungChild_WaitsToRegainIt()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.PurpleMansion, 2);
            w.Clan.Kill(monk, DeathCause.OldAge);
            var child = Child(w);
            child.Age = Settings.RegainAge - 1;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationPath.Immortal, child.Path);
            Assert.IsNotNull(child.RebornMonk);
        }

        [Test]
        public void AnAncientMonk_MayNotChooseRebirth()
        {
            var w = World(new FixedRandom(0.99));
            var monk = Monk(w, Ancient);
            w.Clan.Kill(monk, DeathCause.OldAge);
            Assert.IsEmpty(w.Buddhism.PendingSouls);
        }

        [Test]
        public void AModernTemple_GivesTheSoulBack_ToAClanInGoodStanding()
        {
            var w = World();
            var monk = Monk(w, Modern);
            w.Clan.Kill(monk, DeathCause.OldAge);
            Assert.AreEqual(1, w.Buddhism.PendingSouls.Count);
        }

        [Test]
        public void AModernTemple_KeepsTheSoul_OfAClanFallenOut()
        {
            var w = World();
            var monk = Monk(w, Modern);
            w.Factions.GetFactionByName(Modern).RelationWithPlayer = Settings.ModernRebirthRelation - 1;
            w.Clan.Kill(monk, DeathCause.OldAge);
            Assert.IsEmpty(w.Buddhism.PendingSouls);
        }

        [Test]
        public void AnImmortal_LeavesNoSoulToTheTemples()
        {
            var w = World();
            var immortal = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            w.Clan.Kill(immortal, DeathCause.OldAge);
            Assert.IsEmpty(w.Buddhism.PendingSouls);
        }

        [Test]
        public void TheSouls_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(4));
            s.Buddhism.RestoreSouls(new[] { new MonkSoul("Mo Hui", Ancient, CultivationSubPath.AncientBuddhism, CultivationRealm.Foundation, 3) });
            var loaded = GameSession.FromSaveData(s.ToSaveData(), Fixtures.VeteranSetup(4));
            Assert.AreEqual("Mo Hui", loaded.Buddhism.PendingSouls.Single().Name);
        }

        // ---- The borrowing pyramid ----

        [Test]
        public void AModernMonk_BorrowsItsTemplesStrength()
        {
            var w = World();
            var monk = Monk(w, Modern);
            var temple = w.Factions.GetFactionByName(Modern);
            monk.CurrentTask = TaskType.Cultivation;
            monk.CultivationXP = 0;
            w.Buddhism.ProcessYear();
            double lent = 1 + Settings.LentSpeedPerRealm * (int)temple.HighestRealm;
            Assert.AreEqual((int)System.Math.Round((CultivationSystem.BaseYearlyXp + monk.SpiritualRoot / 2) * Settings.ModernSpeed * lent), monk.CultivationXP);
        }

        [Test]
        public void AModernMonk_FallsBackToMonk_WhenItsTempleIsNoMore()
        {
            var w = World();
            var monk = Monk(w, Modern, CultivationRealm.PurpleMansion, 3);
            w.Factions.RemoveFaction(Modern);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.QiRefinement, monk.Realm);
            Assert.AreEqual(CultivationPath.Buddhist, monk.Path);
        }

        [Test]
        public void AnAncientMonk_LosesNothing_WhenItsTempleIsNoMore()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.PurpleMansion, 3);
            w.Factions.RemoveFaction(Ancient);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, monk.Realm);
        }

        // ---- The Revered One ----

        [Test]
        public void AnAncientDharmaMaster_AtItsApogee_MayBecomeARevered()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.GoldenCore, Settings.AncientCeilingStage);
            monk.GoldenCore = GoldenCoreState.DharmaMaster;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.DaoEmbryo, monk.Realm);
            Assert.AreEqual("Vénérable", RankCatalog.DisplayName(monk));
            Assert.AreEqual(PowerLadder.Unbounded, monk.MaxLifespan);
        }

        [Test]
        public void ADharmaMasterShortOfItsApogee_IsNoRevered()
        {
            var w = World();
            var monk = Monk(w, Ancient, CultivationRealm.GoldenCore, Settings.AncientCeilingStage - 1);
            monk.GoldenCore = GoldenCoreState.DharmaMaster;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.GoldenCore, monk.Realm);
        }

        [Test]
        public void AModernDharmaMaster_NeverBecomesARevered()
        {
            var w = World();
            var monk = Monk(w, Modern, CultivationRealm.GoldenCore, Settings.ModernCeilingStage);
            monk.GoldenCore = GoldenCoreState.DharmaMaster;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.GoldenCore, monk.Realm);
        }

        [Test]
        public void ARevered_ReachesTheEndingOfTheOtherDaos()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var reached = new System.Collections.Generic.List<string>();
            s.Events.OnEndingReached += (ending, subject) => reached.Add(ending.Id);
            var revered = Fixtures.Cultivator(age: 400, realm: CultivationRealm.DaoEmbryo, stage: 1);
            revered.Path = CultivationPath.Buddhist;
            s.Clan.AddMember(revered);
            s.Events.TriggerYearStarted(s.Clock.Year);
            CollectionAssert.Contains(reached, "other-daos");
        }
    }
}
