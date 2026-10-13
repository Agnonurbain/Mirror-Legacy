using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Shamanic Talisman Dao (R17; LORE.md §3.5; the user's choices of 2026-10-13): a member whose foundation is of the
    /// Grand Shaman's lineage passes to the shamanic Dao keeping its realm (back to its first stage, its abilities kept) — at
    /// any time, or as a refuge from a failed Golden Core. It walks one method: Transformation weighs a realm higher,
    /// Concealment hides it from the powers' hunts, Sacrifice burns blood to cultivate faster. A rite of blood at the Purple
    /// Mansion's Grand Perfection may give it a shamanic Golden Core.
    /// </summary>
    [TestFixture]
    public class ShamanismTests
    {
        private const string Gate = "grand-shaman:locust-shade";

        private static readonly string[] FiveShaman =
        {
            "grand-shaman:locust-shade", "grand-shaman:untraceable-presence", "grand-shaman:earth-shaman-blessing",
            "grand-shaman:emperor-mandate", "grand-shaman:blood-drinker"
        };

        private static ShamanismSettings Settings => Fixtures.Content.Balance.Shamanism;

        private static TestWorld World(double sample = 0.0) => new TestWorld(new FixedRandom(sample));

        private static CharacterData GrandShamanFoundation(TestWorld w, CultivationRealm realm = CultivationRealm.Foundation, int stage = 3)
        {
            var c = w.Join(Fixtures.Cultivator(age: 80, realm: realm, stage: stage));
            c.FoundationId = Gate;
            c.MaxLifespan = c.Age + 300;
            return c;
        }

        private static CharacterData Shaman(TestWorld w, ShamanMethod method, CultivationRealm realm = CultivationRealm.Foundation, int stage = 1)
        {
            var c = GrandShamanFoundation(w, realm == CultivationRealm.GoldenCore ? CultivationRealm.PurpleMansion : realm);
            Assert.IsNull(w.Shamans.Convert(c, method));
            c.Realm = realm;
            c.RealmStage = stage;
            c.CurrentTask = TaskType.Cultivation;
            return c;
        }

        // ---- The passage ----

        [Test]
        public void AGrandShamanFoundation_PassesToTheShamanicDao_KeepingItsRealm()
        {
            var w = World();
            var member = GrandShamanFoundation(w, CultivationRealm.Foundation, 3);
            member.DivineAbilities = new List<string> { Gate };
            Assert.IsNull(w.Shamans.Convert(member, ShamanMethod.Sacrifice));
            Assert.AreEqual(CultivationPath.Shamanic, member.Path);
            Assert.AreEqual(CultivationSubPath.ShamanicTalisman, member.SubPath);
            Assert.AreEqual(ShamanMethod.Sacrifice, member.ShamanMethod);
            Assert.AreEqual(CultivationRealm.Foundation, member.Realm);
            Assert.AreEqual(1, member.RealmStage);
            CollectionAssert.Contains(member.DivineAbilities, Gate, "its abilities turned talismans");
        }

        [Test]
        public void AnotherFoundation_DoesNotOpenTheWay()
        {
            var w = World();
            var member = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 2));
            member.FoundationId = "orthodox-water:boundless-sea";
            Assert.IsNotNull(w.Shamans.ConversionRefusal(member));
        }

        [Test]
        public void NoPassage_WithoutAMethod_NorForATrueMonarch()
        {
            var w = World();
            Assert.IsNotNull(w.Shamans.Convert(GrandShamanFoundation(w), ShamanMethod.None));
            Assert.IsNotNull(w.Shamans.ConversionRefusal(GrandShamanFoundation(w, CultivationRealm.GoldenCore, 1)));
        }

        // ---- The methods ----

        [Test]
        public void ATransformationShaman_WeighsARealmHigher()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Transformation);
            Assert.AreEqual(CultivationRealm.Foundation + Settings.TransformLeap, PowerLadder.EffectiveRealm(shaman));
        }

        [Test]
        public void AConcealmentShaman_IsHidden_AndNoOtherIs()
        {
            var w = World();
            Assert.IsTrue(ShamanismSystem.IsConcealed(Shaman(w, ShamanMethod.Concealment)));
            Assert.IsFalse(ShamanismSystem.IsConcealed(Shaman(w, ShamanMethod.Sacrifice)));
        }

        [Test]
        public void AConcealedShaman_IsNoDaoHuntsPrey()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Concealment);
            CollectionAssert.DoesNotContain(w.DaoHunts.Preys(), shaman);
        }

        [Test]
        public void ASacrificeShaman_BurnsBlood_ToCultivateFaster()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Sacrifice);
            w.Resources.AddBlood(Settings.SacrificeBlood);
            shaman.CultivationXP = 0;
            w.Shamans.ProcessYear();
            int fed = (int)System.Math.Round((CultivationSystem.BaseYearlyXp + shaman.SpiritualRoot / 2) * Settings.Speed * Settings.SacrificeSpeed);
            Assert.AreEqual(fed, shaman.CultivationXP);
            Assert.AreEqual(0, w.Resources.BloodEssence);
        }

        [Test]
        public void ASacrificeShaman_WithoutBlood_CultivatesAtThePlainPace()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Sacrifice);
            shaman.CultivationXP = 0;
            w.Shamans.ProcessYear();
            Assert.AreEqual((int)System.Math.Round((CultivationSystem.BaseYearlyXp + shaman.SpiritualRoot / 2) * Settings.Speed), shaman.CultivationXP);
        }

        [Test]
        public void AShaman_ClimbsWithoutTrial_UpToThePurpleMansion()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Transformation, CultivationRealm.Foundation, 4);
            shaman.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.Shamans.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, shaman.Realm);
        }

        [Test]
        public void AShaman_AttemptsNoImmortalTrial()
        {
            var w = World();
            var shaman = Shaman(w, ShamanMethod.Sacrifice, CultivationRealm.Foundation, 4);
            shaman.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            Assert.DoesNotThrow(() => w.Breakthroughs.ProcessBreakthroughPhase());
            Assert.DoesNotThrow(() => w.PurpleMansion.ProcessBreakthroughPhase());
        }

        // ---- The shamanic Golden Core ----

        [Test]
        public void ARiteOfBlood_GivesAShamanicGoldenCore()
        {
            var w = World(0.0);
            var shaman = Shaman(w, ShamanMethod.Sacrifice, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Resources.AddBlood(Settings.CoreBlood);
            Assert.IsNull(w.Shamans.CondenseCore(shaman));
            Assert.AreEqual(CultivationRealm.GoldenCore, shaman.Realm);
            Assert.AreEqual(GoldenCoreState.ShamanicEquivalent, shaman.GoldenCore);
            Assert.IsFalse(PowerLadder.IsTrueMonarch(shaman));
        }

        [Test]
        public void AFailedRite_SpendsTheBlood_AndWoundsTheDao()
        {
            var w = World(0.99);
            var shaman = Shaman(w, ShamanMethod.Sacrifice, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Resources.AddBlood(Settings.CoreBlood);
            Assert.IsNull(w.Shamans.CondenseCore(shaman));
            Assert.AreEqual(CultivationRealm.PurpleMansion, shaman.Realm);
            Assert.AreEqual(0, w.Resources.BloodEssence);
            Assert.AreEqual(1, shaman.DaoWounds);
        }

        [Test]
        public void TheRite_WaitsForTheGrandPerfection_AndTheBlood()
        {
            var w = World();
            var young = Shaman(w, ShamanMethod.Sacrifice, CultivationRealm.PurpleMansion, 1);
            w.Resources.AddBlood(Settings.CoreBlood);
            Assert.IsNotNull(w.Shamans.CoreRefusal(young));
            var ripe = Shaman(w, ShamanMethod.Sacrifice, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            w.Resources.ConsumeBlood(w.Resources.BloodEssence);
            Assert.IsNotNull(w.Shamans.CoreRefusal(ripe));
        }

        [Test]
        public void AShamanicGoldenCore_ReachesTheEndingOfTheOtherDaos()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var reached = new List<string>();
            s.Events.OnEndingReached += (ending, subject) => reached.Add(ending.Id);
            var shaman = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1);
            shaman.Path = CultivationPath.Shamanic;
            shaman.GoldenCore = GoldenCoreState.ShamanicEquivalent;
            s.Clan.AddMember(shaman);
            s.Events.TriggerYearStarted(s.Clock.Year);
            CollectionAssert.Contains(reached, "other-daos");
        }

        // ---- The refuge from the Emissaries of Death ----

        [Test]
        public void AFailedCore_OfTheGrandShaman_EscapesIntoTheShamanicDao()
        {
            var w = World(0.99);
            var master = GrandShamanFoundation(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion));
            master.DivineAbilities = new List<string>(FiveShaman);
            master.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            w.Knowledge.Reveal(FactKind.GoldSeeking, "grand-shaman", KnowledgeSource.Mirror);
            w.GoldenCore.Forge(master, "grand-shaman");
            Assert.IsTrue(master.IsAlive, "no demon born");
            Assert.AreEqual(CultivationPath.Shamanic, master.Path);
            Assert.AreEqual(ShamanMethod.Sacrifice, master.ShamanMethod);
        }

        [Test]
        public void AFailedCore_OfAnotherLineage_StillBecomesADemon()
        {
            var w = World(0.99);
            var master = w.Join(Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: PowerLadder.StageCount(CultivationRealm.PurpleMansion)));
            master.FoundationId = "orthodox-water:boundless-sea";
            master.DivineAbilities = new List<string>
            {
                "orthodox-water:boundless-sea", "orthodox-water:ford-watcher", "orthodox-water:storm-sky",
                "orthodox-water:dike-guard", "orthodox-water:river-farewell"
            };
            w.Knowledge.Reveal(FactKind.GoldSeeking, "orthodox-water", KnowledgeSource.Mirror);
            w.GoldenCore.Forge(master, "orthodox-water");
            Assert.IsFalse(master.IsAlive);
        }
    }
}
