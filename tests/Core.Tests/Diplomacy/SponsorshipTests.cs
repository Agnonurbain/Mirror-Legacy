using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Patrons (LORE.md §11.10, C; the user's decisions of 2026-09-30): the likeliest way to an ascent method is a power that
    /// offers it — always a plot. The clan sees the offer, never the design, unless it pierces the patron's secret; accepting
    /// teaches the method and leaves a debt; the design falls due at its milestone; a mark or a flaw in the manual can be
    /// cleansed by the mirror.
    /// </summary>
    [TestFixture]
    public class SponsorshipTests
    {
        private const string Patron = "Famille Bai";
        private const string Ascent = "silent-tide-sutra";

        private static TestWorld World(double roll = 0.0)
        {
            var w = new TestWorld(new FixedRandom(roll));
            w.Factions.AddFaction(new FactionData { Name = Patron, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 10,
                HighestRealm = CultivationRealm.PurpleMansion, Techniques = { Ascent } });
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 4)); // a Foundation at its peak: the clan needs a way up
            return w;
        }

        private static PatronDesign Design(string id) => Fixtures.Content.PatronDesigns.Single(d => d.Id == id);

        [Test]
        public void TheGameShips_TheDesignsOfTheLore()
        {
            Assert.AreEqual(23, Fixtures.Content.PatronDesigns.Count);
            Assert.IsTrue(Fixtures.Content.PatronDesigns.Any(d => d.Id == "harvest") && Fixtures.Content.PatronDesigns.Any(d => d.Id == "sincere"));
        }

        [Test]
        public void APatron_OffersAnAscentMethod_WhenTheClanNeedsOne()
        {
            var w = World();
            w.Sponsorships.ProcessYear();
            var offer = w.Sponsorships.Pending;
            Assert.IsTrue(offer != null && offer.Power == Patron && offer.TechniqueId == Ascent);
            Assert.IsNotNull(Fixtures.Content.PatronDesigns.SingleOrDefault(d => d.Id == offer.DesignId), "a design is drawn, hidden");
        }

        [Test]
        public void NoOffer_WhenTheClanAlreadyKnowsAWayUp()
        {
            var w = World();
            w.Techniques.Learn(Ascent);
            w.Sponsorships.ProcessYear();
            Assert.IsNull(w.Sponsorships.Pending);
        }

        [Test]
        public void Accepting_TeachesTheMethod_LeavesADebt_AndHidesTheDesignInThePatronsSecrets()
        {
            var w = World();
            w.Sponsorships.ProcessYear();
            var offer = w.Sponsorships.Pending;

            Assert.IsNull(w.Sponsorships.Accept());

            Assert.IsTrue(w.Techniques.Knows(Ascent) && w.Sponsorships.Pending == null);
            var sponsorship = w.Sponsorships.Active.Single();
            Assert.IsTrue(sponsorship.Power == Patron && sponsorship.DesignId == offer.DesignId && !w.Sponsorships.IsRevealed(sponsorship));
            Assert.IsTrue(w.Accords.Debts.Any(d => d.Power == Patron && d.TechniqueId == Ascent));
            Assert.IsTrue(w.SecretBook.Of(Patron).Any(s => s.KindId == "patron-design" && s.Subject == sponsorship.Id));
        }

        [Test]
        public void Refusing_SoursThePatron()
        {
            var w = World();
            w.Sponsorships.ProcessYear();
            int relation = w.Factions.GetFactionByName(Patron).RelationWithPlayer;
            w.Sponsorships.Refuse();
            Assert.IsTrue(w.Sponsorships.Pending == null && w.Factions.GetFactionByName(Patron).RelationWithPlayer < relation);
        }

        [Test]
        public void PiercingThePatronsSecret_RevealsTheDesign()
        {
            var w = World();
            w.Sponsorships.ProcessYear();
            w.Sponsorships.Accept();
            var sponsorship = w.Sponsorships.Active.Single();
            w.SecretBook.Grant(SecretBook.ClanHolder, w.SecretBook.Of(Patron).Single(s => s.Subject == sponsorship.Id).Id);
            Assert.IsTrue(w.Sponsorships.IsRevealed(sponsorship));
        }

        [Test]
        public void ADesign_FallsDue_WhenItsPractitionerReachesThePurpleMansion()
        {
            var w = World();
            Assert.AreEqual(DesignDue.PurpleMansion, Design("refining").Due);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Ascent, "refining", 1, false) });
            string due = null;
            w.Ctx.Events.OnPatronDesignDue += (s, d) => due = d.Id;
            var practitioner = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            practitioner.CultivationMethodId = Ascent;

            w.Ctx.Events.TriggerBreakthroughSuccess(practitioner, CultivationRealm.PurpleMansion);

            Assert.AreEqual("refining", due);
            Assert.IsTrue(w.Sponsorships.Active.Single().Due);
        }

        [Test]
        public void ADesign_FallsDue_AfterItsYears()
        {
            var w = World();
            var harvest = Design("harvest");
            Assert.AreEqual(DesignDue.Years, harvest.Due);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Ascent, "harvest", 1, false) });
            w.Ctx.Clock.Restore(1 + harvest.Years - 1, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(w.Ctx.Clock.Year);
            Assert.IsFalse(w.Sponsorships.Active.Single().Due);
            w.Ctx.Clock.Restore(1 + harvest.Years, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(w.Ctx.Clock.Year);
            Assert.IsTrue(w.Sponsorships.Active.Single().Due);
        }

        [Test]
        public void TheMirror_CleansesAMarkedManual()
        {
            var w = World();
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            Assert.IsTrue(Design("mark").Cleansable);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Ascent, "mark", 1, false) });
            Assert.IsNull(w.Sponsorships.Cleanse("sp1"));
            Assert.IsTrue(w.Sponsorships.Active.Single().Cleansed);
            StringAssert.Contains("rien à nettoyer", w.Sponsorships.Cleanse("sp1"));
        }

        [Test]
        public void TheSponsorships_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Sponsorships.Restore(new SponsorOffer(Patron, Ascent, "harvest", 3), new[] { new Sponsorship("sp1", Patron, Ascent, "mark", 1, false) });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(s.Sponsorships.Pending, reloaded.Sponsorships.Pending);
            CollectionAssert.AreEqual(s.Sponsorships.Active, reloaded.Sponsorships.Active);
        }
    }
}
