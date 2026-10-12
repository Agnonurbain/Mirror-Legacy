using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Demonic Dao of the clan's bound beasts (R16; LORE.md §3.4; the user's choices of 2026-10-12): a captured beast is
    /// bound to the clan rather than offered to the mirror; it absorbs the Qi around it every year, whatever its task, slower
    /// than a human but for a longer life; its lineage awakens its realms on its own, up to the Purple Mansion; it guards and
    /// hunts, never weds, never leads the clan, and does not carry the line.
    /// </summary>
    [TestFixture]
    public class DemonicDaoTests
    {
        private static DemonicDaoSettings Settings => Fixtures.Content.Balance.DemonicDao;

        private static CapturedBeast Captured(TestWorld w, CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 3, string owner = null)
        {
            var beast = new CapturedBeast("mist-wolf-region-1", realm, stage, owner);
            w.Resources.AddBeast(beast);
            return beast;
        }

        // ---- Binding ----

        [Test]
        public void ABoundBeast_JoinsTheClan_AsADemonicCultivatorOfItsRealm()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 2));
            Assert.IsNotNull(bound);
            Assert.IsTrue(w.Clan.LivingMembers.Contains(bound));
            Assert.AreEqual(Species.SpiritBeast, bound.Species);
            Assert.AreEqual(CultivationPath.Demonic, bound.Path);
            Assert.AreEqual(CultivationSubPath.MyriadRaces, bound.SubPath);
            Assert.AreEqual(CultivationRealm.Foundation, bound.Realm);
            Assert.AreEqual(2, bound.RealmStage);
        }

        [Test]
        public void ABoundBeast_TakesItsSpeciesNameAndElement()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w));
            var species = Fixtures.Content.BeastSpecies.Single(s => s.Id == "mist-wolf");
            StringAssert.Contains(species.Name, bound.FullName);
            Assert.AreEqual(species.Element, bound.Affinity);
        }

        [Test]
        public void ABoundBeast_LeavesTheMirrorsOfferings()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var beast = Captured(w);
            w.DemonicDao.Bind(beast);
            CollectionAssert.DoesNotContain(w.Resources.Beasts, beast);
        }

        [Test]
        public void ABoundBeast_LivesLongerThanAHumanOfItsRealm()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            Assert.AreEqual((int)(PowerLadder.MaxLifespan(CultivationRealm.Foundation, 1) * Settings.LifespanFactor), bound.MaxLifespan);
            Assert.Less(bound.Age, bound.MaxLifespan);
        }

        [Test]
        public void ABeastTheClanDoesNotHold_IsNotBound()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            Assert.IsNull(w.DemonicDao.Bind(new CapturedBeast("mist-wolf-region-1", CultivationRealm.QiRefinement, 1)));
            Assert.AreEqual(0, w.Clan.LivingMembers.Count);
        }

        // ---- Absorption and the lineage's awakening ----

        [Test]
        public void ABoundBeast_AbsorbsQiEveryYear_WhateverItsTask()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            bound.CurrentTask = TaskType.Patrol;
            bound.CultivationXP = 0;
            w.DemonicDao.ProcessYear();
            int expected = (int)System.Math.Round((CultivationSystem.BaseYearlyXp + bound.SpiritualRoot / 2) * Settings.AbsorptionSpeed);
            Assert.AreEqual(expected, bound.CultivationXP);
        }

        [Test]
        public void AHuman_DoesNotAbsorbAsABeast()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var human = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            human.CurrentTask = TaskType.Patrol;
            human.CultivationXP = 0;
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(0, human.CultivationXP);
        }

        [Test]
        public void ABoundBeast_ClimbsItsStages_WithoutATrial()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.QiRefinement, 3));
            bound.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.QiRefinement);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(4, bound.RealmStage);
        }

        [Test]
        public void ItsLineage_AwakensTheNextRealm_AtTheApogee()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 4));
            bound.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.AreEqual(1, bound.RealmStage);
            Assert.AreEqual((int)(PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, 1) * Settings.LifespanFactor), bound.MaxLifespan);
        }

        [Test]
        public void ABoundBeast_StopsAtThePurpleMansionsGrandPerfection()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion)));
            bound.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion) * 3;
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.AreEqual(PowerLadder.StageCount(CultivationRealm.PurpleMansion), bound.RealmStage);
        }

        [Test]
        public void ABoundBeast_CondensesNoDivineAbility()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.PurpleMansion, 1));
            bound.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion) * 3;
            w.Abilities.ProcessBreakthroughPhase();
            Assert.IsEmpty(bound.DivineAbilities);
            Assert.IsNull(bound.PursuedAbility);
        }

        [Test]
        public void ACapturedBeast_AbsorbsNothing()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w));
            bound.CaptorFaction = "Secte du Pic des Nuées";
            bound.CultivationXP = 0;
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(0, bound.CultivationXP);
        }

        // ---- Its place in the clan ----

        [Test]
        public void ABoundBeast_GuardsAndHunts_AndNothingElse()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w));
            var allowed = TaskRules.AllowedTasks(bound, huntOpen: true, lakeOpen: true);
            CollectionAssert.IsSubsetOf(allowed, new[] { TaskType.None, TaskType.Patrol, TaskType.Rest, TaskType.HuntBeast, TaskType.ScoutBeasts, TaskType.Diversion });
            CollectionAssert.Contains(allowed, TaskType.Patrol);
            CollectionAssert.Contains(allowed, TaskType.HuntBeast);
            CollectionAssert.DoesNotContain(allowed, TaskType.Cultivation);
            CollectionAssert.DoesNotContain(allowed, TaskType.Diplomacy);
        }

        [Test]
        public void ABoundBeast_StartsOnPatrol()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            Assert.AreEqual(TaskType.Patrol, w.DemonicDao.Bind(Captured(w)).CurrentTask);
        }

        [Test]
        public void ABoundBeast_NeverWeds()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w));
            Assert.IsFalse(MarriageMatchmaker.IsEligible(bound));
        }

        [Test]
        public void ABoundBeast_CannotBeWedByTheClan_NorSoughtASpouse()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w));
            var human = w.Join(Fixtures.Cultivator(isMale: !bound.IsMale));
            Assert.IsNotNull(w.Marriages.MarriageRefusal(bound, human));
            Assert.IsNotNull(w.Marriages.SeekRefusal(bound));
        }

        [Test]
        public void ABoundBeast_AtTheFoundationsApogee_AttemptsNoAscent()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 4));
            bound.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            Assert.DoesNotThrow(() => w.Breakthroughs.ProcessBreakthroughPhase());
            Assert.DoesNotThrow(() => w.PurpleMansion.ProcessBreakthroughPhase());
            Assert.AreEqual(CultivationRealm.Foundation, bound.Realm);
        }

        [Test]
        public void ABoundBeast_KeepsItsLongLife_AcrossASave()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.QiRefinement, 2));
            int lifespan = bound.MaxLifespan;
            PowerLadder.NormalizeLifespan(bound); // as every record on loading
            Assert.AreEqual(lifespan, bound.MaxLifespan);
        }

        [Test]
        public void ABoundBeast_IsBoundTheSameWay_FromTheSameSeed()
        {
            var a = new TestWorld(7);
            var b = new TestWorld(7);
            Assert.AreEqual(a.DemonicDao.Bind(Captured(a)).ID, b.DemonicDao.Bind(Captured(b)).ID);
        }

        [Test]
        public void ABoundBeast_NeverLeadsTheClan_WhileAHumanLives()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var patriarch = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 1));
            var heir = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));
            w.Clan.AppointPatriarch(patriarch);
            w.DemonicDao.Bind(Captured(w, CultivationRealm.PurpleMansion, 2));
            w.Clan.Kill(patriarch, DeathCause.OldAge);
            Assert.AreSame(heir, w.Clan.GetPatriarch());
        }

        [Test]
        public void TheLine_DiesOut_WhenOnlyBoundBeastsRemain()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var victory = new VictoryConditionSystem(w.Ctx, w.Clan);
            var last = w.Join(Fixtures.Cultivator());
            w.DemonicDao.Bind(Captured(w));
            w.Clan.Kill(last, DeathCause.OldAge);
            Assert.IsTrue(victory.GameLost);
        }

        [Test]
        public void ABoundBeast_IsShownAsADemonicCultivator()
        {
            var w = new TestWorld(new FixedRandom(0.5));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 2));
            StringAssert.Contains("démoniaque", RankCatalog.DisplayName(bound).ToLowerInvariant());
        }
    }
}
