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
    /// The pills the clan's arts spend (AUDIT_LORE.md §2.2, §2.8): the Bright Spirit Powder for the fifth chakra (📚 wiki), the
    /// Heart's Purification Pill against a Heart Demon, the Condensation Pill for an ability condensed by resources — refined by
    /// an alchemist of the pill's mastery, or bought from a power that knows alchemy (the Purple Mansion's pill only in kind).
    /// </summary>
    [TestFixture]
    public class PillTests
    {
        private const string Qi = "clear-spring-qi";

        private static ArtSettings A => Fixtures.Content.Balance.Arts;

        private static PillDefinition Def(PillKind kind) => A.Pills.Single(p => p.Kind == kind);

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Resources.AddHerbs(10_000);
            s.Resources.AddSpiritStones(100_000);
            return s;
        }

        private static CharacterData Alchemist(GameSession s, int mastery)
        {
            s.Arts.GainLegacy(ImmortalArt.Alchemy);
            var a = Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 5);
            a.TalismanQiId = "holding-profit";
            a.ArtMastery[ImmortalArt.Alchemy] = mastery;
            s.Clan.AddMember(a);
            return a;
        }

        private static FactionData AlchemyPower(GameSession s) =>
            s.Factions.Factions.First(p => PowerArts.Knows(p, ImmortalArt.Alchemy, s.Context.Content));

        [Test]
        public void AnAlchemist_RefinesOnlyThePillsItsMasteryAllows()
        {
            var s = Session();
            var a = Alchemist(s, 1);
            StringAssert.Contains("adepte", s.Alchemy.PillRefusal(a, PillKind.Purification));
            Assert.IsNull(s.Alchemy.RefinePill(a.ID, PillKind.BrightSpirit));
            Assert.AreEqual(1, s.Alchemy.PillsOf(PillKind.BrightSpirit));
            StringAssert.Contains("cette année", s.Alchemy.PillRefusal(a, PillKind.BrightSpirit));
            var master = Alchemist(s, A.MasterAt);
            int herbs = s.Resources.MedicinalHerbs;
            Assert.IsNull(s.Alchemy.RefinePill(master.ID, PillKind.Condensation));
            Assert.AreEqual(herbs - Def(PillKind.Condensation).Herbs, s.Resources.MedicinalHerbs);
        }

        [Test]
        public void APower_SellsItsPills_ThePurpleMansionsOnlyInKind()
        {
            var s = Session();
            var power = AlchemyPower(s);
            power.RelationWithPlayer = A.PillBuyRelation;
            int stones = s.Resources.SpiritStones;
            Assert.IsNull(s.Alchemy.BuyPill(power.Name, PillKind.Purification, null));
            Assert.AreEqual(stones - Def(PillKind.Purification).Worth, s.Resources.SpiritStones);
            Assert.AreEqual(1, s.Alchemy.PillsOf(PillKind.Purification));
            var stonesTerms = new List<AccordTerm> { new AccordTerm(AccordCurrency.Stones, null, 1_000_000) };
            Assert.IsNotNull(s.Alchemy.BuyPill(power.Name, PillKind.Condensation, stonesTerms), "a Purple Mansion's pill is not bought with stones");
            int portions = Def(PillKind.Condensation).Worth / s.Context.Content.Balance.KnowledgeTrade.QiWorthPerPortion + 1;
            s.Resources.AddQi(Qi, portions);
            Assert.IsNull(s.Alchemy.BuyPill(power.Name, PillKind.Condensation, new List<AccordTerm> { new AccordTerm(AccordCurrency.Qi, Qi, portions) }));
            Assert.AreEqual(1, s.Alchemy.PillsOf(PillKind.Condensation));
        }

        [Test]
        public void APowerWithoutAlchemy_SellsNoPill()
        {
            var s = Session();
            var power = s.Factions.Factions.First(p => !PowerArts.Knows(p, ImmortalArt.Alchemy, s.Context.Content));
            power.RelationWithPlayer = 100;
            StringAssert.Contains("alchimie", s.Alchemy.BuyRefusal(power.Name, PillKind.BrightSpirit, null));
        }

        [Test]
        public void TheFifthChakra_WithoutTheBrightSpiritPowder_IsHarder_AndThePowderIsSpent()
        {
            var s = Session();
            var c = Fixtures.Cultivator(realm: CultivationRealm.Embryonic, stage: 4);
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Embryonic);
            s.Clan.AddMember(c);
            Assert.AreEqual(TrialKind.SummitEyeChakra, PowerLadder.Next(c.Realm, c.RealmStage).Trial);
            int without = s.Breakthroughs.TrialSuccessRate(c);
            s.Alchemy.GainPills(PillKind.BrightSpirit, 1);
            int with = s.Breakthroughs.TrialSuccessRate(c);
            Assert.AreEqual(System.Math.Max(1, with - A.WithoutPowderPenalty), without);
            s.Breakthroughs.AttemptBreakthrough(c);
            Assert.AreEqual(0, s.Alchemy.PillsOf(PillKind.BrightSpirit));
        }

        [Test]
        public void AHeartDemon_IsPurified_WithThePill_NotWithHerbs()
        {
            var s = Session();
            var m = Fixtures.Cultivator();
            m.HeartDemonYearsLeft = 5;
            s.Clan.AddMember(m);
            StringAssert.Contains("Pilule", s.Oaths.PurifyRefusal(m));
            s.Alchemy.GainPills(PillKind.Purification, 1);
            Assert.IsNull(s.Oaths.PurifyRefusal(m));
            s.Oaths.Purify(m);
            Assert.AreEqual(0, s.Alchemy.PillsOf(PillKind.Purification), "the pill is swallowed, whatever comes of it");
        }

        [Test]
        public void ThePills_AreSaved()
        {
            var s = Session();
            s.Alchemy.GainPills(PillKind.Condensation, 2);
            var loaded = GameSession.FromSaveData(s.ToSaveData(), new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(2, loaded.Alchemy.PillsOf(PillKind.Condensation));
        }
    }
}
