using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The armoury as the clan sees it (L4f): its artifacts, whom to entrust each to, and what may be done.</summary>
    [TestFixture]
    public class ArtifactViewTests
    {
        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Buildings.GetBuilding(BuildingType.Forge).Level = 2;
            s.Resources.AddOres(1_000);
            s.Resources.AddSpiritStones(10_000);
            return s;
        }

        [Test]
        public void EachArtifact_IsShown_WithItsClassAndWhoBearsIt_AndAnEntrusting()
        {
            var s = Session();
            var m = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            s.Clan.AddMember(m);
            s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.Foundation, null);
            var row = ArtifactView.Rows(s).Single();
            StringAssert.Contains("Artefact de Dharma", row.Label);
            StringAssert.Contains("armurerie", row.Label);
            Assert.AreEqual(m.ID, row.EntrustTo);
        }

        [Test]
        public void TheForgeOffers_TheHighestRankItsBestSmithCanMake()
        {
            var s = Session();
            var smith = Fixtures.Cultivator(realm: CultivationRealm.Foundation).AsSmith(s);
            s.Clan.AddMember(smith);
            var offers = ArtifactView.ForgeOffers(s);
            Assert.AreEqual(s.Context.Content.ArtifactForms.Count, offers.Count, "one offer per form");
            Assert.IsTrue(offers.All(o => o.Rank == CultivationRealm.Foundation && o.SmithId == smith.ID && o.Refusal == null));
        }

        [Test]
        public void FriendlyPowers_OfferLoans_AndHostileOnes_ATheft()
        {
            var s = Session();
            var friend = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            friend.RelationWithPlayer = 90;
            var foe = s.Factions.Factions.First(f => f != friend);
            foe.RelationWithPlayer = -60;
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            var offers = ArtifactView.PowerOffers(s);
            Assert.IsTrue(offers.Any(o => o.Kind == ArtifactDeal.Borrow && o.Power == friend.Name));
            Assert.IsTrue(offers.Any(o => o.Kind == ArtifactDeal.Steal && o.Power == foe.Name));
            Assert.IsFalse(offers.Any(o => o.Kind == ArtifactDeal.Steal && o.Power == friend.Name));
        }

        [Test]
        public void AnArtifactInStore_OffersItsRaising_ByTheBestSmith_AtItsPrice()
        {
            var s = Session();
            var smith = Fixtures.Cultivator(realm: CultivationRealm.Foundation).AsSmith(s);
            s.Clan.AddMember(smith);
            var a = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.QiRefinement, null);
            var row = ArtifactView.Rows(s).Single(r => r.Id == a.Id);
            Assert.IsNull(row.RaiseRefusal);
            Assert.AreEqual(smith.ID, row.RaiseSmithId);
            StringAssert.Contains("minerais", row.RaiseLabel);
            Assert.IsNull(s.Forge.Raise(a.Id, row.RaiseSmithId));
            Assert.AreEqual(CultivationRealm.Foundation, s.Artifacts.Armoury.Single().Rank);
        }

        [Test]
        public void ASpiritualTreasure_OffersNoRaising()
        {
            var s = Session();
            s.Clan.AddMember(Fixtures.Cultivator(realm: CultivationRealm.GoldenCore));
            var t = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.PurpleMansion, null, ArtifactClass.SpiritualTreasure);
            Assert.IsNull(ArtifactView.Rows(s).Single(r => r.Id == t.Id).RaiseLabel);
        }

        [Test]
        public void AnArtifactInStore_MayBeLent_ToTheFriendliestPower()
        {
            var s = Session();
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = -10;
            var friend = s.Factions.Factions.First();
            friend.RelationWithPlayer = 40;
            var a = s.Artifacts.Create(s.Context.Content.ArtifactForms.First().Id, CultivationRealm.QiRefinement, null);
            var row = ArtifactView.Rows(s).Single(r => r.Id == a.Id);
            Assert.AreEqual(friend.Name, row.LendTo);
            StringAssert.Contains(friend.Name, row.LendLabel);
            Assert.IsNull(s.ArtifactTrade.Lend(a.Id, row.LendTo));
            Assert.IsEmpty(s.Artifacts.Armoury);
            var lent = ArtifactView.Rows(s).Single(r => r.Id == a.Id);
            StringAssert.Contains($"prêté à {friend.Name} jusqu'en l'an", lent.Label, "a loan is still the clan's: it is shown");
            Assert.IsNull(lent.EntrustTo);
            Assert.IsNull(lent.LendTo);
            Assert.IsNull(lent.RaiseLabel);
        }
    }
}
