using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The bridge Intercalary, 4A+1B=C (LORE.md §5.5.1, R6; L4e, user decisions 2026-10-03): four abilities of a lineage and
    /// one « bridge » ability of another lead to the Intercalary of a third — possible only while their Virtue is corrupted.
    /// The lore's one case (Winter Drizzle of the Nourishing Water between the Mutable and the Hidden Water) is open from
    /// the start, the Water's Virtue being corrupted; the bridges of the other Virtues wait for theirs to be.
    /// </summary>
    [TestFixture]
    public class BridgeIntercalaryTests
    {
        private static GameContent Content => Fixtures.QuietContent;

        private static string[] FourOf(string lineage) =>
            Content.Fruitions.Single(f => f.Id == lineage).Abilities.Where(a => !a.Substitute).Take(4).Select(a => $"{lineage}:{a.Id}").ToArray();

        private static string[] WaterBridge => FourOf("mutable-water").Append("nourishing-water:winter-drizzle").ToArray();

        [Test]
        public void FourMutableWater_AndWinterDrizzle_LeadToTheHiddenWatersIntercalary_WhileTheWaterIsCorrupted()
        {
            var corrupted = new[] { Element.Water };
            Assert.AreEqual(PositionRoute.IntercalaryBridge, GoldenCoreRules.RouteTo(WaterBridge, "hidden-water", Content, corrupted));
            Assert.AreEqual(PositionRoute.None, GoldenCoreRules.RouteTo(WaterBridge, "hidden-water", Content, new Element[0]), "no bridge without corruption");
            Assert.AreEqual(PositionRoute.IntercalaryBridge.ToString(), "IntercalaryBridge");
        }

        [Test]
        public void TheWater_IsCorruptedFromTheStart_AndTheOtherVirtuesAreNot()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Content });
            CollectionAssert.AreEquivalent(new[] { Element.Water }, s.Bridges.Corrupted);
            var fire = s.Context.Content.Balance.PositionBridges.Bridges.First(b => b.Virtue == Element.Fire);
            var abilities = FourOf(fire.From).Append($"{fire.Bridge}:{Content.Fruitions.Single(f => f.Id == fire.Bridge).Abilities[0].Id}");
            Assert.AreEqual(PositionRoute.None, GoldenCoreRules.RouteTo(abilities, fire.To, Content, s.Bridges.Corrupted));
            s.Bridges.Corrupt(Element.Fire);
            Assert.AreEqual(PositionRoute.IntercalaryBridge, GoldenCoreRules.RouteTo(abilities, fire.To, Content, s.Bridges.Corrupted));
        }

        [Test]
        public void ABridgeEssence_IsGrantedTheIntercalary()
        {
            var b = Content.Balance;
            var content = Content with { Balance = b with { GoldenCore = b.GoldenCore with { IntercalaryBridgeChance = 99 } } };
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = content });
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.GoldenCore);
            m.DivineAbilities = WaterBridge.ToList();
            m.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            m.FruitionId = "hidden-water";
            m.SpiritualRoot = 100;
            s.Clan.AddMember(m);
            var state = s.Fruitions.State("hidden-water");
            if (state.Status == FruitionStatus.Occupied) s.Fruitions.Vacate("hidden-water");
            Assume.That(s.Fruitions.State("hidden-water").Status, Is.EqualTo(FruitionStatus.Free));
            Assert.IsTrue(s.GoldenCore.ClaimPosition(m));
            Assert.AreEqual(GoldenCoreState.Intercalary, m.GoldenCore);
        }

        [Test]
        public void ACorruptedVirtue_SurvivesASave()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Content });
            s.Bridges.Corrupt(Element.Fire);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = Content });
            CollectionAssert.AreEquivalent(new[] { Element.Water, Element.Fire }, reloaded.Bridges.Corrupted);
        }
    }
}
