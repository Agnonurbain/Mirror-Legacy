using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Revealing what the clan does not know (L4c; LORE.md §11 P3 « knowledge is a resource »): a technique bought
    /// from a power that holds it (LORE.md §2.4), the Dao Partners of a foundation deciphered by the mirror.
    /// </summary>
    [TestFixture]
    public class KnowledgeExchangeTests
    {
        private const string Ruan = "Famille Ruan";
        private const string Brook = "upstream-brook-method";

        private static KnowledgeTradeSettings Settings => Fixtures.Content.Balance.KnowledgeTrade;

        private static TestWorld World()
        {
            var w = new TestWorld();
            w.Factions.InitializeFactions();
            return w;
        }

        private static void Befriend(TestWorld w, string faction) =>
            w.Factions.ChangeRelation(w.Factions.GetFactionByName(faction).ID, 100);

        private static int Price(string techniqueId) =>
            Settings.StonesPerGrade[Fixtures.Content.Techniques.Single(t => t.ID == techniqueId).Grade - 1];

        // ---- Buying a technique from a power that holds it ----

        [Test]
        public void BuyTechnique_TeachesTheClan_ForStonesByGrade()
        {
            var w = World();
            Befriend(w, Ruan);
            w.Resources.AddSpiritStones(Price(Brook));
            int before = w.Resources.SpiritStones;

            Assert.IsTrue(w.Exchange.BuyTechnique(Ruan, Brook));

            Assert.IsTrue(w.Techniques.Knows(Brook));
            Assert.AreEqual(before - Price(Brook), w.Resources.SpiritStones);
        }

        [Test]
        public void BuyTechnique_Refuses_ATechniqueThePowerDoesNotHold()
        {
            var w = World();
            Befriend(w, Ruan);
            w.Resources.AddSpiritStones(100000);
            Assert.IsFalse(w.Exchange.BuyTechnique(Ruan, "seven-terraces-canon")); // the Gu's, not the Ruan's
        }

        [Test]
        public void BuyTechnique_Refuses_APowerThatDoesNotTrustTheClan()
        {
            var w = World();
            w.Factions.ChangeRelation(w.Factions.GetFactionByName(Ruan).ID, -100);
            w.Resources.AddSpiritStones(100000);
            Assert.IsFalse(w.Exchange.BuyTechnique(Ruan, Brook));
            Assert.IsFalse(w.Techniques.Knows(Brook));
        }

        [Test]
        public void BuyTechnique_Refuses_WithoutTheStones()
        {
            var w = World();
            Befriend(w, Ruan);
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones);
            Assert.IsFalse(w.Exchange.BuyTechnique(Ruan, Brook));
        }

        [Test]
        public void BuyTechnique_Refuses_WhatTheClanAlreadyKnows()
        {
            var w = World();
            Befriend(w, Ruan);
            w.Resources.AddSpiritStones(100000);
            w.Techniques.Learn(Brook);
            int before = w.Resources.SpiritStones;

            Assert.IsFalse(w.Exchange.BuyTechnique(Ruan, Brook));
            Assert.AreEqual(before, w.Resources.SpiritStones);
        }

        [Test]
        public void Offers_ListTheTechniquesAPowerCouldSell_ThatTheClanLacks()
        {
            var w = World();
            CollectionAssert.AreEquivalent(new[] { "clear-spring-sutra", Brook }.Where(id => !w.Techniques.Knows(id)),
                w.Exchange.Offers(Ruan).Select(t => t.ID));
        }

        // ---- The mirror deciphers a foundation's Dao Partners ----

        [Test]
        public void DecipherDaoPartners_RevealsThePartnersOfAKnownFoundation()
        {
            var w = World();
            const string sea = "orthodox-water:boundless-sea";
            w.Knowledge.Reveal(FactKind.Ability, sea, KnowledgeSource.Formed);
            int before = w.Mirror.MirrorPower;

            Assert.IsTrue(w.Exchange.DecipherDaoPartners(sea));

            Assert.IsTrue(w.Knowledge.Knows(FactKind.DaoPartners, sea));
            Assert.IsTrue(w.Knowledge.Knows(FactKind.Ability, "orthodox-water:river-farewell"));
            Assert.AreEqual(before - Settings.DaoPartnersMirrorCost, w.Mirror.MirrorPower);
        }

        [Test]
        public void DecipherDaoPartners_Refuses_AFoundationTheClanDoesNotKnow()
        {
            var w = World();
            Assert.IsFalse(w.Exchange.DecipherDaoPartners("orthodox-water:boundless-sea"));
        }
    }
}
