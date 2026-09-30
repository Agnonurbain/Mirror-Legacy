using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The clan's library (G6): the arts the clan knows — kind, grade, category, element, first realm, the Qi a method
    /// needs and how many portions of it are in store, who practises it — and the market of knowledge: what each power
    /// would sell, its price, and why it will not sell now (LORE.md §2.3-§2.4).
    /// </summary>
    [TestFixture]
    public class LibraryViewTests
    {
        private const string Peak = "Secte du Pic des Nuées";

        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        [Test]
        public void Library_ListsTheKnownArts_WithTheirQi_AndWhoPractisesThem()
        {
            var s = NewGame();

            var sutra = LibraryView.Library(s).Single(t => t.Id == "clear-spring-sutra");

            Assert.AreEqual("Méthode de cultivation", sutra.Kind);
            Assert.AreEqual("Qi de la Source Claire", sutra.Qi);
            Assert.AreEqual(2, sutra.QiInStore);
            CollectionAssert.Contains(sutra.Practitioners, s.Clan.GetPatriarch().FullName);
            Assert.IsFalse(LibraryView.Library(s).Any(t => t.Id == "night-frost-canon"), "only what the clan knows");
        }

        [Test]
        public void Library_PutsTheMethodsFirst_TheHighestGradeFirst()
        {
            var library = LibraryView.Library(NewGame());
            Assert.AreEqual("Méthode de cultivation", library[0].Kind);
            Assert.GreaterOrEqual(library[0].Grade, library.Where(t => t.Kind == library[0].Kind).Max(t => t.Grade));
        }

        [Test]
        public void KindLabel_NamesEveryKindDifferently()
        {
            var labels = System.Enum.GetValues(typeof(TechniqueKind)).Cast<TechniqueKind>().Select(LibraryView.KindLabel).ToList();
            Assert.IsTrue(labels.All(l => !string.IsNullOrWhiteSpace(l)) && labels.Distinct().Count() == labels.Count);
        }

        // ---- The market of knowledge ----

        [Test]
        public void Market_ListsWhatThePowersWouldSell_AndWhyNotNow()
        {
            var s = NewGame();
            var trade = Fixtures.Content.Balance.KnowledgeTrade;

            var method = LibraryView.Market(s).Single(m => m.Power == Peak && m.TechniqueId == "measured-rain-method");
            var canon = LibraryView.Market(s).Single(m => m.Power == Peak && m.TechniqueId == "night-frost-canon");

            Assert.AreEqual(trade.StonesPerGrade[method.Grade - 1], method.Price);
            StringAssert.Contains("relation", method.Refusal, "the Peak is not yet friendly enough");
            StringAssert.Contains("ne s'achète pas", canon.Refusal, "an ascent method is never sold (LORE.md §11.10)");
            Assert.IsFalse(LibraryView.Market(s).Any(m => m.TechniqueId == "clear-spring-sutra"), "nothing the clan knows");
        }

        [Test]
        public void Market_SaysWhenTheStonesAreMissing()
        {
            var s = NewGame();
            s.Factions.ChangeRelation(s.Factions.GetFactionByName(Peak).ID, 50);
            s.Resources.ConsumeSpiritStones(s.Resources.SpiritStones);

            StringAssert.Contains("pierres", LibraryView.Market(s).First(m => m.Power == Peak).Refusal);
        }

        [Test]
        public void Market_AgreesWithThePurchase()
        {
            var s = NewGame();
            s.Factions.ChangeRelation(s.Factions.GetFactionByName(Peak).ID, 50);
            s.Resources.AddSpiritStones(100000);
            var offer = LibraryView.Market(s).First(m => m.Power == Peak);

            Assert.IsNull(offer.Refusal);
            Assert.IsTrue(s.Exchange.BuyTechnique(Peak, offer.TechniqueId));
            Assert.IsTrue(LibraryView.Library(s).Any(t => t.Id == offer.TechniqueId));
        }
    }
}
