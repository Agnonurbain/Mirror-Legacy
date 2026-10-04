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
    }
}
