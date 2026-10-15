using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The lore audit's basic cultivation (AUDIT_LORE.md §7, 2026-10-15): an orifice nobody examined does not cultivate; only a
    /// free examiner at the domain examines; a breathing cultivator does at least what a mortal does; a spouse comes with a method
    /// and its Qi.
    /// </summary>
    [TestFixture]
    public class BasicCultivationAuditTests
    {
        private static TestWorld World(double sample = 0.99) => new TestWorld(new FixedRandom(sample));

        private static CharacterData Breathing(int age, int chakra) =>
            Fixtures.Cultivator(age: age, realm: CultivationRealm.Embryonic, stage: chakra);

        // ---- 7.1 An orifice nobody examined ----

        [Test]
        public void AnUnexaminedOrifice_DoesNotCultivate()
        {
            var w = World();
            var child = w.Join(Breathing(age: 10, chakra: 0));
            child.OrificeKnown = false;
            child.CurrentTask = TaskType.Cultivation;
            CollectionAssert.DoesNotContain(TaskRules.AllowedTasks(child), TaskType.Cultivation, "the task list must not betray it");
            w.Cultivation.ProcessYearlyCultivation(child, 1.0);
            Assert.AreEqual(0, child.CultivationXP);
        }

        [Test]
        public void ATalismanSeed_Cultivates_ThoughNobodyExaminedIt()
        {
            var seeded = Breathing(age: 20, chakra: 1);
            seeded.HasSpiritualOrifice = false;
            seeded.HasTalismanSeed = true;
            seeded.OrificeKnown = false;
            CollectionAssert.Contains(TaskRules.AllowedTasks(seeded), TaskType.Cultivation);
        }

        // ---- 7.2 The examiner ----

        private static (TestWorld w, CharacterData examiner, CharacterData newborn) Examination()
        {
            var w = World();
            var examiner = w.Join(Fixtures.Cultivator(age: 40, realm: CultivationRealm.QiRefinement, stage: 2));
            var newborn = w.Join(Breathing(age: 1, chakra: 0));
            newborn.OrificeKnown = false;
            return (w, examiner, newborn);
        }

        [Test]
        public void AFreeExaminerAtTheDomain_ExaminesTheNewborns()
        {
            var (w, _, newborn) = Examination();
            Assert.AreEqual(1, w.Clan.ExamineOrifices());
            Assert.IsTrue(newborn.OrificeKnown);
        }

        [Test]
        public void ACaptiveExaminer_ExaminesNobody()
        {
            var (w, examiner, newborn) = Examination();
            examiner.CaptorFaction = "Secte du Pic des Nuées";
            Assert.AreEqual(0, w.Clan.ExamineOrifices());
            Assert.IsFalse(newborn.OrificeKnown);
        }

        [Test]
        public void AnExaminerAway_ExaminesNobody()
        {
            var (w, examiner, newborn) = Examination();
            examiner.CurrentTask = TaskType.Diplomacy;
            Assert.AreEqual(0, w.Clan.ExamineOrifices());
            Assert.IsFalse(newborn.OrificeKnown);
        }

        [Test]
        public void AnExaminerInRetreat_ExaminesNobody()
        {
            var (w, examiner, newborn) = Examination();
            examiner.Retreat = Retreat.Manifestation;
            Assert.AreEqual(0, w.Clan.ExamineOrifices());
            Assert.IsFalse(newborn.OrificeKnown);
        }

        // ---- 7.1 Nothing else betrays an orifice nobody examined (review) ----

        private static CharacterData Unexamined(TestWorld w)
        {
            var hidden = w.Join(Breathing(age: 20, chakra: 0));
            hidden.OrificeKnown = false;
            return hidden;
        }

        [Test]
        public void AnUnexaminedOrifice_CostsWhatAMortalCosts()
        {
            var w = World();
            Assert.AreEqual(w.Upkeep.UpkeepOf(w.Join(Fixtures.Mortal())), w.Upkeep.UpkeepOf(Unexamined(w)));
        }

        [Test]
        public void AnUnexaminedOrifice_TakesUpNoMethod()
        {
            var w = World();
            var breathing = Fixtures.Content.Techniques.First(t => t.Kind == TechniqueKind.Cultivation && TechniqueRules.Covers(t, CultivationRealm.Embryonic));
            Assert.IsFalse(TechniqueRules.CanPractise(Unexamined(w), breathing));
        }

        [Test]
        public void AnUnexaminedEnvoy_IsReceivedAsAMortal()
        {
            var w = World();
            w.Factions.InitializeFactions();
            var family = w.Factions.Factions.First(f => f.Kind == FactionKind.Family);
            var mortal = w.Join(Fixtures.Mortal());
            var hidden = Unexamined(w);
            foreach (var envoy in new[] { mortal, hidden }) { envoy.CurrentTask = TaskType.Diplomacy; envoy.DiplomacyTarget = family.Name; }
            int before = family.RelationWithPlayer;
            mortal.CurrentTask = TaskType.None;
            w.Tasks.ProcessYearlyTasks();
            int byTheHidden = family.RelationWithPlayer - before;
            hidden.CurrentTask = TaskType.None;
            mortal.CurrentTask = TaskType.Diplomacy;
            before = family.RelationWithPlayer;
            w.Tasks.ProcessYearlyTasks();
            Assert.AreEqual(family.RelationWithPlayer - before, byTheHidden);
        }

        // ---- 7.5-7.6 Equip (review) ----

        [Test]
        public void ANewcomer_NeverPractisesAMethodOfAVanishedQi()
        {
            var w = World();
            var member = Fixtures.Cultivator(age: 30);
            member.CultivationMethodId = null;
            member.QiId = null;
            w.Techniques.Equip(member, new[] { "elder-knocking-sutra" });
            Assert.AreNotEqual("elder-knocking-sutra", member.CultivationMethodId);
        }

        // ---- 7.4 The breathing cultivator's tasks ----

        [Test]
        public void ABreathingAdult_DoesWhatAMortalDoes()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 20, chakra: 1));
            CollectionAssert.IsSubsetOf(TaskRules.AllowedTasks(Fixtures.Mortal(age: 20)), tasks);
            CollectionAssert.Contains(tasks, TaskType.Cultivation);
        }

        [Test]
        public void ABreathingCultivator_Spies_FromTheThirdChakra()
        {
            CollectionAssert.DoesNotContain(TaskRules.AllowedTasks(Breathing(age: 20, chakra: 2)), TaskType.Espionage);
            CollectionAssert.Contains(TaskRules.AllowedTasks(Breathing(age: 20, chakra: 3)), TaskType.Espionage);
        }

        [Test]
        public void ABreathingChild_StillOnlyCultivatesAndRests()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 10, chakra: 3));
            CollectionAssert.DoesNotContain(tasks, TaskType.Mine);
            CollectionAssert.DoesNotContain(tasks, TaskType.Espionage);
            CollectionAssert.Contains(tasks, TaskType.Cultivation);
        }

        [Test]
        public void TheSummitEye_StillGathersQi_AndDoesTheRest()
        {
            var tasks = TaskRules.AllowedTasks(Breathing(age: 20, chakra: TaskRules.QiPerceptionChakra));
            CollectionAssert.Contains(tasks, TaskType.GatherQi);
            CollectionAssert.Contains(tasks, TaskType.Espionage);
            CollectionAssert.Contains(tasks, TaskType.Mine);
        }

        // ---- 7.5 A spouse comes with its method ----

        [Test]
        public void ASpouseOfAPower_PractisesAMethodOfThatPower_OnItsQi()
        {
            var w = World();
            w.Factions.InitializeFactions();
            var power = w.Factions.Factions.First(f => f.FamilyName != null
                && f.Techniques.Select(id => Fixtures.Content.Techniques.FirstOrDefault(t => t.ID == id))
                    .Any(t => t != null && TechniqueRules.Covers(t, CultivationRealm.QiRefinement)));
            var member = w.Join(Fixtures.Cultivator(age: 25));
            Assert.IsTrue(w.Marriages.HandleArrangedMarriage(member, power.ID, isForced: false));
            var spouse = w.Clan.FindById(member.SpouseID);
            CollectionAssert.Contains(power.Techniques, spouse.CultivationMethodId);
            var method = Fixtures.Content.Techniques.Single(t => t.ID == spouse.CultivationMethodId);
            Assert.AreEqual(method.RequiredQiId, spouse.QiId);
            Assert.IsTrue(TechniqueRules.Covers(method, spouse.Realm));
        }

        [Test]
        public void AWanderingSpouse_PractisesTheClansMethod()
        {
            var w = World(0.0);
            w.Techniques.Learn(Fixtures.ClanMethod);
            w.Resources.AddSpiritStones(10000);
            var member = w.Join(Fixtures.Cultivator(age: 25));
            Assert.IsNull(w.Marriages.SeekCultivatorSpouse(member.ID));
            var spouse = w.Clan.FindById(member.SpouseID);
            Assert.AreEqual(Fixtures.ClanMethod, spouse.CultivationMethodId);
            Assert.AreEqual(Fixtures.ClanQi, spouse.QiId);
        }
            // ---- 7.7 The Dao Partners: the four others of the centre (the user's choice of 2026-10-15) ----

        private static FruitionDefinition Lineage(string id) => Fixtures.Content.Fruitions.Single(f => f.Id == id);

        [Test]
        public void TheDaoPartners_LeaveOutTheSubstitutes()
        {
            var lineage = Fixtures.Content.Fruitions.First(f => f.Abilities.Any(a => a.Substitute));
            var foundation = lineage.Abilities.First(a => !a.Substitute);
            CollectionAssert.IsEmpty(FoundationRules.DaoPartners(lineage, foundation.Id).Where(a => a.Substitute));
        }

        [Test]
        public void ALineageOfSeven_GivesFourPartners_TheOthersOfItsCentre()
        {
            var water = Lineage("orthodox-water");
            var partners = FoundationRules.DaoPartners(water, "boundless-sea");
            Assert.AreEqual(4, partners.Count);
            CollectionAssert.DoesNotContain(partners.Select(a => a.Id), "peril-refuge");
            CollectionAssert.DoesNotContain(partners.Select(a => a.Id), "black-water-serpent-king");
        }

        [TestCase("orthodox-water")]
        [TestCase("orthodox-wood")]
        [TestCase("celestial-regent")]
        [TestCase("violet-qi")]
        [TestCase("supreme-yang")]
        public void EveryLineage_HasACentreOfFive(string lineageId)
        {
            Assert.AreEqual(5, Lineage(lineageId).Abilities.Count(a => !a.Substitute && !a.Peripheral));
        }

        [Test]
        public void KnowingOnesPartners_RevealsTheFourOfTheCentre()
        {
            var w = World();
            w.Knowledge.Reveal(MirrorChronicles.World.FactKind.DaoPartners, "orthodox-water:boundless-sea", MirrorChronicles.World.KnowledgeSource.Mirror);
            Assert.IsFalse(w.Knowledge.Knows(MirrorChronicles.World.FactKind.Ability, "orthodox-water:peril-refuge"));
            Assert.IsTrue(w.Knowledge.Knows(MirrorChronicles.World.FactKind.Ability, "orthodox-water:ford-watcher"));
        }

        [Test]
        public void APeripheralFoundation_IsNoPartnerToConsume()
        {
            var w = World();
            var consumer = w.Join(Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation, stage: 2));
            consumer.FoundationId = "orthodox-water:boundless-sea";
            var donor = w.Join(Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation, stage: 2));
            donor.FoundationId = "orthodox-water:peril-refuge";
            w.Knowledge.Reveal(MirrorChronicles.World.FactKind.DaoPartners, consumer.FoundationId, MirrorChronicles.World.KnowledgeSource.Mirror);
            Assert.IsFalse(w.Foundations.ConsumeDaoPartner(consumer, donor));
        }

        // ---- 7.12 The transformed lineage (the user's choice of 2026-10-15) ----

        private static CharacterData Realized(bool isMale)
        {
            var holder = Fixtures.Cultivator(isMale: isMale, age: 300, realm: CultivationRealm.GoldenCore, stage: 1);
            holder.GoldenCore = GoldenCoreState.Realization;
            return holder;
        }

        [Test]
        public void ARealizationsChild_IsAlwaysBornWithAnOrifice()
        {
            var w = World(0.999); // no orifice by the usual odds
            var child = w.Clan.GenerateChild(w.Join(Realized(true)), w.Join(Fixtures.Mortal(isMale: false)));
            Assert.IsTrue(child.TransformedLineage);
            Assert.IsTrue(child.HasSpiritualOrifice);
        }

        [Test]
        public void ATransformedLineage_PassesEveryTrial_UpToThePurpleMansion()
        {
            var w = World();
            var heir = w.Join(Breathing(age: 20, chakra: 2));
            heir.TransformedLineage = true;
            heir.MentalStability = 10; // even a troubled mind
            Assert.AreEqual(100, w.Breakthroughs.TrialSuccessRate(heir));
            var mansion = w.Join(Fixtures.Cultivator(age: 80, realm: CultivationRealm.Foundation, stage: 4));
            mansion.TransformedLineage = true;
            Assert.AreEqual(100, w.PurpleMansion.AscentChanceOf(mansion));
        }

        [Test]
        public void ATransformedLineage_GetsNothingSureBeyond()
        {
            var heir = Fixtures.Cultivator(age: 80, realm: CultivationRealm.PurpleMansion, stage: 1);
            heir.TransformedLineage = true;
            Assert.Less(PurpleMansionRules.IllusionsChance(heir, Fixtures.Content), 100);
        }

        // ---- 7.10 Stones buy only a common manual of grade 3 or less ----

        private static TestWorld Market()
        {
            var w = World();
            w.Factions.InitializeFactions();
            foreach (var f in w.Factions.Factions) w.Factions.ChangeRelation(f.ID, 100);
            w.Resources.AddSpiritStones(100000);
            return w;
        }

        [Test]
        public void AManualOfGradeFour_IsNotSoldForStones()
        {
            var w = Market();
            Assert.IsNotNull(w.Exchange.PurchaseRefusal("Famille Zang", "bronze-rampart-method"));
            Assert.IsFalse(w.Exchange.BuyTechnique("Famille Zang", "bronze-rampart-method"));
        }

        [Test]
        public void ASecretTechnique_IsNotSoldForStones()
        {
            var w = Market();
            Assert.IsFalse(w.Exchange.BuyTechnique("Famille Fang", "path-watcher"));
        }

        [Test]
        public void ACommonManualOfGradeThree_IsStillSold()
        {
            var w = Market();
            Assert.IsTrue(w.Exchange.BuyTechnique("Famille Ruan", "upstream-brook-method"));
        }

        // ---- 7.3 No mana below the first chakra ----

        [Test]
        public void AMortal_FightsWithoutQi()
        {
            Assert.AreEqual(0, new CombatUnit(Fixtures.Mortal(), isAlly: true).MaxQi);
        }

        [Test]
        public void TheMana_ComesWithTheFirstChakra()
        {
            Assert.AreEqual(0, new CombatUnit(Breathing(age: 20, chakra: 0), isAlly: true).MaxQi);
            Assert.Greater(new CombatUnit(Breathing(age: 20, chakra: 1), isAlly: true).MaxQi, 0);
        }

        [Test]
        public void NoDuel_BeforeTheFirstChakra()
        {
            Assert.IsNotNull(FighterRules.Refusal(Breathing(age: 20, chakra: 0)));
            Assert.IsNull(FighterRules.Refusal(Breathing(age: 20, chakra: 1)));
        }

        // ---- Review of the decisions (batch A) ----

        [Test]
        public void APeripheralAbility_HasNoDaoPartner()
        {
            CollectionAssert.IsEmpty(FoundationRules.DaoPartners(Lineage("orthodox-water"), "peril-refuge"));
        }

        [Test]
        public void APeripheralFoundation_ConsumesNoPartnerOfTheCentre()
        {
            var w = World();
            var consumer = w.Join(Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation, stage: 2));
            consumer.FoundationId = "orthodox-water:peril-refuge";
            var donor = w.Join(Fixtures.Cultivator(age: 60, realm: CultivationRealm.Foundation, stage: 2));
            donor.FoundationId = "orthodox-water:boundless-sea";
            w.Knowledge.Reveal(MirrorChronicles.World.FactKind.DaoPartners, consumer.FoundationId, MirrorChronicles.World.KnowledgeSource.Mirror);
            Assert.IsFalse(w.Foundations.ConsumeDaoPartner(consumer, donor));
        }

        [Test]
        public void FiveAbilitiesOutsideTheCentre_ForgeNoRealization()
        {
            var five = new[] { "boundless-sea", "ford-watcher", "storm-sky", "dike-guard", "peril-refuge" }.Select(a => $"orthodox-water:{a}");
            Assert.AreNotEqual(PositionRoute.Realization, GoldenCoreRules.RouteTo(five, "orthodox-water", Fixtures.Content.Fruitions));
        }

        [Test]
        public void TheMirror_DeducesTheMinors_OnlyFromTheFiveOfTheCentre()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            foreach (var a in new[] { "boundless-sea", "ford-watcher", "storm-sky", "peril-refuge", "black-water-serpent-king" })
                s.Knowledge.Reveal(MirrorChronicles.World.FactKind.Ability, $"orthodox-water:{a}", MirrorChronicles.World.KnowledgeSource.Mirror);
            StringAssert.Contains("cinq capacités orthodoxes", s.Minors.DeduceRefusal("orthodox-water"));
        }

        [Test]
        public void ARealizationsChild_SavedWithoutAnOrifice_GetsOneBack()
        {
            var w = World();
            var heir = Fixtures.Mortal(isMale: true);
            heir.TransformedLineage = true;
            heir.HasSpiritualOrifice = false; // a save made before audit §7.12
            w.Clan.Restore(new[] { heir }, heir.ID);
            Assert.IsTrue(heir.HasSpiritualOrifice);
        }
    }
}
