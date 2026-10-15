using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The haunting of the Divine Dao (L7; LORE.md §3.6; the user's choice of 2026-10-15): a Divine Attendant imprints its
    /// existence so deeply that after its death its memory haunts the clan for years — the stronger its hold on a Profundity,
    /// the stronger and longer the haunting. Untended, it may drive a member mad or bring a calamity; honoured, it lends the
    /// clan prestige each year but commemoration doubles the danger; exorcised, it ends, for spirit stones — half as many when
    /// a monk of the clan performs it.
    /// </summary>
    [TestFixture]
    public class HauntingTests
    {
        private static DivineDaoSettings Settings => Fixtures.Content.Balance.DivineDao;

        private static TestWorld World(double sample) => new TestWorld(new FixedRandom(sample));

        private static CharacterData Attendant(TestWorld w, CultivationSubPath method = CultivationSubPath.UnitedProfundity,
            CultivationRealm realm = CultivationRealm.Foundation)
        {
            var member = w.Join(Fixtures.Cultivator(age: 80, realm: realm, stage: 1));
            member.Path = CultivationPath.Divine;
            member.SubPath = method;
            if (realm == CultivationRealm.GoldenCore) member.GoldenCore = GoldenCoreState.DivineCore;
            return member;
        }

        private static Haunting Dies(TestWorld w, CharacterData member)
        {
            w.Clan.Kill(member, DeathCause.Combat);
            return w.Haunting.Hauntings.Single(h => h.Id == member.ID);
        }

        // ---- A haunting begins ----

        [Test]
        public void ADivineAttendant_WhoDies_HauntsTheClan()
        {
            var w = World(0.99);
            var attendant = Attendant(w);
            var haunting = Dies(w, attendant);
            Assert.AreEqual(attendant.FullName, haunting.Name);
            Assert.AreEqual(1, haunting.Strength);
            Assert.AreEqual(Settings.HauntYearsPerStrength, haunting.YearsLeft);
            Assert.IsFalse(haunting.Honoured);
        }

        [Test]
        public void AnOwnerOfAProfundity_HauntsMoreStrongly()
        {
            var w = World(0.99);
            Assert.AreEqual(2, Dies(w, Attendant(w, CultivationSubPath.OwnedProfundity)).Strength);
        }

        [Test]
        public void ADivineCore_HauntsTheStrongest_AndTheLongest()
        {
            var w = World(0.99);
            var haunting = Dies(w, Attendant(w, CultivationSubPath.OwnedProfundity, CultivationRealm.GoldenCore));
            Assert.AreEqual(3, haunting.Strength);
            Assert.AreEqual(3 * Settings.HauntYearsPerStrength, haunting.YearsLeft);
        }

        [Test]
        public void ACultivatorOfAnotherWay_HauntsNothing()
        {
            var w = World(0.99);
            var member = w.Join(Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: 1));
            w.Clan.Kill(member, DeathCause.Combat);
            Assert.IsEmpty(w.Haunting.Hauntings);
        }

        // ---- Untended ----

        [Test]
        public void AnUntendedHaunting_MayDriveAMemberMad()
        {
            var w = World(0.0);
            var living = w.Join(Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 3));
            Dies(w, Attendant(w));
            w.Haunting.ProcessYear();
            Assert.AreEqual(1, living.DaoWounds);
        }

        [Test]
        public void AnUntendedHaunting_MayBringACalamity()
        {
            var w = World(0.0);
            w.Join(Fixtures.Cultivator(age: 40));
            Dies(w, Attendant(w));
            w.Resources.SetSpiritStones(1000);
            w.Haunting.ProcessYear();
            Assert.AreEqual(1000 - (int)(1000 * Settings.CalamityStoneShare), w.Resources.SpiritStones);
        }

        [Test]
        public void AQuietYear_HarmsNobody()
        {
            var w = World(0.99);
            var living = w.Join(Fixtures.Cultivator(age: 40));
            Dies(w, Attendant(w));
            w.Resources.SetSpiritStones(1000);
            w.Haunting.ProcessYear();
            Assert.AreEqual(0, living.DaoWounds);
            Assert.AreEqual(1000, w.Resources.SpiritStones);
        }

        [Test]
        public void TheHaunting_FadesAfterItsYears()
        {
            var w = World(0.99);
            var haunting = Dies(w, Attendant(w));
            for (int i = 0; i < Settings.HauntYearsPerStrength; i++) w.Haunting.ProcessYear();
            Assert.IsEmpty(w.Haunting.Hauntings);
            Assert.AreEqual(0, haunting.YearsLeft);
        }

        // ---- Honoured ----

        [Test]
        public void HonouringTheHaunting_LendsPrestigeEachYear()
        {
            var w = World(0.99);
            var haunting = Dies(w, Attendant(w, CultivationSubPath.OwnedProfundity));
            Assert.IsNull(w.Haunting.Honour(haunting.Id));
            int before = w.Resources.Prestige;
            w.Haunting.ProcessYear();
            Assert.AreEqual(before + 2 * Settings.HonourPrestigePerStrength, w.Resources.Prestige);
        }

        [Test]
        public void Commemoration_DoublesTheDanger()
        {
            double between = Settings.MadnessChancePerStrength * 1.5; // above the untended risk, below the honoured one
            var untended = World(between);
            var calm = untended.Join(Fixtures.Cultivator(age: 40));
            Dies(untended, Attendant(untended));
            untended.Haunting.ProcessYear();
            Assert.AreEqual(0, calm.DaoWounds);

            var honoured = World(between);
            var maddened = honoured.Join(Fixtures.Cultivator(age: 40));
            honoured.Haunting.Honour(Dies(honoured, Attendant(honoured)).Id);
            honoured.Haunting.ProcessYear();
            Assert.AreEqual(1, maddened.DaoWounds);
        }

        // ---- Exorcised ----

        [Test]
        public void AnExorcism_CostsStones_AndEndsTheHaunting()
        {
            var w = World(0.99);
            var haunting = Dies(w, Attendant(w, CultivationSubPath.OwnedProfundity));
            int cost = 2 * Settings.ExorcismStonesPerStrength;
            Assert.AreEqual(cost, w.Haunting.ExorcismCost(haunting));
            w.Resources.SetSpiritStones(cost);
            Assert.IsNull(w.Haunting.Exorcise(haunting.Id));
            Assert.AreEqual(0, w.Resources.SpiritStones);
            Assert.IsEmpty(w.Haunting.Hauntings);
        }

        [Test]
        public void AMonkOfTheClan_ExorcisesForLess()
        {
            var w = World(0.99);
            var monk = w.Join(Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 2));
            monk.Path = CultivationPath.Buddhist;
            monk.SubPath = CultivationSubPath.AncientBuddhism;
            var haunting = Dies(w, Attendant(w));
            Assert.AreEqual((int)(Settings.ExorcismStonesPerStrength * Settings.MonkExorcismFactor), w.Haunting.ExorcismCost(haunting));
        }

        [Test]
        public void AnExorcism_WaitsForTheStones()
        {
            var w = World(0.99);
            var haunting = Dies(w, Attendant(w));
            w.Resources.SetSpiritStones(Settings.ExorcismStonesPerStrength - 1);
            Assert.IsNotNull(w.Haunting.Exorcise(haunting.Id));
            Assert.AreEqual(1, w.Haunting.Hauntings.Count);
        }

        // ---- Kept and shown ----

        [Test]
        public void TheHauntings_AreKept_AcrossASave()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var attendant = Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: 1);
            attendant.Path = CultivationPath.Divine;
            attendant.SubPath = CultivationSubPath.OwnedProfundity;
            s.Clan.AddMember(attendant);
            s.Clan.Kill(attendant, DeathCause.Combat);
            s.Haunting.Honour(attendant.ID);
            var loaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            var kept = loaded.Haunting.Hauntings.Single();
            Assert.AreEqual(attendant.ID, kept.Id);
            Assert.AreEqual(2, kept.Strength);
            Assert.IsTrue(kept.Honoured);
        }

        [Test]
        public void TheDiplomacyScreen_ListsTheHauntings_AndTheirExorcism()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var attendant = Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: 1);
            attendant.Path = CultivationPath.Divine;
            attendant.SubPath = CultivationSubPath.UnitedProfundity;
            s.Clan.AddMember(attendant);
            s.Clan.Kill(attendant, DeathCause.Combat);
            var line = DiplomacyView.Hauntings(s).Single();
            Assert.AreEqual(attendant.ID, line.Id);
            StringAssert.Contains(attendant.FullName, line.Label);
            Assert.AreEqual(Settings.ExorcismStonesPerStrength, line.ExorcismCost);
            Assert.IsFalse(line.Honoured);
        }
    }
}
