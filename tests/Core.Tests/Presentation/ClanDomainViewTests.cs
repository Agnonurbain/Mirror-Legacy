using System;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>What the clan domain screen shows: the turn, the treasury and the roster.</summary>
    [TestFixture]
    public class ClanDomainViewTests
    {
        private static GameSession NewGame() =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void Header_ShowsTheTurnTheTreasuryAndTheMirror()
        {
            var header = ClanDomainView.Header(NewGame());
            Assert.AreEqual(new DomainHeader(1, "Gestion", 1000, 50, 1), header);
        }

        [Test]
        public void Roster_PutsThePatriarchFirst()
        {
            var s = NewGame();
            var first = ClanDomainView.Roster(s)[0];
            Assert.IsTrue(first.IsPatriarch && first.Id == s.Clan.PatriarchID);
        }

        [Test]
        public void Roster_ListsOnlyTheLiving()
        {
            var s = NewGame();
            s.Clan.Kill(s.Clan.LivingMembers.Last(), DeathCause.Illness);
            Assert.AreEqual(4, ClanDomainView.Roster(s).Count);
        }

        [Test]
        public void Roster_ShowsTheRankName()
        {
            Assert.AreEqual("Culture du Qi — 3e niveau (début)", ClanDomainView.Roster(NewGame())[0].Rank);
        }

        [Test]
        public void Roster_OffersOnlyTheTasksAMemberMayTake()
        {
            var s = NewGame();
            var embryonic = ClanDomainView.Roster(s).First(r => r.Rank.StartsWith("Respiration Embryonnaire"));
            CollectionAssert.AreEquivalent(new[] { TaskType.None, TaskType.Cultivation, TaskType.Rest }, embryonic.AllowedTasks);
        }

        // ---- Techniques and Qi (LORE.md §2) ----

        [Test]
        public void QiStock_ListsTheQiInStore()
        {
            CollectionAssert.AreEqual(new[] { new QiLine("Qi de la Source Claire", 2) }, ClanDomainView.QiStock(NewGame()));
        }

        [Test]
        public void Roster_ShowsTheMethodEachMemberPractises()
        {
            Assert.AreEqual("Sutra de la Source Claire (grade 3)", ClanDomainView.Roster(NewGame())[0].Method);
        }

        [Test]
        public void Roster_ShowsTheCommonBreathing_OfABreathingMemberWithoutAManual()
        {
            var embryonic = ClanDomainView.Roster(NewGame()).First(r => r.Rank.StartsWith("Respiration Embryonnaire"));
            Assert.AreEqual("Respiration commune", embryonic.Method);
        }

        [Test]
        public void Roster_OffersTheMethodsAMemberMayTakeUp()
        {
            var roster = ClanDomainView.Roster(NewGame());
            var embryonic = roster.First(r => r.Rank.StartsWith("Respiration Embryonnaire"));
            CollectionAssert.AreEqual(new[] { "clear-spring-sutra", "common-breath-method" }, embryonic.Methods.Select(m => m.Id));
            CollectionAssert.AreEqual(new[] { "clear-spring-sutra" }, roster[0].Methods.Select(m => m.Id)); // bound to the Clear Spring Qi
        }

        [Test]
        public void Roster_ShowsNoMethod_ForAMortal()
        {
            var s = NewGame();
            var child = s.Clan.GenerateChild(s.Clan.GetPatriarch(), s.Clan.LivingMembers.First(m => !m.IsMale && m.SpouseID == s.Clan.PatriarchID));
            child.HasSpiritualOrifice = false;

            var row = ClanDomainView.Roster(s).Single(r => r.Id == child.ID);

            Assert.IsTrue(row.Method == "—" && row.Methods.Count == 0);
        }

        // ---- Foundations, abilities, retreats, temper (L4) ----

        private static MemberRow RowOf(GameSession s, CharacterData member) => ClanDomainView.Roster(s).Single(r => r.Id == member.ID);

        [Test]
        public void Roster_ShowsEachMembersTemper()
        {
            Assert.IsTrue(ClanDomainView.Roster(NewGame()).All(r => !string.IsNullOrWhiteSpace(r.Temperament)));
        }

        [Test]
        public void TemperamentLabel_NamesEveryTemperDifferently()
        {
            var labels = Enum.GetValues(typeof(Temperament)).Cast<Temperament>().Where(t => t != Temperament.None)
                .Select(ClanDomainView.TemperamentLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        [Test]
        public void Roster_ShowsTheFoundationAndItsLineage()
        {
            var s = NewGame();
            var patriarch = s.Clan.GetPatriarch();
            patriarch.Realm = CultivationRealm.Foundation;
            patriarch.FoundationId = "orthodox-water:boundless-sea";

            Assert.AreEqual("Mer sans Rivage (Eau Orthodoxe)", RowOf(s, patriarch).Foundation);
        }

        [Test]
        public void Roster_ShowsTheRetreatUnderWay()
        {
            var s = NewGame();
            var patriarch = s.Clan.GetPatriarch();
            patriarch.Retreat = Retreat.Manifestation;
            patriarch.RetreatYearsLeft = 4;
            Assert.AreEqual("en retraite : Manifestation (4 ans)", RowOf(s, patriarch).Retreat);

            patriarch.Retreat = Retreat.GreatVoid;
            patriarch.ImprisonedInVoid = true;
            Assert.AreEqual("prisonnier du Grand Vide", RowOf(s, patriarch).Retreat);
        }

        [Test]
        public void Roster_CountsTheDivineAbilitiesOfThePurpleMansion()
        {
            var s = NewGame();
            var patriarch = s.Clan.GetPatriarch();
            patriarch.Realm = CultivationRealm.PurpleMansion;
            patriarch.DivineAbilities = new System.Collections.Generic.List<string> { "orthodox-water:boundless-sea", "orthodox-water:river-farewell" };

            Assert.AreEqual("2/5 capacités divines", RowOf(s, patriarch).Abilities);
            Assert.IsNull(ClanDomainView.Roster(s).First(r => r.Id != patriarch.ID).Abilities);
        }

        // ---- The Golden Core (L4b) ----

        [TestCase(GoldenCoreState.MetallicEssenceOnly, "orthodox-water", null, "Essence métallique sans position (vise Eau Orthodoxe)")]
        [TestCase(GoldenCoreState.Realization, "orthodox-water", null, "Réalisation — Eau Orthodoxe")]
        [TestCase(GoldenCoreState.Surplus, "mutable-water", null, "Surplus — Eau Muable")]
        [TestCase(GoldenCoreState.Intercalary, "nourishing-water", null, "Intercalaire — Eau Nourricière")]
        [TestCase(GoldenCoreState.TrueLeftHand, "lesser-yin", null, "Main Gauche vraie — Immortels de Jade Tressé (Yin Mineur)")]
        [TestCase(GoldenCoreState.FalseLeftHand, "mutable-water", "Tan Qing", "Main Gauche fausse — au service de Tan Qing (Eau Muable)")]
        public void Roster_ShowsTheGoldenCoreStanding(GoldenCoreState state, string fruitionId, string patron, string label)
        {
            var s = NewGame();
            var patriarch = s.Clan.GetPatriarch();
            patriarch.Realm = CultivationRealm.GoldenCore;
            patriarch.RealmStage = 1;
            patriarch.GoldenCore = state;
            patriarch.FruitionId = fruitionId;
            patriarch.PatronId = patron;

            Assert.AreEqual(label, RowOf(s, patriarch).Position);
        }

        [Test]
        public void Roster_ShowsNoStanding_BeforeTheGoldenCore()
        {
            Assert.IsTrue(ClanDomainView.Roster(NewGame()).All(r => r.Position == null));
        }

        [Test]
        public void MethodLabel_WritesTheHighestGradeAsSevenPlus()
        {
            Assert.AreEqual("Dialogue (grade 7+)", ClanDomainView.MethodLabel(new TechniqueData { Name = "Dialogue", Grade = 7 }));
        }

        [Test]
        public void TaskLabel_NamesTheQiHarvest()
        {
            Assert.AreEqual("Récolte de Qi", ClanDomainView.TaskLabel(TaskType.GatherQi));
        }

        [Test]
        public void PhaseLabel_NamesEveryPhaseDifferently()
        {
            var labels = Enum.GetValues(typeof(GamePhase)).Cast<GamePhase>().Select(ClanDomainView.PhaseLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        [Test]
        public void TaskLabel_NamesEveryTaskDifferently()
        {
            var labels = Enum.GetValues(typeof(TaskType)).Cast<TaskType>().Select(ClanDomainView.TaskLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        [Test]
        public void DeathLabel_NamesEveryCause()
        {
            var causes = Enum.GetValues(typeof(DeathCause)).Cast<DeathCause>().Where(c => c != DeathCause.None);
            Assert.IsTrue(causes.All(c => !string.IsNullOrWhiteSpace(ClanDomainView.DeathLabel(c))));
        }
    }
}
