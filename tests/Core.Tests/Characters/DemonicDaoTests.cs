using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
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

        private static CapturedBeast Captured(TestWorld w, CultivationRealm realm = CultivationRealm.QiRefinement, int stage = 3, string owner = null, string species = "mist-wolf")
        {
            var beast = new CapturedBeast($"{species}-region-1", realm, stage, owner);
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
            // ---- The demonic Golden Core (L7; the user's choice of 2026-10-15: awakening and offering) ----

        private static int Apogee => PowerLadder.StageCount(CultivationRealm.PurpleMansion);

        private static CharacterData AtTheApogee(TestWorld w, string species = "mist-wolf")
        {
            var beast = new CapturedBeast($"{species}-region-1", CultivationRealm.PurpleMansion, Apogee);
            w.Resources.AddBeast(beast);
            return w.DemonicDao.Bind(beast);
        }

        [Test]
        public void ItsLineage_MayAwakenADemonicCore_OnItsOwn_AtTheApogee()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.GoldenCore, bound.Realm);
            Assert.AreEqual(GoldenCoreState.DemonicEquivalent, bound.GoldenCore);
            Assert.AreEqual((int)(PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1) * Settings.LifespanFactor), bound.MaxLifespan);
        }

        [Test]
        public void ANaturalAwakening_ThatDoesNotCome_WoundsNothing()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var bound = AtTheApogee(w);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.AreEqual(0, bound.DaoWounds);
        }

        [Test]
        public void ItsLineage_IsRarelyAwakened_OnItsOwn()
        {
            var w = new TestWorld(new FixedRandom(Settings.NaturalCoreChance + 0.001));
            var bound = AtTheApogee(w);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.Less(Settings.NaturalCoreChance, Settings.OfferingCoreChance);
        }

        [Test]
        public void BelowTheApogee_NoCoreAwakens()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = w.DemonicDao.Bind(Captured(w, CultivationRealm.PurpleMansion, 1));
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.IsNotNull(w.DemonicDao.CoreRefusal(bound));
        }

        [Test]
        public void TheClansOffering_CostsStones_AndTriesTheAwakening()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            w.Resources.SetSpiritStones(Settings.OfferingStones);
            Assert.IsNull(w.DemonicDao.Offer(bound));
            Assert.AreEqual(0, w.Resources.SpiritStones);
            Assert.AreEqual(GoldenCoreState.DemonicEquivalent, bound.GoldenCore);
        }

        [Test]
        public void AFailedOffering_WoundsItsDao()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var bound = AtTheApogee(w);
            w.Resources.SetSpiritStones(Settings.OfferingStones);
            Assert.IsNull(w.DemonicDao.Offer(bound));
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.AreEqual(1, bound.DaoWounds);
        }

        [Test]
        public void TheOffering_WaitsForTheStones()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            w.Resources.SetSpiritStones(Settings.OfferingStones - 1);
            Assert.IsNotNull(w.DemonicDao.Offer(bound));
            Assert.AreEqual(CultivationRealm.PurpleMansion, bound.Realm);
            Assert.AreEqual(Settings.OfferingStones - 1, w.Resources.SpiritStones);
        }

        [Test]
        public void TheOffering_IsForABoundBeast_Only()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var human = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion, stage: Apogee));
            w.Resources.SetSpiritStones(Settings.OfferingStones);
            Assert.IsNotNull(w.DemonicDao.CoreRefusal(human));
        }

        [Test]
        public void ACapturedBeast_ReceivesNoOffering()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            bound.CaptorFaction = "Secte du Pic des Nuées";
            w.Resources.SetSpiritStones(Settings.OfferingStones);
            Assert.IsNotNull(w.DemonicDao.CoreRefusal(bound));
        }

        [Test]
        public void ADemonicCore_IsShownAsSuch()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            w.DemonicDao.ProcessYear();
            StringAssert.Contains("noyau", RankCatalog.DisplayName(bound).ToLowerInvariant());
        }

        [Test]
        public void ADemonicCore_ReachesTheEndingOfTheOtherDaos()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var reached = new List<string>();
            s.Events.OnEndingReached += (ending, subject) => reached.Add(ending.Id);
            var beast = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1);
            beast.Species = Species.SpiritBeast;
            beast.Path = CultivationPath.Demonic;
            beast.SubPath = CultivationSubPath.MyriadRaces;
            beast.GoldenCore = GoldenCoreState.DemonicEquivalent;
            s.Clan.AddMember(beast);
            s.Events.TriggerYearStarted(s.Clock.Year);
            CollectionAssert.Contains(reached, "other-daos");
        }

        [Test]
        public void TheDiplomacyScreen_ListsTheBeastsAtTheirApogee_AndTheirOffering()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var beast = new CapturedBeast("mist-wolf-region-1", CultivationRealm.PurpleMansion, Apogee);
            s.Resources.AddBeast(beast);
            var bound = s.DemonicDao.Bind(beast);
            var line = DiplomacyView.DemonicCores(s).Single(l => l.MemberId == bound.ID);
            StringAssert.Contains(Settings.OfferingStones.ToString(), line.Label);
        }

        // ---- The True Serpent-Dragon Dao (the user's choice of 2026-10-15: a hidden dragon blood) ----

        [Test]
        public void ASerpentOfTheDragonsBlood_MayCarryItHidden()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var serpent = w.DemonicDao.Bind(Captured(w, species: "jade-serpent"));
            Assert.IsTrue(serpent.DragonBlood);
            Assert.AreEqual(CultivationSubPath.MyriadRaces, serpent.SubPath, "hidden until an awakening");
            Assert.AreEqual(Species.SpiritBeast, serpent.Species);
        }

        [Test]
        public void ABeastOfAnotherKind_NeverCarriesTheDragonsBlood()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            Assert.IsFalse(w.DemonicDao.Bind(Captured(w)).DragonBlood);
            CollectionAssert.DoesNotContain(Settings.DragonBloodSpecies, "mist-wolf");
        }

        [Test]
        public void TheDragonsBlood_IsRevealed_AtTheNextAwakening()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var serpent = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 4, species: "jade-serpent"));
            serpent.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(CultivationRealm.PurpleMansion, serpent.Realm);
            Assert.AreEqual(CultivationSubPath.TrueSerpentDragon, serpent.SubPath);
            Assert.AreEqual(Species.Dragon, serpent.Species);
            StringAssert.Contains("serpent-dragon", RankCatalog.DisplayName(serpent).ToLowerInvariant());
        }

        [Test]
        public void ARevealedDragon_AbsorbsFaster()
        {
            var w = new TestWorld(new FixedRandom(0.99));
            var dragon = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            dragon.CultivationXP = 0;
            w.DemonicDao.ProcessYear();
            int expected = (int)System.Math.Round((CultivationSystem.BaseYearlyXp + dragon.SpiritualRoot / 2) * Settings.AbsorptionSpeed * Settings.DragonSpeed);
            Assert.AreEqual(expected, dragon.CultivationXP);
        }

        [Test]
        public void ARevealedDragon_AwakensItsCore_MoreEasily()
        {
            var w = new TestWorld(new FixedRandom(Settings.NaturalCoreChance + Settings.DragonCoreBonus / 2));
            var dragon = AtTheApogee(w);
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            w.DemonicDao.ProcessYear();
            Assert.AreEqual(GoldenCoreState.DemonicEquivalent, dragon.GoldenCore);
        }

        [Test]
        public void TheDragonsOfTheEasternSea_ReclaimTheirBlood_FromAClanWithoutATrueMonarch()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var dragon = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            CharacterData taken = null;
            w.Ctx.Events.OnDragonReclaimed += d => taken = d;
            w.DemonicDao.ProcessYear();
            Assert.AreSame(dragon, taken);
            CollectionAssert.DoesNotContain(w.Clan.LivingMembers, dragon);
        }

        [Test]
        public void ATrueMonarchOfTheClan_KeepsTheDragonsAway()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Join(Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1));
            var dragon = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            bool repelled = false;
            w.Ctx.Events.OnDragonsRepelled += d => repelled = true;
            w.DemonicDao.ProcessYear();
            CollectionAssert.Contains(w.Clan.LivingMembers, dragon);
            Assert.IsTrue(repelled);
        }

        [Test]
        public void AMonkAtTheGoldenCore_IsNoTrueMonarch_ToTheDragons()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var master = w.Join(Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore, stage: 1));
            master.Path = CultivationPath.Buddhist;
            master.SubPath = CultivationSubPath.AncientBuddhism;
            master.GoldenCore = GoldenCoreState.DharmaMaster;
            var dragon = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1));
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            w.DemonicDao.ProcessYear();
            CollectionAssert.DoesNotContain(w.Clan.LivingMembers, dragon);
        }

        [Test]
        public void ASealedBeast_ReceivesNoOffering()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var bound = AtTheApogee(w);
            bound.ProgressionSealed = true;
            w.Resources.SetSpiritStones(Settings.OfferingStones);
            Assert.IsNotNull(w.DemonicDao.CoreRefusal(bound));
        }

        [Test]
        public void ADragonWithItsOwnCore_ChoosesItsClan()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var dragon = AtTheApogee(w);
            dragon.SubPath = CultivationSubPath.TrueSerpentDragon;
            dragon.Species = Species.Dragon;
            dragon.Realm = CultivationRealm.GoldenCore;
            dragon.RealmStage = 1;
            dragon.GoldenCore = GoldenCoreState.DemonicEquivalent;
            w.DemonicDao.ProcessYear();
            CollectionAssert.Contains(w.Clan.LivingMembers, dragon);
        }

        [Test]
        public void AHiddenBlood_IsNotYetReclaimed()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var serpent = w.DemonicDao.Bind(Captured(w, CultivationRealm.Foundation, 1, species: "jade-serpent"));
            w.DemonicDao.ProcessYear();
            CollectionAssert.Contains(w.Clan.LivingMembers, serpent);
        }

        [Test]
        public void TheHiddenBlood_IsKept_AcrossASave()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var beast = new CapturedBeast("jade-serpent-region-1", CultivationRealm.QiRefinement, 2);
            s.Resources.AddBeast(beast);
            var serpent = s.DemonicDao.Bind(beast);
            serpent.DragonBlood = true;
            var loaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Fixtures.QuietContent });
            Assert.IsTrue(loaded.Clan.FindById(serpent.ID).DragonBlood);
        }
    }
}
