using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>The player's diplomatic acts: tribute, non-aggression pacts, war.</summary>
    [TestFixture]
    public class AllianceSystemTests
    {
        private static (TestWorld world, FactionData faction) WithFaction(FactionPersonality personality, int relation = 0)
        {
            var w = new TestWorld();
            var f = new FactionData { Name = "Test Faction", Personality = personality, RelationWithPlayer = relation };
            w.Factions.AddFaction(f);
            return (w, f);
        }

        [Test]
        public void OfferTribute_WarmsByFivePlusOnePerHundredStones()
        {
            var (w, f) = WithFaction(FactionPersonality.Isolationist);
            w.Alliances.OfferTribute(f.ID, 300);
            Assert.IsTrue(f.RelationWithPlayer == 8 && w.Resources.SpiritStones == 700);
        }

        [Test]
        public void OfferTribute_PleasesMerchantsMore()
        {
            var (w, f) = WithFaction(FactionPersonality.Merchant);
            w.Alliances.OfferTribute(f.ID, 300);
            Assert.AreEqual(12, f.RelationWithPlayer);
        }

        [Test]
        public void OfferTribute_Fails_WhenTheClanCannotPay()
        {
            var (w, f) = WithFaction(FactionPersonality.Merchant);
            w.Resources.SetSpiritStones(100);
            bool offered = w.Alliances.OfferTribute(f.ID, 300);
            Assert.IsTrue(!offered && f.RelationWithPlayer == 0);
        }

        [Test]
        public void ProposeNonAggression_IsAccepted_FromNeutralRelations()
        {
            var (w, f) = WithFaction(FactionPersonality.Isolationist, relation: 0);
            bool accepted = w.Alliances.ProposeNonAggression(f.ID);
            Assert.IsTrue(accepted && f.RelationWithPlayer == 10);
        }

        [Test]
        public void ProposeNonAggression_IsRejected_WhenRelationsAreHostile()
        {
            var (w, f) = WithFaction(FactionPersonality.Aggressive, relation: -5);
            Assert.IsFalse(w.Alliances.ProposeNonAggression(f.ID));
        }

        [Test]
        public void DeclareWar_SinksRelationsByAHundred()
        {
            var (w, f) = WithFaction(FactionPersonality.Aggressive, relation: 20);
            w.Alliances.DeclareWar(f.ID);
            Assert.AreEqual(-80, f.RelationWithPlayer);
        }
    }
}
