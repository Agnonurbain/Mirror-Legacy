using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Presentation;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The accords screen (LORE.md §11.10, A): for a method a power holds, everything the clan could give and what each is worth
    /// to that power; the chosen terms' worth against the method's price, and why the power would refuse.
    /// </summary>
    [TestFixture]
    public class AccordViewTests
    {
        private const string Holder = "Famille Bai";
        private const string Ascent = "silent-tide-sutra";
        private const string Ordinary = "woven-heart-sutra";

        private static TestWorld World()
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.AddFaction(new FactionData { Name = Holder, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 60,
                HighestRealm = CultivationRealm.PurpleMansion, Techniques = { Ascent, Ordinary } });
            w.Factions.AddFaction(new FactionData { Name = "Famille Tao", Kind = FactionKind.Family, RegionId = "jingshui-lake" });
            w.Techniques.Learn("clear-spring-sutra");
            w.Resources.AddQi("clear-spring-qi", 2);
            w.Resources.AddBeast(new CapturedBeast("b1", CultivationRealm.Foundation, 3));
            w.SecretBook.Grant(SecretBook.ClanHolder, w.SecretBook.Create("hidden-debt", "Famille Tao", null).Id);
            w.Join(Fixtures.Cultivator()); // the patriarch never leaves as a disciple
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            w.Resources.AddSpiritStones(100_000);
            return w;
        }

        private static System.Collections.Generic.IReadOnlyList<AccordCandidate> Candidates(TestWorld w, string technique) =>
            AccordView.Candidates(w.Accords, w.Clan, w.Techniques, w.Resources, w.SecretBook, Holder, technique);

        [Test]
        public void TheCandidates_ListWhatTheClanCouldGive_AndTheirWorth()
        {
            var w = World();
            var kinds = Candidates(w, Ascent).Select(c => c.Term.Currency).ToList();
            CollectionAssert.IsSubsetOf(new[] { AccordCurrency.Technique, AccordCurrency.Secret, AccordCurrency.Beast, AccordCurrency.Qi,
                AccordCurrency.Debt, AccordCurrency.Disciple }, kinds);
            CollectionAssert.DoesNotContain(kinds, AccordCurrency.Stones, "stones buy no ascent method");
            Assert.AreEqual(1, kinds.Count(k => k == AccordCurrency.Disciple), "never the patriarch");
            Assert.IsTrue(Candidates(w, Ascent).All(c => c.Worth > 0 && c.Label.Length > 0));
        }

        [Test]
        public void StonesAreOffered_ForAnOrdinaryMethod()
        {
            var w = World();
            var stones = Candidates(w, Ordinary).Single(c => c.Term.Currency == AccordCurrency.Stones);
            Assert.AreEqual(w.Accords.PriceOf(Fixtures.Content.Techniques.Single(t => t.ID == Ordinary)), stones.Term.Amount);
        }

        [Test]
        public void TheSummary_WeighsTheTermsAgainstThePrice()
        {
            var w = World();
            var candidates = Candidates(w, Ascent);
            var debt = candidates.Single(c => c.Term.Currency == AccordCurrency.Debt);

            var few = AccordView.Summary(w.Accords, Holder, Ascent, new[] { debt.Term });
            Assert.IsTrue(few.Worth == debt.Worth && few.Price > few.Worth);
            StringAssert.Contains("ne suffit pas", few.Refusal);

            var all = AccordView.Summary(w.Accords, Holder, Ascent, candidates.Select(c => c.Term).ToList());
            Assert.IsNull(all.Refusal);
        }
    }
}
