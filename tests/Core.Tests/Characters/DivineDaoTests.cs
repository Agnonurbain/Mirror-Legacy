using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Divine Dao (R15; LORE.md §3.6; the wiki « Divine Dao »; the user's choices of 2026-10-13): a cultivator from the Qi
    /// Cultivation to the Purple Mansion becomes a Divine Attendant by binding itself to a Profundity. Borrowing a great
    /// partner's might makes it weigh a realm higher, until the pact ends; uniting with the clan's domain makes it undying but
    /// bound to the domain, capped at the Purple Mansion; owning a Profundity spends the clan's prestige, toward a Divine Core.
    /// </summary>
    [TestFixture]
    public class DivineDaoTests
    {
        private const string Tortoise = "black-tortoise";

        private static DivineDaoSettings Settings => Fixtures.Content.Balance.DivineDao;

        private static TestWorld World(double sample = 0.0) => new TestWorld(new FixedRandom(sample));

        private static CharacterData Cultivator(TestWorld w, CultivationRealm realm = CultivationRealm.Foundation, int stage = 2) =>
            w.Join(Fixtures.Cultivator(age: 60, realm: realm, stage: stage));

        private static void Pact(TestWorld w) => w.Patrons.RestorePacts(new[] { new PatronPact(Tortoise, 0, 50) });

        // ---- Becoming a Divine Attendant ----

        [Test]
        public void ACultivator_UnitesWithTheDomain_AndBecomesADivineAttendant()
        {
            var w = World();
            var member = Cultivator(w);
            Assert.IsNull(w.Divine.Unite(member));
            Assert.AreEqual(CultivationPath.Divine, member.Path);
            Assert.AreEqual(CultivationSubPath.UnitedProfundity, member.SubPath);
            Assert.AreEqual(CultivationRealm.Foundation, member.Realm);
            Assert.AreEqual("Serviteur Divin", RankCatalog.DisplayName(member));
        }

        [Test]
        public void NoAttendant_BelowTheQiCultivation_NorATrueMonarch()
        {
            var w = World();
            Assert.IsNotNull(w.Divine.AttendantRefusal(Cultivator(w, CultivationRealm.Embryonic, 4)));
            Assert.IsNotNull(w.Divine.AttendantRefusal(Cultivator(w, CultivationRealm.GoldenCore, 1)));
        }

        [Test]
        public void NoAttendant_OfAnotherWay()
        {
            var w = World();
            var monk = Cultivator(w);
            monk.Path = CultivationPath.Buddhist;
            Assert.IsNotNull(w.Divine.AttendantRefusal(monk));
        }

        // ---- Borrowing ----

        [Test]
        public void ABorrower_WeighsARealmHigher_WhileThePactHolds()
        {
            var w = World();
            Pact(w);
            var member = Cultivator(w, CultivationRealm.Foundation);
            Assert.IsNull(w.Divine.Borrow(member, Tortoise));
            Assert.AreEqual(CultivationSubPath.BorrowedProfundity, member.SubPath);
            Assert.AreEqual(Tortoise, member.ProfundityOf);
            Assert.AreEqual(CultivationRealm.Foundation + Settings.BorrowLeap, PowerLadder.EffectiveRealm(member));
        }

        [Test]
        public void NoBorrowing_WithoutThePartnersPact()
        {
            var w = World();
            Assert.IsNotNull(w.Divine.Borrow(Cultivator(w), Tortoise));
        }

        [Test]
        public void ThePactEnded_TheBorrowedMightFalls_AndWoundsTheDao()
        {
            var w = World();
            Pact(w);
            var member = Cultivator(w, CultivationRealm.Foundation);
            w.Divine.Borrow(member, Tortoise);
            w.Patrons.RestorePacts(new PatronPact[0]);
            w.Divine.ProcessYear();
            Assert.AreEqual(CultivationRealm.Foundation, PowerLadder.EffectiveRealm(member));
            Assert.IsNull(member.ProfundityOf);
            Assert.AreEqual(1, member.DaoWounds);
        }

        [Test]
        public void ABorrower_HasNoProgressOfItsOwn()
        {
            var w = World();
            Pact(w);
            var member = Cultivator(w);
            w.Divine.Borrow(member, Tortoise);
            member.CurrentTask = TaskType.Cultivation;
            member.CultivationXP = 0;
            w.Divine.ProcessYear();
            Assert.AreEqual(0, member.CultivationXP);
        }

        // ---- Uniting ----

        [Test]
        public void AUnitedAttendant_NoLongerDiesOfAge()
        {
            var w = World();
            var member = Cultivator(w);
            w.Divine.Unite(member);
            Assert.AreEqual(PowerLadder.Unbounded, member.MaxLifespan);
        }

        [Test]
        public void AUnitedAttendant_NeverLeavesTheDomain()
        {
            var w = World();
            var member = Cultivator(w);
            w.Divine.Unite(member);
            var allowed = TaskRules.AllowedTasks(member, huntOpen: true);
            CollectionAssert.DoesNotContain(allowed, TaskType.Diplomacy);
            CollectionAssert.DoesNotContain(allowed, TaskType.Espionage);
            CollectionAssert.DoesNotContain(allowed, TaskType.HuntBeast);
            CollectionAssert.Contains(allowed, TaskType.Cultivation);
            Assert.IsFalse(w.Hunts.IsFree(member), "no operation away from the domain");
        }

        [Test]
        public void AUnitedAttendant_CultivatesUpToThePurpleMansionAlone()
        {
            var w = World();
            var member = Cultivator(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Divine.Unite(member);
            member.CurrentTask = TaskType.Cultivation;
            member.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion) * 5;
            w.Divine.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, member.Realm);
        }

        [Test]
        public void AUnitedAttendant_Cultivates_AtItsPace()
        {
            var w = World();
            var member = Cultivator(w, CultivationRealm.Foundation, 1);
            w.Divine.Unite(member);
            member.CurrentTask = TaskType.Cultivation;
            member.CultivationXP = 0;
            w.Divine.ProcessYear();
            Assert.AreEqual((int)System.Math.Round((CultivationSystem.BaseYearlyXp + member.SpiritualRoot / 2) * Settings.Speed), member.CultivationXP);
        }

        // ---- Owning ----

        [Test]
        public void AnOwner_SpendsTheClansStanding_ToCultivate()
        {
            var w = World();
            var member = Cultivator(w, CultivationRealm.Foundation, 1);
            w.Divine.Own(member);
            w.Resources.AddPrestige(Settings.OwnPrestigePerYear);
            int prestige = w.Resources.Prestige;
            member.CurrentTask = TaskType.Cultivation;
            member.CultivationXP = 0;
            w.Divine.ProcessYear();
            Assert.Greater(member.CultivationXP, 0);
            Assert.AreEqual(prestige - Settings.OwnPrestigePerYear, w.Resources.Prestige);
        }

        [Test]
        public void AnOwner_WithoutStanding_DoesNotProgress()
        {
            var w = World();
            var member = Cultivator(w, CultivationRealm.Foundation, 1);
            w.Divine.Own(member);
            w.Resources.AddPrestige(-w.Resources.Prestige);
            member.CurrentTask = TaskType.Cultivation;
            member.CultivationXP = 0;
            w.Divine.ProcessYear();
            Assert.AreEqual(0, member.CultivationXP);
        }

        [Test]
        public void AnOwnerAtItsApogee_CondensesADivineCore()
        {
            var w = World(0.0);
            var member = Cultivator(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Divine.Own(member);
            w.Resources.AddPrestige(Settings.CorePrestige);
            Assert.IsNull(w.Divine.CondenseCore(member));
            Assert.AreEqual(CultivationRealm.GoldenCore, member.Realm);
            Assert.AreEqual(GoldenCoreState.DivineCore, member.GoldenCore);
            Assert.AreEqual("Noyau Divin", RankCatalog.DisplayName(member));
            Assert.GreaterOrEqual(member.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1));
            Assert.IsFalse(PowerLadder.IsTrueMonarch(member), "a Divine Core holds no Fruition");
        }

        [Test]
        public void AFailedCore_SpendsTheStanding_AndWoundsTheDao()
        {
            var w = World(0.99);
            var member = Cultivator(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Divine.Own(member);
            w.Resources.AddPrestige(Settings.CorePrestige - w.Resources.Prestige);
            Assert.IsNull(w.Divine.CondenseCore(member));
            Assert.AreEqual(CultivationRealm.PurpleMansion, member.Realm);
            Assert.AreEqual(0, w.Resources.Prestige);
            Assert.AreEqual(1, member.DaoWounds);
        }

        [Test]
        public void OnlyAnOwnerAtItsApogee_CondensesACore()
        {
            var w = World();
            w.Resources.AddPrestige(Settings.CorePrestige);
            var united = Cultivator(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Divine.Unite(united);
            Assert.IsNotNull(w.Divine.CoreRefusal(united));
            var young = Cultivator(w, CultivationRealm.PurpleMansion, 1);
            w.Divine.Own(young);
            Assert.IsNotNull(w.Divine.CoreRefusal(young));
        }

        [Test]
        public void ADivineCore_ReachesTheEndingOfTheOtherDaos()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var reached = new System.Collections.Generic.List<string>();
            s.Events.OnEndingReached += (ending, subject) => reached.Add(ending.Id);
            var god = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1);
            god.Path = CultivationPath.Divine;
            god.GoldenCore = GoldenCoreState.DivineCore;
            s.Clan.AddMember(god);
            s.Events.TriggerYearStarted(s.Clock.Year);
            CollectionAssert.Contains(reached, "other-daos");
        }
    }
}
