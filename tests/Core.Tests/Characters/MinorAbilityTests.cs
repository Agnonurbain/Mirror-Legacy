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
    /// known, and a power holding the lineage may teach one — for stones and good relations — or be robbed of it.
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

        [Test]
        public void APowerHoldingTheLineage_TeachesAMinor_ForStonesAndGoodRelations()
        {
            var s = Session();
            var (power, lineage) = AHolder(s);
            power.RelationWithPlayer = -10;
            Assert.IsNotNull(s.Minors.Buy(lineage, power.Name));
            power.RelationWithPlayer = 50;
            int stones = s.Resources.SpiritStones;
            Assert.IsNull(s.Minors.Buy(lineage, power.Name));
            Assert.Less(s.Resources.SpiritStones, stones);
            Assert.IsTrue(s.Context.Content.Fruitions.Single(f => f.Id == lineage).Abilities.Where(a => a.Substitute)
                .Any(a => s.Knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}")));
            var other = s.Factions.Factions.First(f => f.Elders.All(e => e.FruitionId != lineage));
            other.RelationWithPlayer = 50;
            StringAssert.Contains("lignée", s.Minors.Buy(lineage, other.Name), "only a power holding the lineage knows its minors");
        }

        [Test]
        public void APowerHoldingTheLineage_MayBeRobbedOfAMinor()
        {
            var b = Fixtures.QuietContent.Balance;
            var s = Session(Fixtures.QuietContent with { Balance = b with { Shards = b.Shards with { TheftMinChance = 1, TheftMaxChance = 1 } } });
            var (power, lineage) = AHolder(s);
            var thief = Fixtures.Cultivator(realm: CultivationRealm.Foundation);
            s.Clan.AddMember(thief);
            Assert.IsNull(s.Minors.Steal(lineage, power.Name, new[] { thief.ID }));
            Assert.IsTrue(s.Context.Content.Fruitions.Single(f => f.Id == lineage).Abilities.Where(a => a.Substitute)
                .Any(a => s.Knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}")));
        }
    }
}
