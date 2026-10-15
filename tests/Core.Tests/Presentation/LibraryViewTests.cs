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

        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });

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
        public void Library_ShowsAnArtsMasters_ItsStudents_AndWhoMayStudyIt()
        {
            var s = NewGame();
            var art = new TechniqueData { ID = "art-view", Name = "Paume de la Marée", Kind = TechniqueKind.Spell, Effect = TechniqueEffect.Strike, Grade = 2,
                Category = TechniqueCategory.Secret, RequiredRealm = CultivationRealm.Embryonic };
            s.Techniques.AddDeduced(art);
            var cultivators = s.Clan.LivingMembers.Where(m => s.Tasks.ArtStudyRefusal(m, art.ID) == null).ToList();
            var master = cultivators[0];
            master.KnownTechniqueIDs.Add(art.ID);
            var student = cultivators[1];
            s.Tasks.AssignArtStudy(student, art.ID);

            var line = LibraryView.Library(s).Single(t => t.Id == art.ID);

            CollectionAssert.AreEqual(new[] { master.FullName }, line.Masters);
            CollectionAssert.AreEqual(new[] { $"{student.FullName} (0/2 an(s))" }, line.Students);
            CollectionAssert.DoesNotContain(line.MayStudy.Select(c => c.Id), master.ID, "it masters it");
            CollectionAssert.DoesNotContain(line.MayStudy.Select(c => c.Id), student.ID, "it studies it");
            Assert.IsNull(LibraryView.Library(s).Single(t => t.Id == "clear-spring-sutra").MayStudy, "a method is practised, not studied");
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
            var trade = Fixtures.VeteranContent.Balance.KnowledgeTrade;

            var method = LibraryView.Market(s).Single(m => m.Power == "Famille Lü" && m.TechniqueId == "clear-edge-method");
            var gradeFour = LibraryView.Market(s).Single(m => m.Power == Peak && m.TechniqueId == "measured-rain-method");
            var canon = LibraryView.Market(s).Single(m => m.Power == Peak && m.TechniqueId == "night-frost-canon");

            Assert.AreEqual(trade.StonesPerGrade[method.Grade - 1], method.Price);
            StringAssert.Contains("relation", method.Refusal, "the Lü are not friendly enough");
            StringAssert.Contains("grade 3 ou moins", gradeFour.Refusal, "stones buy only a common manual of grade 3 or less (audit §7.10)");
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
            s.Factions.ChangeRelation(s.Factions.GetFactionByName("Famille Ruan").ID, 50);
            s.Resources.AddSpiritStones(100000);
            var offer = LibraryView.Market(s).First(m => m.Power == "Famille Ruan" && m.Refusal == null);

            Assert.IsTrue(s.Exchange.BuyTechnique("Famille Ruan", offer.TechniqueId));
            Assert.IsTrue(LibraryView.Library(s).Any(t => t.Id == offer.TechniqueId));
        }
    }
}
