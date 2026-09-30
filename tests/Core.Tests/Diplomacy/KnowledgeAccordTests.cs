using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Accords of knowledge (LORE.md §11.10; the user's decisions of 2026-09-30): a method is paid with what the power wants —
    /// a technique, a secret, a beast, Qi, a debt, a disciple — never with stones for a method that leads to the Purple
    /// Mansion; the power accepts when the worth covers the method and it trusts the clan enough.
    /// </summary>
    [TestFixture]
    public class KnowledgeAccordTests
    {
        private const string Holder = "Famille Bai";
        private const string Ascent = "silent-tide-sutra";   // grade 5
        private const string Ordinary = "woven-heart-sutra"; // grade 3

        private static KnowledgeTradeSettings Settings => Fixtures.Content.Balance.KnowledgeTrade;

        private static TestWorld World(int relation = 60)
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.AddFaction(new FactionData { Name = Holder, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = relation,
                HighestRealm = CultivationRealm.PurpleMansion, Techniques = { Ascent, Ordinary } });
            w.Factions.AddFaction(new FactionData { Name = "Famille Tao", Kind = FactionKind.Family, RegionId = "jingshui-lake" });
            w.Techniques.Learn("clear-spring-sutra"); // the clan's own method, to give
            w.Resources.AddQi("clear-spring-qi", 1);
            return w;
        }

        /// <summary>A secret of a third party the clan has pierced.</summary>
        private static string ASecret(TestWorld w)
        {
            var secret = w.SecretBook.Create("hidden-debt", "Famille Tao", null);
            w.SecretBook.Grant(SecretBook.ClanHolder, secret.Id);
            return secret.Id;
        }

        [Test]
        public void AnAscentMethod_IsObtained_ForATechniqueASecretAndABeast()
        {
            var w = World();
            var beast = new CapturedBeast("b1", CultivationRealm.Foundation, 3);
            w.Resources.AddBeast(beast);
            string secret = ASecret(w);
            var terms = new[]
            {
                new AccordTerm(AccordCurrency.Technique, "clear-spring-sutra", 1),
                new AccordTerm(AccordCurrency.Secret, secret, 1),
                new AccordTerm(AccordCurrency.Beast, "b1", 1)
            };
            Assert.IsNull(w.Accords.Refusal(Holder, Ascent, terms));

            Assert.IsNull(w.Accords.Conclude(Holder, Ascent, terms));

            Assert.IsTrue(w.Techniques.Knows(Ascent));
            var power = w.Factions.GetFactionByName(Holder);
            Assert.IsTrue(power.Techniques.Contains("clear-spring-sutra") && w.SecretBook.Knows(Holder, secret) && !w.Resources.Beasts.Contains(beast));
        }

        [Test]
        public void Stones_BuyNoAscentMethod_EvenInAnAccord()
        {
            var w = World();
            w.Resources.AddSpiritStones(1_000_000);
            StringAssert.Contains("ne suffit pas", w.Accords.Refusal(Holder, Ascent, new[] { new AccordTerm(AccordCurrency.Stones, null, 1_000_000) }));
        }

        [Test]
        public void Stones_StillPayForAnOrdinaryMethod()
        {
            var w = World();
            w.Resources.AddSpiritStones(1_000_000);
            Assert.IsNull(w.Accords.Conclude(Holder, Ordinary, new[] { new AccordTerm(AccordCurrency.Stones, null, 1_000) }));
            Assert.IsTrue(w.Techniques.Knows(Ordinary));
        }

        [Test]
        public void TooLittle_IsRefused()
        {
            var w = World();
            StringAssert.Contains("ne suffit pas", w.Accords.Refusal(Holder, Ascent, new[] { new AccordTerm(AccordCurrency.Qi, "clear-spring-qi", 1) }));
        }

        [Test]
        public void AColdPower_MakesNoAccordForAnAscentMethod()
        {
            var w = World(relation: Settings.AscentAccordMinRelation - 1);
            StringAssert.Contains("relation", w.Accords.Refusal(Holder, Ascent, new[] { new AccordTerm(AccordCurrency.Debt, null, 1) }));
        }

        [Test]
        public void ADebt_IsOwed_UntilThePowerCallsIt()
        {
            var w = World();
            string secret = ASecret(w);
            Assert.IsNull(w.Accords.Conclude(Holder, Ascent, new[] { new AccordTerm(AccordCurrency.Debt, null, 1), new AccordTerm(AccordCurrency.Secret, secret, 1),
                new AccordTerm(AccordCurrency.Technique, "clear-spring-sutra", 1) }));
            Assert.AreEqual(Holder, w.Accords.Debts.Single().Power);
        }

        [Test]
        public void ADisciple_LeavesTheClan_ForThePower()
        {
            var w = World();
            var disciple = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            string secret = ASecret(w);
            Assert.IsNull(w.Accords.Conclude(Holder, Ascent, new[] { new AccordTerm(AccordCurrency.Disciple, disciple.ID, 1),
                new AccordTerm(AccordCurrency.Secret, secret, 1) }));
            Assert.IsTrue(disciple.Departed && !w.Clan.LivingMembers.Contains(disciple));
        }

        [Test]
        public void TheDebts_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Accords.RestoreDebts(new[] { new KnowledgeDebt(Holder, 12, "silent-tide-sutra") });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            CollectionAssert.AreEqual(s.Accords.Debts, reloaded.Accords.Debts);
        }
    }
}
