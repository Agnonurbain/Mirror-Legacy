using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Buddhist conversion and its two schools (R13-R14a; LORE.md §3.3, §3.7, §11.2; the user's choices of 2026-10-12): a
    /// dying cultivator converts at a temple to escape death, a realm lower, its immortal way lost; it then cultivates without
    /// trial or method at its temple's school's pace — the ancient school slow, up to the Dharma Master's apogee; the modern one
    /// faster, capped at the Dharma Master's beginning, its soul held by its temple, which may call it back.
    /// </summary>
    [TestFixture]
    public class BuddhismTests
    {
        private const string Modern = "Temple du Lotus";
        private const string Ancient = "Temple de la Merveille Profonde";

        private static BuddhismSettings Settings => Fixtures.Content.Balance.Buddhism;

        private static TestWorld World(double sample = 0.5)
        {
            var w = new TestWorld(new FixedRandom(sample));
            w.Factions.InitializeFactions();
            return w;
        }

        /// <summary>A Foundation cultivator with a foundation and a method, five years from its death.</summary>
        private static CharacterData Dying(TestWorld w, CultivationRealm realm = CultivationRealm.Foundation, int stage = 2)
        {
            var c = w.Join(Fixtures.Cultivator(age: 280, realm: realm, stage: stage));
            c.FoundationId = "orthodox-water:boundless-sea";
            c.MaxLifespan = c.Age + 5;
            return c;
        }

        /// <summary>A convert of the temple's school at a realm and stage, cultivating.</summary>
        private static CharacterData Convert(TestWorld w, string temple, CultivationRealm realm, int stage)
        {
            var c = Dying(w, realm == CultivationRealm.GoldenCore ? CultivationRealm.PurpleMansion : realm);
            Assert.IsTrue(w.Buddhism.Convert(c, temple));
            c.Realm = realm;
            c.RealmStage = stage;
            c.CurrentTask = TaskType.Cultivation;
            return c;
        }

        // ---- The conversion ----

        [Test]
        public void ADyingCultivator_Converts_ToItsTemplesSchool_ARealmLower()
        {
            var w = World();
            var monk = Dying(w);
            Assert.IsTrue(w.Buddhism.Convert(monk, Modern));
            Assert.AreEqual(CultivationPath.Buddhist, monk.Path);
            Assert.AreEqual(CultivationSubPath.ModernBuddhism, monk.SubPath);
            Assert.AreEqual(CultivationRealm.QiRefinement, monk.Realm);
            Assert.AreEqual(1, monk.RealmStage);
        }

        [Test]
        public void AnAncientTemple_ConvertsToTheAncientSchool()
        {
            var w = World();
            var monk = Dying(w);
            w.Buddhism.Convert(monk, Ancient);
            Assert.AreEqual(CultivationSubPath.AncientBuddhism, monk.SubPath);
        }

        [Test]
        public void TheConvert_GivesUpItsImmortalWay()
        {
            var w = World();
            var monk = Dying(w, CultivationRealm.PurpleMansion, 2);
            monk.DivineAbilities.Add("orthodox-water:boundless-sea");
            monk.PursuedAbility = "orthodox-water:ford-watcher";
            w.Buddhism.Convert(monk, Modern);
            Assert.IsNull(monk.FoundationId);
            Assert.IsEmpty(monk.DivineAbilities);
            Assert.IsNull(monk.PursuedAbility);
            Assert.IsNull(monk.CultivationMethodId);
            Assert.IsNull(monk.QiId);
        }

        [Test]
        public void TheConvert_EscapesDeath()
        {
            var w = World();
            var monk = Dying(w);
            w.Buddhism.Convert(monk, Modern);
            Assert.AreEqual(monk.Age + Settings.YearsGained, monk.MaxLifespan);
        }

        [Test]
        public void TheConversion_PaysTheOffering_AndWarmsTheTemple()
        {
            var w = World();
            var temple = w.Factions.GetFactionByName(Modern);
            int stones = w.Resources.SpiritStones, relation = temple.RelationWithPlayer;
            w.Buddhism.Convert(Dying(w), Modern);
            Assert.AreEqual(stones - Settings.OfferingStones, w.Resources.SpiritStones);
            Assert.Greater(temple.RelationWithPlayer, relation);
        }

        [Test]
        public void ACultivatorFarFromDeath_DoesNotConvert()
        {
            var w = World();
            var member = Dying(w);
            member.MaxLifespan = member.Age + Settings.DyingYears + 1;
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(member, Modern));
            Assert.IsFalse(w.Buddhism.Convert(member, Modern));
            Assert.AreEqual(CultivationPath.Immortal, member.Path);
        }

        [Test]
        public void AMortal_DoesNotConvert()
        {
            var w = World();
            var mortal = w.Join(Fixtures.Mortal(age: 70));
            mortal.MaxLifespan = mortal.Age + 2;
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(mortal, Modern));
        }

        [Test]
        public void ATrueMonarch_KeepsItsPosition_AndDoesNotConvert()
        {
            var w = World();
            var monarch = Dying(w, CultivationRealm.GoldenCore, 1);
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(monarch, Modern));
        }

        [Test]
        public void OnlyATemple_Converts()
        {
            var w = World();
            var notATemple = w.Factions.Factions.First(f => f.Path != CultivationPath.Buddhist);
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(Dying(w), notATemple.Name));
        }

        [Test]
        public void APoorClan_CannotPayTheOffering()
        {
            var w = World();
            w.Resources.SetSpiritStones(Settings.OfferingStones - 1);
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(Dying(w), Modern));
        }

        [Test]
        public void ABuddhist_DoesNotConvertAgain()
        {
            var w = World();
            var monk = Dying(w);
            w.Buddhism.Convert(monk, Modern);
            monk.MaxLifespan = monk.Age + 1;
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(monk, Ancient));
        }

        // ---- Cultivating its nature ----

        [Test]
        public void AModernMonk_Cultivates_AtItsSchoolsPace()
        {
            var w = World();
            var monk = Convert(w, Modern, CultivationRealm.Foundation, 1);
            monk.CultivationXP = 0;
            w.Buddhism.ProcessYear();
            double lent = 1 + Settings.LentSpeedPerRealm * (int)w.Factions.GetFactionByName(Modern).HighestRealm; // its temple's strength (R14)
            Assert.AreEqual((int)System.Math.Round((CultivationSystem.BaseYearlyXp + monk.SpiritualRoot / 2) * Settings.ModernSpeed * lent), monk.CultivationXP);
        }

        [Test]
        public void TheAncientSchool_IsSlowerThanTheModern()
        {
            var w = World();
            var modern = Convert(w, Modern, CultivationRealm.Foundation, 1);
            var ancient = Convert(w, Ancient, CultivationRealm.Foundation, 1);
            modern.CultivationXP = ancient.CultivationXP = 0;
            w.Buddhism.ProcessYear();
            Assert.Less(ancient.CultivationXP, modern.CultivationXP);
        }

        [Test]
        public void AMonk_CultivatesOnlyWhenCultivating()
        {
            var w = World();
            var monk = Convert(w, Modern, CultivationRealm.Foundation, 1);
            monk.CurrentTask = TaskType.Patrol;
            monk.CultivationXP = 0;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(0, monk.CultivationXP);
        }

        [Test]
        public void AMonk_RisesToTheNextRank_WithoutATrial()
        {
            var w = World();
            var monk = Convert(w, Ancient, CultivationRealm.Foundation, 4);
            monk.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, monk.Realm);
            Assert.AreEqual(1, monk.RealmStage);
        }

        [Test]
        public void AMahaRisingFurther_BecomesADharmaMaster()
        {
            var w = World();
            var monk = Convert(w, Ancient, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            monk.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.GoldenCore, monk.Realm);
            Assert.AreEqual(GoldenCoreState.DharmaMaster, monk.GoldenCore);
            Assert.AreEqual("Maître du Dharma", RankCatalog.DisplayName(monk));
        }

        [Test]
        public void AModernDharmaMaster_StopsAtItsBeginning()
        {
            var w = World();
            var monk = Convert(w, Modern, CultivationRealm.GoldenCore, Settings.ModernCeilingStage);
            monk.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.GoldenCore) * 2;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(Settings.ModernCeilingStage, monk.RealmStage);
        }

        [Test]
        public void AnAncientDharmaMaster_ReachesItsApogee()
        {
            var w = World();
            var monk = Convert(w, Ancient, CultivationRealm.GoldenCore, 1);
            monk.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.GoldenCore) * 10;
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationRealm.GoldenCore, monk.Realm);
            Assert.AreEqual(Settings.AncientCeilingStage, monk.RealmStage);
        }

        [Test]
        public void ARisingMonk_LivesAtLeastItsRanksSpan()
        {
            var w = World();
            var monk = Convert(w, Ancient, CultivationRealm.Foundation, 4);
            monk.MaxLifespan = monk.Age + 1;
            monk.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.Buddhism.ProcessYear();
            Assert.GreaterOrEqual(monk.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, 1));
        }

        [Test]
        public void AMonk_KeepsItsGainedYears_AcrossASave()
        {
            var w = World();
            var monk = Dying(w);
            w.Buddhism.Convert(monk, Modern);
            int lifespan = monk.MaxLifespan;
            PowerLadder.NormalizeLifespan(monk); // as every record on loading
            Assert.AreEqual(lifespan, monk.MaxLifespan);
        }

        [Test]
        public void AMonk_AttemptsNoImmortalTrial_NorAbility()
        {
            var w = World(0.0);
            var foundation = Convert(w, Modern, CultivationRealm.Foundation, 4);
            foundation.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            var mansion = Convert(w, Modern, CultivationRealm.PurpleMansion, 1);
            mansion.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion) * 3;
            Assert.DoesNotThrow(() => w.Breakthroughs.ProcessBreakthroughPhase());
            Assert.DoesNotThrow(() => w.PurpleMansion.ProcessBreakthroughPhase());
            w.Abilities.ProcessBreakthroughPhase();
            Assert.AreEqual(CultivationRealm.Foundation, foundation.Realm);
            Assert.IsEmpty(mansion.DivineAbilities);
        }

        /// <summary>A monk of a whole session (its Dharma Treasures, alchemy and Golden Core), converted then set at a rank.</summary>
        private static (MirrorChronicles.Session.GameSession, CharacterData) SessionMonk(CultivationRealm realm, int stage)
        {
            var s = MirrorChronicles.Session.GameSession.NewGame(Fixtures.VeteranSetup(4));
            var c = Fixtures.Cultivator(age: 280, realm: CultivationRealm.Foundation, stage: 2);
            c.MaxLifespan = c.Age + 5;
            s.Clan.AddMember(c);
            Assert.IsTrue(s.Buddhism.Convert(c, Ancient));
            c.Realm = realm;
            c.RealmStage = stage;
            return (s, c);
        }

        [Test]
        public void ADharmaMaster_IsNoTrueMonarch()
        {
            var (s, monk) = SessionMonk(CultivationRealm.GoldenCore, 1);
            monk.GoldenCore = GoldenCoreState.DharmaMaster;
            Assert.IsFalse(PowerLadder.IsTrueMonarch(monk));
            Assert.IsNotNull(s.Dharma.CondenseRefusal(monk), "no Dharma Treasure for a monk");
        }

        [Test]
        public void AFoundationMonk_NeitherRecastsNorBorrowsLight()
        {
            var (s, monk) = SessionMonk(CultivationRealm.Foundation, 4);
            StringAssert.Contains("voie immortelle", s.Alchemy.RecastRefusal(monk, "clear-spring-qi"));
            Assert.IsFalse(s.GoldenCore.BorrowLight(monk, "orthodox-water"));
        }

        // ---- The modern soul ----

        [Test]
        public void AModernTemple_CallsBackTheSoulItHolds_WhenTheClanFallsOut()
        {
            var w = World(0.0);
            var monk = Convert(w, Modern, CultivationRealm.Foundation, 1);
            var temple = w.Factions.GetFactionByName(Modern);
            temple.RelationWithPlayer = Settings.RecallBelow - 1;
            w.Buddhism.ProcessYear();
            Assert.IsTrue(monk.Departed);
            CollectionAssert.DoesNotContain(w.Clan.LivingMembers, monk);
        }

        [Test]
        public void AModernTemple_KeepsItsCalm_WhileTheClanIsInGoodStanding()
        {
            var w = World(0.0);
            var monk = Convert(w, Modern, CultivationRealm.Foundation, 1);
            w.Buddhism.ProcessYear();
            Assert.IsFalse(monk.Departed);
        }

        [Test]
        public void AnAncientTemple_NeverCallsASoulBack()
        {
            var w = World(0.0);
            var monk = Convert(w, Ancient, CultivationRealm.Foundation, 1);
            w.Factions.GetFactionByName(Ancient).RelationWithPlayer = Settings.RecallBelow - 50;
            w.Buddhism.ProcessYear();
            Assert.IsFalse(monk.Departed);
        }
    }
}
