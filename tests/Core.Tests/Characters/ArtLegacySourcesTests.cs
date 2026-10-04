using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// Where an Immortal Art's legacy comes from (AUDIT_LORE.md §2.5, the user's decision 2026-10-04: none at the start): a power
    /// that knows the art teaches it in kind (precious: stones weigh nothing), ruins and a Purple Mansion's tomb may hold a
    /// manual, and the mirror may deduce one from fragments.
    /// </summary>
    [TestFixture]
    public class ArtLegacySourcesTests
    {
        private const string Qi = "clear-spring-qi";

        private static LegacySettings L => Fixtures.Content.Balance.Arts.Legacy;

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Fixtures.QuietContent });

        private static GameContent With(System.Func<LegacySettings, LegacySettings> tweak)
        {
            var c = Fixtures.QuietContent;
            return c with { Balance = c.Balance with { Arts = c.Balance.Arts with { Legacy = tweak(c.Balance.Arts.Legacy) } } };
        }

        /// <summary>Qi portions worth at least the price, given to the clan to offer.</summary>
        private static List<AccordTerm> QiWorth(GameSession s, int worth)
        {
            int portions = worth / s.Context.Content.Balance.KnowledgeTrade.QiWorthPerPortion + 1;
            s.Resources.AddQi(Qi, portions);
            return new List<AccordTerm> { new AccordTerm(AccordCurrency.Qi, Qi, portions) };
        }

        /// <summary>A power that knows the art.</summary>
        private static (FactionData Power, ImmortalArt Art) ATeacher(GameSession s) =>
            s.Factions.Factions.SelectMany(p => System.Enum.GetValues<ImmortalArt>().Where(a => PowerArts.Knows(p, a, s.Context.Content)).Select(a => (p, a)))
                .First();

        [Test]
        public void ThePowers_KnowArts_ByTheirKind_AndOnlyFromTheFoundation()
        {
            var s = Session();
            Assert.IsTrue(s.Factions.Factions.Any(p => System.Enum.GetValues<ImmortalArt>().Any(a => PowerArts.Knows(p, a, s.Context.Content))));
            var weak = new FactionData { Name = "Les Faibles", Kind = FactionKind.Sect, HighestRealm = CultivationRealm.QiRefinement };
            Assert.IsFalse(System.Enum.GetValues<ImmortalArt>().Any(a => PowerArts.Knows(weak, a, s.Context.Content)));
            var (power, art) = ATeacher(s);
            Assert.AreEqual(PowerArts.Knows(power, art, s.Context.Content), PowerArts.Knows(power, art, s.Context.Content), "stable");
        }

        [Test]
        public void AFriendlyPower_TeachesItsArt_InKind_NeverForStones()
        {
            var s = Session();
            s.Resources.AddSpiritStones(1_000_000);
            var (power, art) = ATeacher(s);
            power.RelationWithPlayer = 0;
            StringAssert.Contains("relation", s.Arts.LearnRefusal(power.Name, art, QiWorth(s, L.Worth)));
            power.RelationWithPlayer = L.TeachRelation;
            var stones = new List<AccordTerm> { new AccordTerm(AccordCurrency.Stones, null, 1_000_000) };
            Assert.IsNotNull(s.Arts.LearnFrom(power.Name, art, stones), "stones weigh nothing for a legacy");
            Assert.IsFalse(s.Arts.HoldsLegacy(art));
            Assert.IsNull(s.Arts.LearnFrom(power.Name, art, QiWorth(s, L.Worth)));
            Assert.IsTrue(s.Arts.HoldsLegacy(art));
            StringAssert.Contains("tient déjà", s.Arts.LearnRefusal(power.Name, art, QiWorth(s, L.Worth)));
        }

        [Test]
        public void APowerTeachesOnlyTheArtsItKnows()
        {
            var s = Session();
            var power = s.Factions.Factions.First(p => System.Enum.GetValues<ImmortalArt>().Any(a => !PowerArts.Knows(p, a, s.Context.Content)));
            var unknown = System.Enum.GetValues<ImmortalArt>().First(a => !PowerArts.Knows(power, a, s.Context.Content));
            power.RelationWithPlayer = 100;
            StringAssert.Contains("ne connaît pas", s.Arts.LearnRefusal(power.Name, unknown, QiWorth(s, L.Worth)));
        }

        [Test]
        public void Ruins_MayHoldAnArtsManual()
        {
            var s = Session(With(l => l with { RuinsChance = 1 }));
            s.Arts.SearchTheRuins();
            Assert.AreEqual(1, s.Arts.Legacies.Count);
        }

        [Test]
        public void ATomb_MayHoldAnArtsManual()
        {
            var s = Session(With(l => l with { TombChance = 1 }));
            s.Context.Events.TriggerTombLooted();
            Assert.AreEqual(1, s.Arts.Legacies.Count);
        }

        [Test]
        public void TheMirror_DeducesAnArt_FromFragments_AtItsPrice()
        {
            var s = Session();
            for (int i = 0; i < L.DeduceFragments; i++) s.Deduction.AddFragment(Element.Fire, 3, $"F{i}");
            var ids = s.Deduction.Fragments.Skip(s.Deduction.Fragments.Count - L.DeduceFragments).Select(f => f.ID).ToList(); // those just added
            s.Mirror.Restore(L.DeduceMoonlight - 1, 0);
            StringAssert.Contains("Clair de Lune", s.Arts.DeduceRefusal(ImmortalArt.Talismans, ids));
            s.Mirror.Restore(L.DeduceMoonlight, 0);
            StringAssert.Contains($"{L.DeduceFragments}", s.Arts.DeduceRefusal(ImmortalArt.Talismans, ids.Take(1).ToList()));
            Assert.IsNull(s.Arts.Deduce(ImmortalArt.Talismans, ids));
            Assert.IsTrue(s.Arts.HoldsLegacy(ImmortalArt.Talismans));
            Assert.IsFalse(s.Deduction.Fragments.Any(f => ids.Contains(f.ID)), "the fragments are spent");
            Assert.AreEqual(0, s.Mirror.MirrorPower);
        }

        [Test]
        public void AGainedLegacy_IsToldInTheChronicle()
        {
            var s = Session();
            string told = null;
            s.Context.Events.OnArtLegacyGained += (art, how) => told = how;
            s.Arts.GainLegacy(ImmortalArt.Forge, "les ruines livrent un manuel");
            Assert.AreEqual("les ruines livrent un manuel", told);
        }
    }
}
