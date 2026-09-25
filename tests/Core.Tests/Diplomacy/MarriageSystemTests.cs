using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>Love, arranged and annual marriages; outsiders join the clan (ported from the Unity CanMarry tests).</summary>
    [TestFixture]
    public class MarriageSystemTests
    {
        [Test]
        public void CanMarry_ReturnsFalse_WhenParentAndChild()
        {
            var w = new TestWorld();
            var father = w.Join(Fixtures.Cultivator(age: 45));
            var daughter = Fixtures.Cultivator(isMale: false, age: 20);
            daughter.FatherID = father.ID;
            w.Join(daughter);
            Assert.IsFalse(w.Marriages.CanMarry(father, daughter));
        }

        [Test]
        public void CanMarry_ReturnsTrue_WhenUnrelatedAdults()
        {
            var w = new TestWorld();
            var a = w.Join(Fixtures.Cultivator(age: 25));
            var b = w.Join(Fixtures.Cultivator(isMale: false, age: 23));
            Assert.IsTrue(w.Marriages.CanMarry(a, b));
        }

        [Test]
        public void HandleLoveMarriage_BringsTheOutsiderIntoTheClan()
        {
            var w = new TestWorld();
            var member = w.Join(Fixtures.Cultivator(age: 25));
            var outsider = Fixtures.Mortal(isMale: false, age: 24);
            w.Marriages.HandleLoveMarriage(member, outsider);
            Assert.IsTrue(member.SpouseID == outsider.ID && outsider.SpouseID == member.ID
                && w.Clan.LivingMembers.Contains(outsider) && member.MentalStability == 80);
        }

        [Test]
        public void HandleArrangedMarriage_BringsATrainedCultivatorAndWarmsTheFaction()
        {
            var w = new TestWorld();
            var faction = new FactionData { Name = "Wang Family" };
            w.Factions.AddFaction(faction);
            var member = w.Join(Fixtures.Cultivator(age: 25));

            w.Marriages.HandleArrangedMarriage(member, faction.ID, isForced: false);

            var spouse = w.Clan.FindById(member.SpouseID);
            Assert.IsTrue(spouse.Realm == CultivationRealm.QiRefinement && spouse.RealmStage == 1
                && spouse.HasSpiritualOrifice && faction.RelationWithPlayer == 25);
        }

        [Test]
        public void HandleArrangedMarriage_HurtsAForcedMember()
        {
            var w = new TestWorld();
            var faction = new FactionData { Name = "Wang Family" };
            w.Factions.AddFaction(faction);
            var member = w.Join(Fixtures.Cultivator(age: 25));
            w.Marriages.HandleArrangedMarriage(member, faction.ID, isForced: true);
            Assert.AreEqual(55, member.MentalStability);
        }

        [Test]
        public void ProcessAnnualMarriages_MarriesTheEligible_WhenTheRollSucceeds()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            var single = w.Join(Fixtures.Cultivator(age: 22));
            int married = w.Marriages.ProcessAnnualMarriages();
            Assert.IsTrue(married == 1 && single.SpouseID != null);
        }
    }
}
