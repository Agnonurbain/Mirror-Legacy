using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The conversions between the cultivation Daos (L7; LORE.md §3.8; the wiki « Cultivation System »: « it is possible via
    /// special methods or techniques to switch between Cultivation Daos […] but each case is circumstantial and safest only at
    /// very high or low cultivation realms »; the user's choice of 2026-10-14: every conversion the wiki allows):
    /// <list type="bullet">
    /// <item>the passage itself is perilous in the middle realms — at the Foundation or the Purple Mansion it may wound the Dao;</item>
    /// <item>any human way, not only the clan's, may turn Buddhist at death's door — or when its way is closed (Tang Shedu) — and
    /// a lost debate may convert any of them by force;</item>
    /// <item>a member held by a temple may be converted under duress, and renounces the clan (Li Chengpan);</item>
    /// <item>a monk beaten by a temple of another school or Aspect may be inducted into it (« Buddhist Dao »);</item>
    /// <item>a Qi cultivator may still turn to the Heavenly Embryo, refining its Qi into a Diverse Mansion for more blood (the
    /// Golden Feather's disciples turned devils by the Blood Dharma Manual);</item>
    /// <item>a bound beast may become a Divine Attendant (Li Wushao, Edict God);</item>
    /// <item>and the powers' elders, dying, may also turn to a temple (parity).</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class PathConversionTests
    {
        private const string Modern = "Temple du Lotus";
        private const string Ancient = "Temple de la Merveille Profonde";
        private const string Gate = "grand-shaman:locust-shade";

        private static ConversionSettings Settings => Fixtures.Content.Balance.Conversions;
        private static BuddhismSettings Buddhism => Fixtures.Content.Balance.Buddhism;
        private static DevilDaoSettings Devil => Fixtures.Content.Balance.DevilDao;

        private static TestWorld World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            return w;
        }

        private static TestWorld World(double sample) => World(new FixedRandom(sample));

        private static CharacterData Cultivator(TestWorld w, CultivationRealm realm, int stage = 2, int age = 60)
        {
            var c = w.Join(Fixtures.Cultivator(age: age, realm: realm, stage: stage));
            c.MaxLifespan = c.Age + 200;
            return c;
        }

        private static void Dying(CharacterData c) => c.MaxLifespan = c.Age + 5;

        // ---- The peril of the passage ----

        [Test]
        public void APassage_AtTheFoundation_MayWoundTheDao()
        {
            var w = World(0.0); // the peril drawn
            var member = Cultivator(w, CultivationRealm.Foundation);
            Assert.IsNull(w.Divine.Unite(member));
            Assert.AreEqual(1, member.DaoWounds);
        }

        [Test]
        public void APassage_AtTheQiCultivation_IsSafe()
        {
            var w = World(0.0);
            var member = Cultivator(w, CultivationRealm.QiRefinement, 5);
            Assert.IsNull(w.Divine.Unite(member));
            Assert.AreEqual(0, member.DaoWounds);
        }

        [Test]
        public void APassage_InTheMiddle_IsOnlyPerilous_NotCertain()
        {
            var w = World(0.99); // the peril not drawn
            var member = Cultivator(w, CultivationRealm.PurpleMansion, 1);
            member.FoundationId = Gate;
            Assert.IsNull(w.Shamans.Convert(member, ShamanMethod.Sacrifice));
            Assert.AreEqual(0, member.DaoWounds);
        }

        [Test]
        public void ThePeril_IsOfTheMiddleRealmsAlone()
        {
            Assert.IsFalse(PathConversion.IsPerilous(CultivationRealm.Embryonic));
            Assert.IsFalse(PathConversion.IsPerilous(CultivationRealm.QiRefinement));
            Assert.IsTrue(PathConversion.IsPerilous(CultivationRealm.Foundation));
            Assert.IsTrue(PathConversion.IsPerilous(CultivationRealm.PurpleMansion));
            Assert.IsFalse(PathConversion.IsPerilous(CultivationRealm.GoldenCore));
        }

        // ---- Buddhism from every human way ----

        [Test]
        public void ADyingDevil_ConvertsAtATemple()
        {
            var w = World(0.99);
            var devil = Cultivator(w, CultivationRealm.QiRefinement, 3);
            devil.Path = CultivationPath.Devil;
            devil.SubPath = CultivationSubPath.HeavenlyEmbryoDemon;
            Dying(devil);
            Assert.IsTrue(w.Buddhism.Convert(devil, Ancient));
            Assert.AreEqual(CultivationPath.Buddhist, devil.Path);
            Assert.AreEqual(CultivationSubPath.AncientBuddhism, devil.SubPath);
        }

        [Test]
        public void ADyingShaman_ConvertsAtATemple_ItsMethodLeft()
        {
            var w = World(0.99);
            var shaman = Cultivator(w, CultivationRealm.Foundation, 1, age: 280);
            shaman.FoundationId = Gate;
            Assert.IsNull(w.Shamans.Convert(shaman, ShamanMethod.Transformation));
            Dying(shaman);
            Assert.IsTrue(w.Buddhism.Convert(shaman, Modern));
            Assert.AreEqual(CultivationPath.Buddhist, shaman.Path);
            Assert.AreEqual(ShamanMethod.None, shaman.ShamanMethod);
            Assert.AreEqual(0, shaman.BorrowedLeap, "its transformation's leap is gone");
        }

        [Test]
        public void ADyingAttendant_ConvertsAtATemple_ItsProfundityLeft()
        {
            var w = World(0.99);
            var attendant = Cultivator(w, CultivationRealm.Foundation);
            w.Patrons.RestorePacts(new[] { new PatronPact("black-tortoise", 0, 50) });
            Assert.IsNull(w.Divine.Borrow(attendant, "black-tortoise"));
            Dying(attendant);
            Assert.IsTrue(w.Buddhism.Convert(attendant, Modern));
            Assert.AreEqual(CultivationPath.Buddhist, attendant.Path);
            Assert.IsNull(attendant.ProfundityOf);
            Assert.AreEqual(0, attendant.BorrowedLeap);
        }

        [Test]
        public void AClosedWay_MayConvert_WithoutDying_AndGainsNoYears()
        {
            var w = World(0.99);
            var stuck = Cultivator(w, CultivationRealm.QiRefinement, 9);
            stuck.ProgressionSealed = true; // its way closed (Tang Shedu)
            int lifespan = stuck.MaxLifespan;
            Assert.IsNull(w.Buddhism.ConversionRefusal(stuck, Modern));
            Assert.IsTrue(w.Buddhism.Convert(stuck, Modern));
            Assert.AreEqual(lifespan, stuck.MaxLifespan, "not dying: no years gained");
            Assert.IsFalse(stuck.ProgressionSealed, "a new way: its nature cultivates again");
        }

        [Test]
        public void AnOpenWay_FarFromDeath_StillMayNotConvert()
        {
            var w = World(0.99);
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(Cultivator(w, CultivationRealm.Foundation), Modern));
        }

        [Test]
        public void AMonk_DoesNotConvertAgain()
        {
            var w = World(0.99);
            var monk = Cultivator(w, CultivationRealm.Foundation);
            Dying(monk);
            Assert.IsTrue(w.Buddhism.Convert(monk, Modern));
            Dying(monk);
            Assert.IsNotNull(w.Buddhism.ConversionRefusal(monk, Ancient));
        }

        // ---- Debates ----

        [Test]
        public void ADevilWhoLoses_MayBeConvertedByForce()
        {
            var w = World(new SequenceRandom(0.99, 0.0)); // the debate lost, the conversion drawn
            var devil = Cultivator(w, CultivationRealm.QiRefinement, 3);
            devil.Path = CultivationPath.Devil;
            devil.SubPath = CultivationSubPath.HeavenlyEmbryoDemon;
            w.Debates.Challenge(devil, Modern);
            Assert.AreEqual(DebateFate.Converted, w.Debates.LastFate);
            Assert.AreEqual(CultivationPath.Buddhist, devil.Path);
        }

        [Test]
        public void AModernMonk_BeatenByAnAncientTemple_MayBeInducted_KeepingItsRank()
        {
            var w = World(new SequenceRandom(0.99, 0.0)); // the debate lost, the induction drawn
            var monk = Cultivator(w, CultivationRealm.Foundation);
            Dying(monk);
            Assert.IsTrue(w.Buddhism.Convert(monk, Modern));
            monk.Realm = CultivationRealm.PurpleMansion;
            monk.RealmStage = 2;
            w.Debates.Challenge(monk, Ancient);
            Assert.AreEqual(DebateFate.Inducted, w.Debates.LastFate);
            Assert.AreEqual(Ancient, monk.Temple);
            Assert.AreEqual(CultivationSubPath.AncientBuddhism, monk.SubPath);
            Assert.AreEqual(CultivationRealm.PurpleMansion, monk.Realm);
            Assert.AreEqual(2, monk.RealmStage);
        }

        [Test]
        public void AnAncientDharmaMaster_InductedModern_IsCappedAtTheModernCeiling()
        {
            var w = World(new SequenceRandom(0.99, 0.0));
            var monk = Cultivator(w, CultivationRealm.PurpleMansion);
            Dying(monk);
            Assert.IsTrue(w.Buddhism.Convert(monk, Ancient));
            monk.Realm = CultivationRealm.GoldenCore;
            monk.RealmStage = Buddhism.AncientCeilingStage;
            w.Debates.Challenge(monk, Modern);
            Assert.AreEqual(DebateFate.Inducted, w.Debates.LastFate);
            Assert.AreEqual(CultivationSubPath.ModernBuddhism, monk.SubPath);
            Assert.AreEqual(CultivationRealm.GoldenCore, monk.Realm);
            Assert.AreEqual(Buddhism.ModernCeilingStage, monk.RealmStage);
        }

        [Test]
        public void AMonkBeatenByItsOwnTemple_IsNotInducted_ItFalls()
        {
            var w = World(new SequenceRandom(0.99, 0.0));
            var monk = Cultivator(w, CultivationRealm.Foundation);
            Dying(monk);
            Assert.IsTrue(w.Buddhism.Convert(monk, Modern));
            monk.Realm = CultivationRealm.PurpleMansion;
            w.Debates.Challenge(monk, Modern);
            Assert.AreEqual(DebateFate.Fell, w.Debates.LastFate);
            Assert.AreEqual(Modern, monk.Temple);
        }

        // ---- A captive under duress ----

        [Test]
        public void ACaptiveOfATemple_MayBeConverted_AndRenouncesTheClan()
        {
            var w = World(0.0);
            var captive = Cultivator(w, CultivationRealm.QiRefinement, 6);
            w.Captives.Take(captive, Modern);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationPath.Buddhist, captive.Path);
            Assert.AreEqual(Modern, captive.Temple);
            Assert.IsTrue(captive.Departed, "converted, it serves the temple now");
            Assert.IsFalse(w.Clan.LivingMembers.Contains(captive));
        }

        [Test]
        public void ACaptiveOfAnotherPower_IsNotConverted()
        {
            var w = World(0.0);
            var captive = Cultivator(w, CultivationRealm.QiRefinement, 6);
            var notATemple = w.Factions.Factions.First(f => f.Path != CultivationPath.Buddhist);
            w.Captives.Take(captive, notATemple.Name);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationPath.Immortal, captive.Path);
            Assert.IsFalse(captive.Departed);
        }

        [Test]
        public void ACaptiveTrueMonarch_IsNotConverted()
        {
            var w = World(0.0);
            var monarch = Cultivator(w, CultivationRealm.GoldenCore, 1, age: 300);
            w.Captives.Take(monarch, Modern);
            w.Buddhism.ProcessYear();
            Assert.AreEqual(CultivationPath.Immortal, monarch.Path);
        }

        // ---- The Heavenly Embryo from the Qi Cultivation ----

        [Test]
        public void AQiCultivator_RefinesItsQiIntoADiverseMansion_ForMoreBlood()
        {
            var w = World(0.0);
            var member = Cultivator(w, CultivationRealm.QiRefinement, 5, age: 30);
            member.CultivationMethodId = "some-method";
            member.QiId = "some-qi";
            w.Resources.AddBlood(Settings.QiFormBlood);
            Assert.IsNull(w.Devils.FormDiverseMansion(member));
            Assert.AreEqual(CultivationPath.Devil, member.Path);
            Assert.AreEqual(CultivationRealm.QiRefinement, member.Realm);
            Assert.AreEqual(1, member.RealmStage, "its Qi refined away: the Diverse Mansion begins anew");
            Assert.IsNull(member.QiId);
            Assert.IsNull(member.CultivationMethodId);
            Assert.AreEqual(0, w.Resources.BloodEssence);
        }

        [Test]
        public void AQiCultivator_NeedsMoreBlood_ThanAChakraBreather()
        {
            Assert.Greater(Settings.QiFormBlood, Devil.FormBlood);
            var w = World(0.0);
            var member = Cultivator(w, CultivationRealm.QiRefinement, 5, age: 30);
            w.Resources.AddBlood(Devil.FormBlood);
            Assert.IsNotNull(w.Devils.FormRefusal(member));
        }

        [Test]
        public void AFoundation_DoesNotTurnToTheHeavenlyEmbryo()
        {
            var w = World(0.0);
            var member = Cultivator(w, CultivationRealm.Foundation);
            w.Resources.AddBlood(Settings.QiFormBlood);
            Assert.IsNotNull(w.Devils.FormRefusal(member));
        }

        // ---- A bound beast, god of an edict ----

        [Test]
        public void ABoundBeast_BecomesADivineAttendant()
        {
            var w = World(0.99);
            var beast = w.DemonicDao.Bind(Captured(w));
            Assert.IsNull(w.Divine.Unite(beast));
            Assert.AreEqual(CultivationPath.Divine, beast.Path);
            Assert.AreEqual(Species.SpiritBeast, beast.Species, "still a beast, now a god");
        }

        private static CapturedBeast Captured(TestWorld w)
        {
            var beast = new CapturedBeast("mist-wolf-region-1", CultivationRealm.Foundation, 2, null);
            w.Resources.AddBeast(beast);
            return beast;
        }
    }

    /// <summary>The powers' elders turn Buddhist too, at death's door (parity, LORE.md §3.8).</summary>
    [TestFixture]
    public class ElderConversionTests
    {
        private const string Peak = "Secte du Pic des Nuées";

        private static GameSession Session(double chance) => GameSession.NewGame(new GameSetup
        {
            Seed = 1,
            Content = Fixtures.QuietContent with
            {
                Balance = Fixtures.QuietContent.Balance with
                {
                    Conversions = Fixtures.QuietContent.Balance.Conversions with { ElderConversionChance = chance }
                }
            }
        });

        private static FactionElder LastYear(GameSession s, CultivationRealm realm)
        {
            var power = s.Factions.GetFactionByName(Peak);
            var elder = new FactionElder { Id = "old", Name = "Vieil Aîné", Realm = realm, Stage = 2,
                BornYear = s.Clock.Year + 1 - 200, MaxLifespan = 200, RealmSinceYear = s.Clock.Year - 50 };
            power.Elders.Add(elder);
            return elder;
        }

        private static void AYear(GameSession s)
        {
            s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
            s.Elders.ProcessYear();
        }

        [Test]
        public void ADyingElder_MayTurnToATemple_ARealmLower()
        {
            var s = Session(1.0);
            var elder = LastYear(s, CultivationRealm.PurpleMansion);
            AYear(s);
            Assert.IsFalse(s.Factions.GetFactionByName(Peak).Elders.Contains(elder));
            var temple = s.Factions.Factions.Single(f => f.Elders.Contains(elder));
            Assert.AreEqual(CultivationPath.Buddhist, temple.Path);
            Assert.AreEqual(CultivationRealm.Foundation, elder.Realm);
            Assert.Greater(elder.MaxLifespan - elder.Age(s.Clock.Year), 0, "death escaped");
        }

        [Test]
        public void WithoutTheChance_ItDies()
        {
            var s = Session(0.0);
            var elder = LastYear(s, CultivationRealm.PurpleMansion);
            AYear(s);
            Assert.IsFalse(s.Factions.Factions.Any(f => f.Elders.Contains(elder)));
        }

        [Test]
        public void ATrueMonarch_DoesNotTurn()
        {
            var s = Session(1.0);
            var elder = LastYear(s, CultivationRealm.GoldenCore);
            AYear(s);
            Assert.IsFalse(s.Factions.Factions.Any(f => f.Elders.Contains(elder)));
        }
    }
}
