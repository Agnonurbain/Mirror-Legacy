using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>The Immortal Arts' screen (audit §2): the legacies held, each cultivator's gifts, mastery, and why it cannot practise.</summary>
    [TestFixture]
    public class ArtViewTests
    {
        [Test]
        public void TheScreen_ShowsTheLegacies_TheGifts_AndWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(4, ArtView.Legacies(s).Count);
            Assert.IsFalse(ArtView.Legacies(s).Any(l => l.Held), "no legacy at the start (the user's decision)");
            var line = ArtView.Members(s).First();
            Assert.AreEqual(4, line.Skills.Count);
            Assert.IsTrue(line.Skills.All(k => k.Refusal != null), "without a legacy, no art");
            s.Arts.GainLegacy(ImmortalArt.Forge);
            Assert.IsTrue(ArtView.Legacies(s).Single(l => l.Art == ImmortalArt.Forge).Held);
        }

        [Test]
        public void ThePills_AreCounted_AndOffered_ForTheElementsTheClanNeeds()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var needy = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 8);
            s.Clan.AddMember(needy);
            var element = AlchemySystem.ElementFor(needy, s.Context.Content).Value;
            StringAssert.Contains("aucune", ArtView.PillStock(s));
            var offer = ArtView.PillOffers(s).Single(o => o.Element == element);
            StringAssert.Contains("héritage", offer.Refusal, "no alchemy without its legacy");

            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            s.Resources.AddHerbs(1_000);
            var alchemist = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            alchemist.TalismanQiId = "holding-profit";
            alchemist.ArtMastery[ImmortalArt.Alchemy] = 50;
            s.Clan.AddMember(alchemist);
            offer = ArtView.PillOffers(s).Single(o => o.Element == element);
            Assert.IsNull(offer.Refusal);
            Assert.AreEqual(alchemist.ID, offer.AlchemistId);
            Assert.IsNull(s.Alchemy.RefineEssencePill(offer.AlchemistId, offer.Element));
            StringAssert.Contains(WorldMapView.ElementLabel(element) + " 1", ArtView.PillStock(s));
        }

        [Test]
        public void TheExamination_IsOffered_ToTheBestAlchemist_OrSaysWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            StringAssert.Contains("aucune pilule", ArtView.ExamineOffer(s).Refusal);
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            s.Alchemy.GainEssencePills(Element.Fire, 1);
            var adept = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            adept.TalismanQiId = "holding-profit";
            adept.ArtMastery[ImmortalArt.Alchemy] = s.Context.Content.Balance.Arts.AdeptAt;
            s.Clan.AddMember(adept);
            var offer = ArtView.ExamineOffer(s);
            Assert.IsNull(offer.Refusal);
            Assert.AreEqual(adept.ID, offer.AlchemistId);
            StringAssert.Contains(adept.FullName, offer.Label);
        }

        [Test]
        public void TheTalismans_SayWhatTheYearWouldSell()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Arts.GainLegacy(ImmortalArt.Talismans);
            var drawer = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            drawer.TalismanQiId = "holding-profit";
            drawer.ArtMastery[ImmortalArt.Talismans] = 40;
            s.Clan.AddMember(drawer);
            var skills = ArtView.Members(s).Single(l => l.Id == drawer.ID).Skills;
            int stones = ImmortalArtRules.TalismanStones(drawer, s.Context.Content.Balance.Arts);
            StringAssert.Contains($"{stones} pierres", skills.Single(k => k.Art == ImmortalArt.Talismans).Yield);
            Assert.IsNull(skills.Single(k => k.Art == ImmortalArt.Forge).Yield, "only the talismans sell");
        }

        [Test]
        public void EachLackingArt_OffersItsSources_OrSaysWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var offers = ArtView.LegacyOffers(s);
            Assert.AreEqual(4, offers.Count(o => o.FromMirror), "the mirror may deduce each lacking art");
            Assert.IsTrue(offers.Where(o => o.FromMirror).All(o => o.Refusal != null || o.FragmentIds.Count == s.Context.Content.Balance.Arts.Legacy.DeduceFragments));
            var taught = offers.Where(o => !o.FromMirror).ToList();
            Assert.IsNotEmpty(taught, "a power knows some art");
            Assert.IsTrue(taught.All(o => PowerArts.Knows(s.Factions.GetFactionByName(o.Power), o.Art, s.Context.Content)));
            s.Arts.GainLegacy(ImmortalArt.Forge);
            Assert.IsFalse(ArtView.LegacyOffers(s).Any(o => o.Art == ImmortalArt.Forge), "a held art is not offered");
        }

        [Test]
        public void EachPill_OffersItsRefining_AndItsPurchase_OrSaysWhyNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            StringAssert.Contains("aucune", ArtView.OtherPillStock(s));
            var offers = ArtView.PillKindOffers(s);
            Assert.AreEqual(s.Context.Content.Balance.Arts.Pills.Count, offers.Count);
            Assert.IsTrue(offers.All(o => o.RefineRefusal != null), "no alchemy legacy at the start");
            var seller = s.Factions.Factions.First(p => PowerArts.Knows(p, ImmortalArt.Alchemy, s.Context.Content));
            foreach (var f in s.Factions.Factions) f.RelationWithPlayer = 0;
            seller.RelationWithPlayer = 50;
            s.Resources.AddSpiritStones(10_000);
            var powder = ArtView.PillKindOffers(s).Single(o => o.Kind == PillKind.BrightSpirit);
            Assert.AreEqual(seller.Name, powder.BuyPower);
            Assert.IsNull(powder.BuyRefusal);
            Assert.IsNull(s.Alchemy.BuyPill(powder.BuyPower, powder.Kind, powder.BuyTerms));
            StringAssert.Contains("Poudre d'Esprit Lumineux 1", ArtView.OtherPillStock(s));
        }

        [Test]
        public void AHeartDemon_OffersItsPurification()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var haunted = s.Clan.LivingMembers.First();
            haunted.HeartDemonYearsLeft = 4;
            var offer = ArtView.PurifyOffers(s).Single();
            Assert.AreEqual(haunted.ID, offer.MemberId);
            StringAssert.Contains("Pilule", offer.Refusal);
            s.Alchemy.GainPills(PillKind.Purification, 1);
            Assert.IsNull(ArtView.PurifyOffers(s).Single().Refusal);
        }

        [Test]
        public void AFoundation_MayRecast_OnAQiTheClanHolds()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var m = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            m.FoundationId = s.Context.Content.Qi.First(q => q.Id == m.QiId).Foundation;
            s.Clan.AddMember(m);
            var qi = s.Context.Content.Qi.First(q => q.Foundation != null && !q.Vanished && q.Foundation != m.FoundationId);
            s.Resources.AddQi(qi.Id, s.Context.Content.Balance.Arts.Longevity.RecastQiPortions);
            var offer = ArtView.RecastOffers(s).Single(o => o.MemberId == m.ID && o.QiId == qi.Id);
            StringAssert.Contains("Refonte", offer.Refusal);
            s.Alchemy.GainPills(PillKind.FoundationRecasting, 1);
            Assert.IsNull(ArtView.RecastOffers(s).Single(o => o.MemberId == m.ID && o.QiId == qi.Id).Refusal);
        }

        [Test]
        public void APrisoner_MayBeRefined_ForTheMemberWithTheFewestYears()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var old = Fixtures.Cultivator(age: 95, realm: CultivationRealm.Foundation);
            old.MaxLifespan = 100;
            s.Clan.AddMember(old);
            s.Captives.Imprison(new Prisoner("p1", s.Factions.Factions.First().Name, CultivationRealm.Foundation, s.Clock.Year));
            var offer = ArtView.HumanPillOffers(s).Single();
            Assert.AreEqual(old.ID, offer.RecipientId);
            StringAssert.Contains("pilule humaine", offer.Label);
            Assert.IsNotNull(offer.Refusal, "no alchemy at the start");
        }
    }
}
