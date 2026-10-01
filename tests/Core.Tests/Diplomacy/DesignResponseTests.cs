using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Answering a patron's design (LORE.md §11.10; 2026-10-01): a design that demands something openly waits for the clan's
    /// answer, and so does a hidden one the clan has pierced; an unseen one strikes at once. The clan yields (the design takes
    /// effect), negotiates (buys it off with what an accord would give), or resists (the design fails, the patron turns hostile,
    /// and a stronger patron makes war on the clan).
    /// </summary>
    [TestFixture]
    public class DesignResponseTests
    {
        private const string Patron = "Famille Bai";
        private const string Method = "silent-tide-sutra";

        private static PatronDesignSettings Settings => Fixtures.Content.Balance.PatronDesigns;

        private static TestWorld World(string design, int patronPower = 5000)
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.AddFaction(new FactionData { Name = Patron, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 10,
                HighestRealm = CultivationRealm.GoldenCore, PowerLevel = patronPower, Techniques = { Method } });
            w.Factions.AddFaction(new FactionData { Name = "Famille Tao", Kind = FactionKind.Family, RegionId = "jingshui-lake" });
            var p = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            p.CultivationMethodId = Method;
            w.Clan.AppointPatriarch(p);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, design, 1, false) });
            return w;
        }

        private static void Harvest(TestWorld w)
        {
            w.Ctx.Clock.Restore(1 + Fixtures.Content.PatronDesigns.Single(d => d.Id == "harvest").Years, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(w.Ctx.Clock.Year);
        }

        private static bool Bound(TestWorld w) => w.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain && t.Faction == Patron);

        [Test]
        public void ADemand_WaitsForTheClansAnswer()
        {
            var w = World("harvest");
            string asked = null;
            w.Ctx.Events.OnPatronDemand += (s, d) => asked = d.Id;
            Harvest(w);
            Assert.IsTrue(asked == "harvest" && w.Sponsorships.Awaiting.Single().Id == "sp1" && !Bound(w));
        }

        [Test]
        public void AnUnseenDesign_StrikesAtOnce()
        {
            var w = World("refining");
            var p = w.Clan.LivingMembers.Single();
            w.Ctx.Events.TriggerBreakthroughSuccess(p, CultivationRealm.PurpleMansion);
            Assert.IsTrue(!p.IsAlive && w.Sponsorships.Awaiting.Count == 0);
        }

        [Test]
        public void APiercedDesign_WaitsForTheClansAnswer()
        {
            var w = World("refining");
            w.SecretBook.Grant(SecretBook.ClanHolder, w.SecretBook.Create("patron-design", Patron, "sp1").Id);
            var p = w.Clan.LivingMembers.Single();
            w.Ctx.Events.TriggerBreakthroughSuccess(p, CultivationRealm.PurpleMansion);
            Assert.IsTrue(p.IsAlive && w.Sponsorships.Awaiting.Count == 1);
        }

        [Test]
        public void Yielding_LetsTheDesignTakeEffect()
        {
            var w = World("harvest");
            Harvest(w);
            Assert.IsNull(w.Sponsorships.Yield("sp1"));
            Assert.IsTrue(Bound(w) && w.Sponsorships.Awaiting.Count == 0);
        }

        [Test]
        public void Negotiating_BuysTheDesignOff()
        {
            var w = World("harvest");
            Harvest(w);
            w.Techniques.Learn("clear-spring-sutra");
            var secret = w.SecretBook.Create("hidden-debt", "Famille Tao", null);
            w.SecretBook.Grant(SecretBook.ClanHolder, secret.Id);
            StringAssert.Contains("dette", w.Sponsorships.Negotiate("sp1", new[] { new AccordTerm(AccordCurrency.Debt, null, 1) }), "a promise buys no design off");
            StringAssert.Contains("ne suffit pas", w.Sponsorships.Negotiate("sp1", new[] { new AccordTerm(AccordCurrency.Technique, "clear-spring-sutra", 1) }));
            Assert.IsNull(w.Sponsorships.Negotiate("sp1", new[] { new AccordTerm(AccordCurrency.Technique, "clear-spring-sutra", 1),
                new AccordTerm(AccordCurrency.Secret, secret.Id, 1) }));
            Assert.IsTrue(!Bound(w) && w.Sponsorships.Awaiting.Count == 0 && w.Factions.GetFactionByName(Patron).Techniques.Contains("clear-spring-sutra"));
        }

        [Test]
        public void Resisting_ThwartsTheDesign_AndAStrongerPatronMakesWar()
        {
            var w = World("harvest");
            Harvest(w);
            Assert.IsNull(w.Sponsorships.Resist("sp1"));
            Assert.IsFalse(Bound(w));
            Assert.AreEqual(10 - Settings.ResistRelationLoss, w.Factions.GetFactionByName(Patron).RelationWithPlayer);
            Assert.IsTrue(w.Wars.ClanWars.Any(war => war.Enemy == Patron));
        }

        [Test]
        public void Resisting_AWeakerPatron_MakesNoWar()
        {
            var w = World("harvest", patronPower: 0);
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo));
            Harvest(w);
            w.Sponsorships.Resist("sp1");
            Assert.IsFalse(w.Wars.ClanWars.Any(war => war.Enemy == Patron));
        }

        [Test]
        public void TheAwaitedAnswers_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, "harvest", 1, true) { Awaiting = true } });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual("sp1", reloaded.Sponsorships.Awaiting.Single().Id);
        }
    }
}
