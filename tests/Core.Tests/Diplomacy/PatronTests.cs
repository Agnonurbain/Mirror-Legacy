using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Pacts of the very high level (user decision 2026-09-27): with a great beast or a lone figure — the Vixen of the
    /// Qingyan Mountains and her ancient pact (LORE.md §7), others interpreted — only for a clan strong enough. A yearly
    /// tribute keeps the partner's favour; its boon protects (an ambush foiled), teaches (fragments) or lends its sight
    /// (the clan's probes); unpaid, favour falls, and at nothing the partner's wrath kills a member and ends the pact.
    /// The clan may end it: safely in favour, dangerously out of it.
    /// </summary>
    [TestFixture]
    public class PatronTests
    {
        private const string Vixen = "qingyan-vixen";      // protection
        private const string Tortoise = "black-tortoise";  // insight
        private const string Hermit = "nameless-hermit";   // sight
        private static PatronSettings Settings => Fixtures.Content.Balance.Patrons;

        private static PatronDefinition Patron(string id) => Fixtures.Content.Patrons.Single(p => p.Id == id);

        private static TestWorld World(System.Random rng, CultivationRealm strongest)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator(realm: strongest, stage: 5)));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement, stage: 1));
            return w;
        }

        [Test]
        public void TheLoresVixen_IsAPartner_OfTheVeryHighLevel()
        {
            var vixen = Patron(Vixen);
            Assert.AreEqual(Provenance.Lore, vixen.Provenance);
            Assert.AreEqual("qingyan-mountains", vixen.RegionId);
            Assert.IsTrue(Fixtures.Content.Patrons.All(p => p.MinClanRealm >= CultivationRealm.PurpleMansion), "only for the strong");
        }

        [Test]
        public void AWeakClan_IsNotHeard()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.Foundation);
            StringAssert.Contains("forts", w.Patrons.Propose(Vixen));
        }

        [Test]
        public void APact_TakesItsFirstTribute_AndBindsThePartner()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.GoldenCore);
            int stones = w.Resources.SpiritStones;
            Assert.IsNull(w.Patrons.Propose(Vixen));
            Assert.AreEqual(stones - Patron(Vixen).Tribute, w.Resources.SpiritStones);
            Assert.IsNotNull(w.Patrons.Propose(Vixen), "one pact per partner");
        }

        [Test]
        public void ThePact_ProtectsTheClan_FromAnAmbush()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.GoldenCore);
            w.Patrons.Propose(Vixen);
            var envoy = w.Join(Fixtures.Cultivator());
            envoy.CurrentTask = TaskType.Diplomacy;
            w.Schemes.Ambush(w.Factions.GetFactionByName("Famille Ruan"));
            Assert.IsNull(envoy.CaptorFaction);
        }

        [Test]
        public void ThePact_Teaches_OrLendsItsSight()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.GoldenCore);
            w.Resources.AddSpiritStones(10000);
            w.Patrons.Propose(Tortoise);
            int fragments = w.Resources.TechniqueFragments;
            w.Patrons.ProcessYear();
            Assert.AreEqual(fragments + Patron(Tortoise).BoonStrength, w.Resources.TechniqueFragments);

            Assert.AreEqual(0, w.Patrons.SightBonus);
            w.Patrons.Propose(Hermit);
            Assert.AreEqual(Patron(Hermit).BoonStrength, w.Patrons.SightBonus);
        }

        [Test]
        public void ATributeUnpaid_CostsFavour_AndNoFavour_BringsWrath()
        {
            var w = World(new FixedRandom(0.999), CultivationRealm.GoldenCore);
            w.Patrons.Propose(Vixen);
            w.Resources.ConsumeSpiritStones(w.Resources.SpiritStones);
            int living = w.Clan.LivingMembers.Count;

            for (int year = 0; year < 20 && w.Patrons.Pacts.Count > 0; year++) w.Patrons.ProcessYear();

            Assert.AreEqual(0, w.Patrons.Pacts.Count, "the pact ends in wrath");
            Assert.AreEqual(living - 1, w.Clan.LivingMembers.Count, "a member pays with their life");
        }

        [Test]
        public void EndingThePact_InFavour_IsSafe()
        {
            var w = World(new FixedRandom(0.0), CultivationRealm.GoldenCore);
            w.Patrons.Propose(Vixen);
            int living = w.Clan.LivingMembers.Count;
            Assert.IsNull(w.Patrons.End(Vixen));
            Assert.AreEqual(0, w.Patrons.Pacts.Count);
            Assert.AreEqual(living, w.Clan.LivingMembers.Count);
        }

        [Test]
        public void RoundTrip_KeepsThePacts()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Patrons.RestorePacts(new[] { new PatronPact(Vixen, 1, 4) });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(new PatronPact(Vixen, 1, 4), reloaded.Patrons.Pacts.Single());
        }

        [Test]
        public void TheDiplomacyScreen_ShowsTheGreatPartners_WithASignOfFavour()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var vixen = MirrorChronicles.Presentation.DiplomacyView.Patrons(s).Single(p => p.Id == Vixen);
            Assert.AreEqual("La Renarde des Monts Qingyan", vixen.Name);
            StringAssert.Contains("forts", vixen.Refusal, "a young clan is not heard");
            Assert.IsNull(vixen.Favor);
            s.Patrons.RestorePacts(new[] { new PatronPact(Vixen, 1, 1) });
            StringAssert.Contains("colère", MirrorChronicles.Presentation.DiplomacyView.Patrons(s).Single(p => p.Id == Vixen).Favor);
        }
    }
}
