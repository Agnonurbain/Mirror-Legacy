using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Clan
{
    /// <summary>
    /// Keeping the cultivating line (user decision 2026-09-29, after the long games): the orifice is hereditary and rare,
    /// so a clan marries its cultivators with care. In the yearly matches a cultivator seeks a cultivator first; the clan
    /// may arrange a marriage between two of its members when kinship allows; and it may seek a cultivator spouse abroad —
    /// at a price, with no certainty, a member of higher realm drawing a better match.
    /// </summary>
    [TestFixture]
    public class LineageTests
    {
        private static LineageSettings Settings => Fixtures.VeteranContent.Balance.Lineage;

        private static CharacterData Cultivator(TestWorld w, bool isMale, int age = 25, CultivationRealm realm = CultivationRealm.QiRefinement) =>
            w.Join(Fixtures.Cultivator(isMale: isMale, age: age, realm: realm));

        [Test]
        public void ACultivator_SeeksACultivator_First()
        {
            var seeker = Fixtures.Cultivator(isMale: true, age: 25);
            var mortal = Fixtures.Mortal(isMale: false, age: 25);
            var cultivator = Fixtures.Cultivator(isMale: false, age: 35);
            foreach (var (c, id) in new[] { (seeker, "s"), (mortal, "m"), (cultivator, "c") }) c.ID = id;
            Assert.AreSame(cultivator, MarriageMatchmaker.FindClanPartner(seeker, new[] { seeker, mortal, cultivator }, _ => null));
            Assert.AreSame(mortal, MarriageMatchmaker.FindClanPartner(Fixtures.Mortal(age: 25), new[] { mortal, cultivator }, _ => null),
                "a mortal marries by age");
        }

        [Test]
        public void TheClan_ArrangesAMarriage_BetweenTwoOfItsOwn()
        {
            var w = new TestWorld(new FixedRandom(0.999));
            var a = Cultivator(w, true);
            var b = Cultivator(w, false);
            Assert.IsNull(w.Marriages.Arrange(a.ID, b.ID));
            Assert.AreEqual(b.ID, a.SpouseID);
            Assert.IsNotNull(w.Marriages.Arrange(a.ID, b.ID), "already wed");
        }

        [Test]
        public void KinDoesNotWed()
        {
            var w = new TestWorld(new FixedRandom(0.999));
            var father = Cultivator(w, true, 50);
            var mother = Cultivator(w, false, 48);
            var son = w.Join(Fixtures.Cultivator(isMale: true, age: 25));
            var daughter = w.Join(Fixtures.Cultivator(isMale: false, age: 22));
            son.FatherID = daughter.FatherID = father.ID;
            son.MotherID = daughter.MotherID = mother.ID;
            StringAssert.Contains("parenté", w.Marriages.Arrange(son.ID, daughter.ID));
        }

        [Test]
        public void AFoundingKinsman_IsKin_ToTheFoundingFamily()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            var kinsman = s.Clan.LivingMembers.Single(m => m.FirstName == "Shan");   // clan.json: role « Kin »
            var niece = s.Clan.LivingMembers.Single(m => m.FirstName == "Mei");      // a child of the patriarch
            StringAssert.Contains("parenté", s.Marriages.MarriageRefusal(kinsman, niece));
            Assert.IsFalse(GenealogyView.Tree(s).Any(n => n.Id == kinsman.FatherID), "the forebears are only remembered, not drawn");
        }

        [Test]
        public void ACultivatorSpouse_IsSoughtAbroad_AtAPrice()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var seeker = Cultivator(w, true);
            int stones = w.Resources.SpiritStones;

            Assert.IsNull(w.Marriages.SeekCultivatorSpouse(seeker.ID));

            Assert.AreEqual(stones - Settings.SeekStones, w.Resources.SpiritStones);
            var spouse = w.Clan.FindById(seeker.SpouseID);
            Assert.IsTrue(spouse.HasSpiritualOrifice && spouse.OrificeKnown);
            Assert.AreEqual(CultivationRealm.QiRefinement, spouse.Realm);
            Assert.IsNull(spouse.FromFaction, "a wandering cultivator, bound to no power");
        }

        [Test]
        public void TheSearch_MayFail_AndStillCosts()
        {
            var w = new TestWorld(new FixedRandom(0.999));
            var seeker = Cultivator(w, true);
            int stones = w.Resources.SpiritStones;
            StringAssert.Contains("personne", w.Marriages.SeekCultivatorSpouse(seeker.ID));
            Assert.AreEqual(stones - Settings.SeekStones, w.Resources.SpiritStones);
            Assert.IsNull(seeker.SpouseID);
        }

        [Test]
        public void TheSearch_IsRefused_WithoutTheStones_OrForTheWed()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var seeker = Cultivator(w, true);
            w.Resources.SetSpiritStones(Settings.SeekStones - 1);
            StringAssert.Contains("pierres", w.Marriages.SeekCultivatorSpouse(seeker.ID));
            w.Resources.SetSpiritStones(10000);
            w.Marriages.SeekCultivatorSpouse(seeker.ID);
            Assert.IsNotNull(w.Marriages.SeekCultivatorSpouse(seeker.ID), "already wed");
        }

        [Test]
        public void AHigherRealm_DrawsABetterMatch()
        {
            var low = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement);
            var high = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            Assert.Greater(LineageRules.SeekChance(high, Settings), LineageRules.SeekChance(low, Settings));
        }

        [Test]
        public void TheScreen_ListsTheUnwedCultivators_AndWhomTheyMayWed()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            var a = Fixtures.Cultivator(isMale: true, age: 25);
            var b = Fixtures.Cultivator(isMale: false, age: 24);
            s.Clan.AddMember(a);
            s.Clan.AddMember(b);
            var line = OperationsView.Unwed(s).Single(u => u.Id == a.ID);
            Assert.IsTrue(line.Partners.Any(p => p.Id == b.ID));
            Assert.IsNull(line.SeekRefusal);
        }

        [Test]
        public void ThePilot_MarriesItsCultivators()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 3, Content = Fixtures.VeteranContent });
            s.Resources.SetSpiritStones(100000);
            var a = Fixtures.Cultivator(isMale: true, age: 25);
            var b = Fixtures.Cultivator(isMale: false, age: 24);
            (a.ID, b.ID) = ("line-a", "line-b");
            s.Clan.AddMember(a);
            s.Clan.AddMember(b);
            BalanceRun.Act(s);
            bool Cultivates(string id) => MirrorChronicles.Characters.SpiritualOrificeRules.CanCultivate(s.Clan.FindById(id));
            Assert.IsFalse(OperationsView.Unwed(s).Any(u => u.Partners.Any(p => Cultivates(p.Id))),
                "no two unwed cultivators left who could wed each other");
        }

        // ---- Review ----

        [Test]
        public void OneRule_SaysWhoMayWed()
        {
            var w = new TestWorld(new FixedRandom(0.999));
            var a = Cultivator(w, true);
            var b = Cultivator(w, true);
            Assert.IsFalse(w.Marriages.CanMarry(a, b), "the same rule as MarriageRefusal");
        }

        [Test]
        public void TheMinesYield_IsEstimatedAsTheMineGivesIt()
        {
            var w = new TestWorld();
            w.Buildings.Restore(new[] { new BuildingData(BuildingType.Forge) { Level = 3 } });
            var miners = Enumerable.Range(0, Fixtures.VeteranContent.Balance.Upkeep.VeinMiners + 4)
                .Select(_ => w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement))).ToList();
            foreach (var m in miners) w.Tasks.AssignTask(m, TaskType.Mine);
            int estimate = w.Tasks.MiningYield(miners);
            Assert.AreEqual(w.Tasks.ProcessYearlyTasks().StonesMined, estimate);
        }

        [Test]
        public void ARecord_IsFound_EvenWhenNamedAfterItJoined()
        {
            var w = new TestWorld();
            var member = w.Join(Fixtures.Cultivator());
            member.ID = "named-late";
            Assert.AreSame(member, w.Clan.FindById("named-late"));
        }
    }
}
