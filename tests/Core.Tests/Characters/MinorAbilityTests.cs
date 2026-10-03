using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The minor abilities of former True Monarchs (LORE.md §5.5.1, R2; L4e, user decisions 2026-10-03): every lineage has
    /// at least one (invented in the data where the lore names none); five abilities of the lineage with a minor one lead to
    /// its Surplus. They are rare knowledge: a tomb or ruins reveal one, the mirror deduces those of a lineage whose five are
    /// known, and a power holding the lineage may teach one — in kind, never for stones, and to the trusted. An ability is no thing
    /// to steal: its knowledge is given, found or deduced (the user's decision, 2026-10-03).
    /// </summary>
    [TestFixture]
    public class MinorAbilityTests
    {
        private static GameContent Sure()
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with { Balance = b with { MinorAbilities = b.MinorAbilities with { TombRevealChance = 1, RuinsRevealChance = 1 } } };
        }

        private static GameSession Session(GameContent content = null)
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? Sure() });
            s.Resources.AddSpiritStones(100_000);
            s.Mirror.Restore(MirrorSystem.MaxMirrorPower, 0);
            return s;
        }

        private static int KnownMinors(GameSession s) =>
            s.Context.Content.Fruitions.Sum(f => f.Abilities.Count(a => a.Substitute && s.Knowledge.Knows(FactKind.Ability, $"{f.Id}:{a.Id}")));

        [Test]
        public void EveryLineage_HasAMinorAbility_ThatLeadsToItsSurplus()
        {
            var content = Fixtures.Content;
            Assert.IsTrue(content.Fruitions.All(f => f.Abilities.Any(a => a.Substitute)));
            var water = content.Fruitions.Single(f => f.Id == "orthodox-water");
            var five = water.Abilities.Where(a => !a.Substitute).Take(4).Append(water.Abilities.First(a => a.Substitute)).Select(a => $"orthodox-water:{a.Id}");
            Assert.AreEqual(PositionRoute.Surplus, MirrorChronicles.Characters.GoldenCoreRules.RouteTo(five, "orthodox-water", content.Fruitions));
        }

        [Test]
        public void KnowingTheDaoPartners_RevealsNoMinorAbility()
        {
            var s = Session();
            s.Knowledge.Reveal(FactKind.DaoPartners, "gathered-wood:" + s.Context.Content.Fruitions.Single(f => f.Id == "gathered-wood").Abilities[0].Id, KnowledgeSource.Mirror);
            Assert.IsEmpty(s.Minors.Known("gathered-wood"), "the partners are the orthodox four; a minor ability is rare knowledge");
        }

        [Test]
        public void TheClan_KnowsNoMinorAbility_AtTheStart()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.AreEqual(0, KnownMinors(s));
        }

        [Test]
        public void ATomb_RevealsAMinorAbility()
        {
            var s = Session();
            int before = KnownMinors(s);
            s.Events.TriggerTombLooted();
            Assert.AreEqual(before + 1, KnownMinors(s));
        }

        [Test]
        public void Ruins_MayRevealOne()
        {
            var s = Session();
            int before = KnownMinors(s);
            s.Minors.SearchTheRuins();
            Assert.AreEqual(before + 1, KnownMinors(s));
        }

        [Test]
        public void TheMirror_DeducesTheMinorsOfALineageWhoseFiveAreKnown()
        {
            var s = Session();
            var water = s.Context.Content.Fruitions.Single(f => f.Id == "orthodox-water");
            StringAssert.Contains("cinq", s.Minors.Deduce("orthodox-water"));
            foreach (var a in water.Abilities.Where(a => !a.Substitute).Take(5)) s.Knowledge.Reveal(FactKind.Ability, $"orthodox-water:{a.Id}", KnowledgeSource.Mirror);
            int power = s.Mirror.MirrorPower;
            Assert.IsNull(s.Minors.Deduce("orthodox-water"));
            Assert.Less(s.Mirror.MirrorPower, power);
            Assert.IsTrue(water.Abilities.Where(a => a.Substitute).All(a => s.Knowledge.Knows(FactKind.Ability, $"orthodox-water:{a.Id}")));
        }

        private static (FactionData Power, string Lineage) AHolder(GameSession s)
        {
            var power = s.Factions.Factions.First(f => f.Elders.Count > 0);
            var lineage = s.Context.Content.Fruitions.First(f => f.Id == "gathered-wood");
            power.Elders[0].FruitionId = lineage.Id;
            return (power, lineage.Id);
        }

        private static System.Collections.Generic.List<MirrorChronicles.Diplomacy.AccordTerm> QiWorth(GameSession s, int worth)
        {
            int portions = worth / s.Context.Content.Balance.KnowledgeTrade.QiWorthPerPortion + 1;
            s.Resources.AddQi("clear-spring-qi", portions);
            return new System.Collections.Generic.List<MirrorChronicles.Diplomacy.AccordTerm>
                { new MirrorChronicles.Diplomacy.AccordTerm(MirrorChronicles.Diplomacy.AccordCurrency.Qi, "clear-spring-qi", portions) };
        }

        [Test]
        public void APowerHoldingTheLineage_TeachesAMinor_InKind_AndToTheTrusted()
        {
            var s = Session();
            var (power, lineage) = AHolder(s);
            int worth = s.Context.Content.Balance.MinorAbilities.Worth;
            power.RelationWithPlayer = -10;
            Assert.IsNotNull(s.Minors.Buy(lineage, power.Name, QiWorth(s, worth)));
            power.RelationWithPlayer = 50;
            Assert.IsNull(s.Minors.Buy(lineage, power.Name, QiWorth(s, worth)));
            Assert.IsTrue(s.Context.Content.Fruitions.Single(f => f.Id == lineage).Abilities.Where(a => a.Substitute)
                .Any(a => s.Knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}")));
            var other = s.Factions.Factions.First(f => f.Elders.All(e => e.FruitionId != lineage));
            other.RelationWithPlayer = 50;
            StringAssert.Contains("lignée", s.Minors.Buy(lineage, other.Name, QiWorth(s, worth)), "only a power holding the lineage knows its minors");
        }
    }
}
