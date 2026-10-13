using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Heavenly Embryo Devil Dao (R12; LORE.md §3.2; the wiki « Cultivation System »; the user's choices of 2026-10-13):
    /// after the six chakras a member forms a Diverse Mansion instead of entering Qi Cultivation — no method nor Qi, but blood
    /// essence; every realm it passes thirsts for blood; its two ascents become one, a single trial to the Purple Mansion. Blood
    /// comes from beasts bled and foes struck down beside a devil; a devil found out earns the orthodox powers' distrust.
    /// </summary>
    [TestFixture]
    public class DevilDaoTests
    {
        private const string Sea = "orthodox-water:boundless-sea";
        private const string Farewell = "orthodox-water:river-farewell";

        private static DevilDaoSettings Settings => Fixtures.Content.Balance.DevilDao;

        private static TestWorld World(double sample = 0.0)
        {
            var w = new TestWorld(new FixedRandom(sample));
            w.Factions.InitializeFactions();
            return w;
        }

        /// <summary>A breathing member at its sixth chakra, the last before Qi Cultivation.</summary>
        private static CharacterData SixthChakra(TestWorld w)
        {
            var c = w.Join(Fixtures.Cultivator(age: 18, realm: CultivationRealm.Embryonic, stage: 6));
            c.Affinity = Element.Water;
            return c;
        }

        /// <summary>A devil of the clan at the realm and stage, cultivating.</summary>
        private static CharacterData Devil(TestWorld w, CultivationRealm realm, int stage)
        {
            var c = SixthChakra(w);
            w.Resources.AddBlood(Settings.FormBlood);
            Assert.IsNull(w.Devils.FormDiverseMansion(c));
            c.Realm = realm;
            c.RealmStage = stage;
            c.CurrentTask = TaskType.Cultivation;
            c.MaxLifespan = c.Age + 300;
            return c;
        }

        // ---- The Diverse Mansion ----

        [Test]
        public void AMemberAtTheSixthChakra_FormsADiverseMansion_WithBlood()
        {
            var w = World();
            var member = SixthChakra(w);
            w.Resources.AddBlood(Settings.FormBlood);
            Assert.IsNull(w.Devils.FormDiverseMansion(member));
            Assert.AreEqual(CultivationPath.Devil, member.Path);
            Assert.AreEqual(CultivationSubPath.HeavenlyEmbryoDemon, member.SubPath);
            Assert.AreEqual(CultivationRealm.QiRefinement, member.Realm);
            Assert.AreEqual(1, member.RealmStage);
            Assert.AreEqual(0, w.Resources.BloodEssence);
            Assert.AreEqual("Manoir Divers", RankCatalog.DisplayName(member));
        }

        [Test]
        public void NoDiverseMansion_WithoutTheBlood()
        {
            var w = World();
            var member = SixthChakra(w);
            w.Resources.AddBlood(Settings.FormBlood - 1);
            Assert.IsNotNull(w.Devils.FormDiverseMansion(member));
            Assert.AreEqual(CultivationPath.Immortal, member.Path);
        }

        [Test]
        public void NoDiverseMansion_BeforeTheSixthChakra_NorOnceInQiCultivation()
        {
            var w = World();
            w.Resources.AddBlood(Settings.FormBlood * 2);
            Assert.IsNotNull(w.Devils.FormRefusal(w.Join(Fixtures.Cultivator(age: 15, realm: CultivationRealm.Embryonic, stage: 5))));
            Assert.IsNotNull(w.Devils.FormRefusal(w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1))));
        }

        [Test]
        public void AnElect_DoesNotTurnDevil()
        {
            var w = World();
            var elect = SixthChakra(w);
            elect.SubPath = CultivationSubPath.SpiritualNature;
            w.Resources.AddBlood(Settings.FormBlood);
            Assert.IsNotNull(w.Devils.FormRefusal(elect));
        }

        // ---- Cultivating, and its thirst ----

        [Test]
        public void ADevil_Cultivates_AtItsPace()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.QiRefinement, 2);
            devil.CultivationXP = 0;
            w.Devils.ProcessYear();
            Assert.AreEqual((int)System.Math.Round((CultivationSystem.BaseYearlyXp + devil.SpiritualRoot / 2) * Settings.Speed), devil.CultivationXP);
        }

        [Test]
        public void ADevil_ClimbsItsStages_WithoutBlood()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.QiRefinement, 3);
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Devils.ProcessYear();
            Assert.AreEqual(4, devil.RealmStage);
        }

        [Test]
        public void TheUnifiedFurnace_ThirstsForBlood()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.QiRefinement, 9);
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Devils.ProcessYear();
            Assert.AreEqual(CultivationRealm.QiRefinement, devil.Realm, "no blood: it waits");
        }

        [Test]
        public void TheUnifiedFurnace_IsFormedWithBlood_OnAFoundationOfItsElement()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.QiRefinement, 9);
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.Resources.AddBlood(Settings.FurnaceBlood);
            w.Devils.ProcessYear();
            Assert.AreEqual(CultivationRealm.Foundation, devil.Realm);
            Assert.AreEqual("Fournaise Unifiée", RankCatalog.DisplayName(devil));
            Assert.AreEqual(0, w.Resources.BloodEssence);
            Assert.IsNotNull(devil.FoundationId);
            var lineage = FoundationRef.Parse(devil.FoundationId).FruitionId;
            Assert.AreEqual(Element.Water, Fixtures.Content.Fruitions.Single(f => f.Id == lineage).Element);
        }

        [Test]
        public void TheSingleAscent_ReachesThePurpleMansion()
        {
            var w = World(0.0);
            var devil = Devil(w, CultivationRealm.Foundation, 4);
            devil.FoundationId = Sea;
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.Resources.AddBlood(Settings.MansionBlood);
            w.Devils.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, devil.Realm);
            Assert.GreaterOrEqual(devil.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, 1));
        }

        [Test]
        public void AFailedAscent_WoundsTheDao_AndSpendsTheBlood()
        {
            var w = World(0.99);
            var devil = Devil(w, CultivationRealm.Foundation, 4);
            devil.FoundationId = Sea;
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.Resources.AddBlood(Settings.MansionBlood);
            w.Devils.ProcessYear();
            Assert.AreEqual(CultivationRealm.Foundation, devil.Realm);
            Assert.AreEqual(1, devil.DaoWounds);
            Assert.AreEqual(0, w.Resources.BloodEssence);
        }

        [Test]
        public void ADevil_AttemptsNoImmortalTrial()
        {
            var w = World(0.0);
            var devil = Devil(w, CultivationRealm.Foundation, 4);
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            Assert.DoesNotThrow(() => w.Breakthroughs.ProcessBreakthroughPhase());
            Assert.DoesNotThrow(() => w.PurpleMansion.ProcessBreakthroughPhase());
            Assert.AreEqual(CultivationRealm.Foundation, devil.Realm);
        }

        [Test]
        public void APurpleMansionDevil_CondensesItsAbilities()
        {
            var w = World(0.0);
            w.Techniques.Learn("upstream-brook-method"); // its Qi builds the River Farewell
            w.Resources.AddQi("upstream-brook-qi", 1);
            var devil = Devil(w, CultivationRealm.PurpleMansion, 1);
            devil.FoundationId = Sea;
            devil.DivineAbilities = new List<string> { Sea };
            Assert.IsTrue(w.Abilities.Pursue(devil, Farewell));
            devil.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);
            w.Abilities.ProcessBreakthroughPhase();
            CollectionAssert.Contains(devil.DivineAbilities, Farewell);
        }

        [Test]
        public void AGoldenCoreDevil_IsATrueMonarch()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.GoldenCore, 1);
            Assert.IsTrue(PowerLadder.IsTrueMonarch(devil));
        }

        // ---- Blood ----

        [Test]
        public void ABledBeast_GivesBloodByItsRealm()
        {
            var w = World();
            var beast = new CapturedBeast("mist-wolf-region-1", CultivationRealm.Foundation, 2);
            w.Resources.AddBeast(beast);
            Assert.IsNull(w.Devils.Bleed(beast));
            Assert.AreEqual(Settings.BloodByRealm[CultivationRealm.Foundation], w.Resources.BloodEssence);
            CollectionAssert.DoesNotContain(w.Resources.Beasts, beast);
        }

        [Test]
        public void ABeastTheClanDoesNotHold_IsNotBled()
        {
            var w = World();
            Assert.IsNotNull(w.Devils.Bleed(new CapturedBeast("mist-wolf-region-1", CultivationRealm.Foundation, 2)));
            Assert.AreEqual(0, w.Resources.BloodEssence);
        }

        [Test]
        public void FoesStruckDownBesideADevil_GiveBlood()
        {
            var w = World();
            var devil = Devil(w, CultivationRealm.QiRefinement, 3);
            w.Ctx.Events.TriggerClanBattleEnded(new[] { devil }, 3);
            Assert.AreEqual(3 * Settings.BloodPerKill, w.Resources.BloodEssence);
        }

        [Test]
        public void FoesStruckDown_WithoutADevil_GiveNoBlood()
        {
            var w = World();
            var immortal = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerClanBattleEnded(new[] { immortal }, 3);
            Assert.AreEqual(0, w.Resources.BloodEssence);
        }

        [Test]
        public void TheBlood_SurvivesASave()
        {
            var s = GameSession.NewGame(Fixtures.VeteranSetup(4));
            s.Resources.AddBlood(7);
            Assert.AreEqual(7, GameSession.FromSaveData(s.ToSaveData(), Fixtures.VeteranSetup(4)).Resources.BloodEssence);
        }

        // ---- Found out ----

        [Test]
        public void ADevilFoundOut_EarnsTheOrthodoxPowersDistrust()
        {
            var w = World(0.0);
            Devil(w, CultivationRealm.QiRefinement, 2);
            var orthodox = w.Factions.Factions.First(f => f.Path == CultivationPath.Immortal);
            int relation = orthodox.RelationWithPlayer;
            w.Devils.ProcessYear();
            Assert.AreEqual(relation - Settings.DiscoveryDistrust, orthodox.RelationWithPlayer);
        }

        [Test]
        public void AClanWithoutDevils_IsNeverFoundOut()
        {
            var w = World(0.0);
            w.Join(Fixtures.Cultivator());
            var orthodox = w.Factions.Factions.First(f => f.Path == CultivationPath.Immortal);
            int relation = orthodox.RelationWithPlayer;
            w.Devils.ProcessYear();
            Assert.AreEqual(relation, orthodox.RelationWithPlayer);
        }
    }
}
