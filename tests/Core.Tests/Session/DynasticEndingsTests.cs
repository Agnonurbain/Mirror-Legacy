using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Session
{
    /// <summary>
    /// The dynastic endings (LORE.md §11.9, B3b; the user's decisions of 2026-09-30): seventeen endings in endings.json,
    /// each told once when its conditions hold, kept in the Annals; the game goes on after them.
    /// </summary>
    [TestFixture]
    public class DynasticEndingsTests
    {
        private static GameSession Quiet() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static List<string> Watch(GameSession s)
        {
            var reached = new List<string>();
            s.Events.OnEndingReached += (ending, subject) => reached.Add(ending.Id);
            return reached;
        }

        private static CharacterData Member(GameSession s, CultivationRealm realm, GoldenCoreState position = GoldenCoreState.None)
        {
            var c = Fixtures.Cultivator(realm: realm);
            c.GoldenCore = position;
            s.Clan.AddMember(c);
            return c;
        }

        private static void Vassal(GameSession s, string power) =>
            s.Treaties.Conclude(new Treaty($"v-{power}", TreatyKind.Vassalage, power, s.Clock.Year, null, false, false, true));

        private static void NextYear(GameSession s) => s.Events.TriggerYearStarted(s.Clock.Year);

        /// <summary>
        /// A clan that outweighs every power of Linxi at war: war strength counts the strongest member and a fifth of the
        /// others (balance.json « wars »), and the Cloud Peak Sect weighs 145 — one Immortal alone does not.
        /// </summary>
        private static void Mighty(GameSession s)
        {
            Member(s, CultivationRealm.DaoEmbryo);
            for (int i = 0; i < 12; i++) Member(s, CultivationRealm.GoldenCore);
        }

        // ---- The content ----

        [Test]
        public void TheGameShips_SeventeenEndings()
        {
            Assert.AreEqual(17, Fixtures.Content.Endings.Count);
        }

        [Test]
        public void TheEndingsThatAwaitAnotherPhase_AreListedAmongTheGaps()
        {
            var gaps = ContentGaps.Report(Fixtures.Content);
            foreach (var id in new[] { "grand-culmination", "double-house", "ancestor-return", "core-legacy", "imperial-way", "other-daos" })
                Assert.IsTrue(gaps.Any(g => g.StartsWith(GameContentLoader.EndingsFile) && g.Contains(id)), id);
        }

        [Test]
        public void TheLoader_RefusesAnEndingWithoutConditions()
        {
            var ex = Assert.Throws<System.IO.InvalidDataException>(() => GameContentLoader.Load(file =>
                file == GameContentLoader.EndingsFile
                    ? "[ { \"id\": \"x\", \"name\": \"X\", \"narrative\": \"…\", \"conditions\": [], \"provenance\": \"Lore\", \"interpretedFields\": [] } ]"
                    : Fixtures.ReadDataFile(file)));
            StringAssert.Contains(GameContentLoader.EndingsFile, ex.Message);
        }

        // ---- Summits of cultivation ----

        [Test]
        public void TheAscension_IsReached_WhenAMemberBecomesADaoEmbryo_AndTheGameGoesOn()
        {
            var s = Quiet();
            var reached = Watch(s);
            var c = Member(s, CultivationRealm.DaoEmbryo);

            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.DaoEmbryo);

            CollectionAssert.Contains(reached, "ascension");
            Assert.IsTrue(s.Endings.IsReached("ascension") && !s.Victory.IsOver && c.IsAlive);
        }

        [Test]
        public void AnEnding_IsToldOnlyOnce()
        {
            var s = Quiet();
            var reached = Watch(s);
            var c = Member(s, CultivationRealm.DaoEmbryo);
            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.DaoEmbryo);
            NextYear(s);
            s.Events.TriggerBreakthroughSuccess(Member(s, CultivationRealm.DaoEmbryo), CultivationRealm.DaoEmbryo);
            Assert.AreEqual(1, reached.Count(id => id == "ascension"));
        }

        [Test]
        public void AnEnding_IsKeptInTheAnnals_WithWhoReachedIt()
        {
            var s = Quiet();
            var c = Member(s, CultivationRealm.DaoEmbryo);
            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.DaoEmbryo);

            var entry = s.Annals.Entries.Single(e => e.Kind == AnnalKind.EndingReached);
            Assert.IsTrue(entry.Ref == "ascension" && entry.Subject == c.FullName && entry.Year == s.Clock.Year);
            StringAssert.Contains("L'Ascension", AnnalsView.Describe(entry, s.Context.Content));
        }

        [Test]
        public void TheThroneOfAFruition_IsReached_ByTheFirstRealization()
        {
            var s = Quiet();
            var c = Member(s, CultivationRealm.GoldenCore, GoldenCoreState.Realization);
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Realization, GoldenCoreState.MetallicEssenceOnly);
            Assert.IsTrue(s.Endings.IsReached("fruition-throne") && !s.Endings.IsReached("river-bed"));
        }

        [Test]
        public void TheRiverBedMoved_IsReached_ByATransferOrATransformation()
        {
            var s = Quiet();
            var c = Member(s, CultivationRealm.GoldenCore, GoldenCoreState.Realization);
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Realization, GoldenCoreState.Intercalary);
            Assert.IsTrue(s.Endings.IsReached("river-bed"));
        }

        [Test]
        public void TheHouseOfTrueMonarchs_NeedsThreeLivingTrueMonarchs()
        {
            var s = Quiet();
            Member(s, CultivationRealm.GoldenCore);
            Member(s, CultivationRealm.GoldenCore);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("house-of-monarchs"), "two are not a house");

            Member(s, CultivationRealm.GoldenCore);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("house-of-monarchs"));
        }

        [Test]
        public void TheGoldenLine_NeedsThreeGenerationsOfTrueMonarchs_ParentToChild()
        {
            var s = Quiet();
            var grandmother = Member(s, CultivationRealm.GoldenCore);
            grandmother.IsMale = false;
            var father = Member(s, CultivationRealm.GoldenCore);
            father.MotherID = grandmother.ID;
            var child = Member(s, CultivationRealm.PurpleMansion);
            child.FatherID = father.ID;
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("golden-line"));

            child.Realm = CultivationRealm.GoldenCore;
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("golden-line"));
        }

        [Test]
        public void TheGrandCulmination_AwaitsTheUsersDecision_AndIsNeverReachedMeanwhile()
        {
            var s = Quiet();
            Member(s, CultivationRealm.DaoEmbryo);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("grand-culmination"));
            Assert.IsNotNull(Fixtures.Content.Endings.Single(e => e.Id == "grand-culmination").Awaits);
        }

        // ---- The clan and the world ----

        [Test]
        public void TheLakeIsUnified_WhenEveryOtherFamilyOfTheLakeIsTheClansVassal()
        {
            var s = Quiet();
            var lake = new[] { "Famille Lü", "Famille Tao", "Famille Lou", "Famille Fang", "Famille Kang" };
            foreach (var family in lake.Take(4)) Vassal(s, family);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("lake-unified"));

            Vassal(s, lake[4]);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("lake-unified"));
        }

        [Test]
        public void TheDebtOfTheTwelveGates_IsPaid_WhenSixOfTheirHeirsBow()
        {
            var s = Quiet();
            var heirs = s.Factions.Factions.Where(f => (f.Kind == FactionKind.Sect || f.Kind == FactionKind.Gate)
                && s.Context.Content.Regions.First(r => r.Id == f.RegionId).ParentId == "linxi").Select(f => f.Name).ToList();
            Assert.AreEqual(11, heirs.Count, "three sects and eight gates in Linxi");

            foreach (var heir in heirs.Take(5)) Vassal(s, heir);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("twelve-gates-debt"));

            Vassal(s, heirs[5]);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("twelve-gates-debt"));
        }

        [Test]
        public void TheHegemonyOfLinxi_IsHeldTenYearsInARow()
        {
            var s = Quiet();
            Mighty(s);
            foreach (var family in new[] { "Famille Lü", "Famille Tao", "Famille Lou" }) Vassal(s, family);

            for (int y = 0; y < 9; y++) NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("linxi-hegemony"), "nine years are not ten");
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("linxi-hegemony"));
        }

        [Test]
        public void TheHegemonyOfLinxi_StartsOver_WhenItSlips()
        {
            var s = Quiet();
            Mighty(s);
            foreach (var family in new[] { "Famille Lü", "Famille Tao", "Famille Lou" }) Vassal(s, family);
            for (int y = 0; y < 9; y++) NextYear(s);

            s.Treaties.End("v-Famille Lou");
            NextYear(s);
            Vassal(s, "Famille Lou");
            for (int y = 0; y < 9; y++) NextYear(s);

            Assert.IsFalse(s.Endings.IsReached("linxi-hegemony"));
        }

        [Test]
        public void TheMillennialLine_IsReached_InTheThousandthYear()
        {
            var s = Quiet();
            s.Clock.Restore(999, GamePhase.Management);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("millennial-line"));
            s.Clock.Restore(1000, GamePhase.Management);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("millennial-line"));
        }

        // ---- The mirror ----

        [Test]
        public void TheMirrorRestored_NeedsItsSevenShards()
        {
            var s = Quiet();
            s.Mirror.Restore(s.Mirror.MirrorPower, 6);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("mirror-restored"));
            s.Mirror.Restore(s.Mirror.MirrorPower, 7);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("mirror-restored"));
        }

        [Test]
        public void TheMirrorAwakened_NeedsTheSevenShards_AndARealizationOrADaoEmbryo()
        {
            var s = Quiet();
            s.Mirror.Restore(s.Mirror.MirrorPower, 7);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("mirror-awakened"));

            Member(s, CultivationRealm.GoldenCore, GoldenCoreState.Realization);
            NextYear(s);
            Assert.IsTrue(s.Endings.IsReached("mirror-awakened"));
        }

        // ---- The game ----

        [Test]
        public void ALostGame_ReachesNoEnding()
        {
            var s = Quiet();
            s.Events.TriggerMirrorSeized("Secte du Pic des Nuées");
            s.Mirror.Restore(s.Mirror.MirrorPower, 7);
            NextYear(s);
            Assert.IsFalse(s.Endings.IsReached("mirror-restored"));
        }

        [Test]
        public void TheEndings_SurviveASave()
        {
            var s = Quiet();
            var c = Member(s, CultivationRealm.GoldenCore, GoldenCoreState.Realization);
            s.Events.TriggerPositionTaken(c, GoldenCoreState.Realization, GoldenCoreState.Surplus);
            Mighty(s);
            foreach (var family in new[] { "Famille Lü", "Famille Tao", "Famille Lou" }) Vassal(s, family);
            for (int y = 0; y < 4; y++) NextYear(s);

            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            var reached = Watch(reloaded);

            Assert.IsTrue(reloaded.Endings.IsReached("river-bed") && reloaded.Endings.IsReached("fruition-throne"));
            for (int y = 0; y < 6; y++) NextYear(reloaded);
            CollectionAssert.AreEqual(new[] { "linxi-hegemony" }, reached, "the hegemony's years carry over; nothing is told twice");
        }
    }
}
