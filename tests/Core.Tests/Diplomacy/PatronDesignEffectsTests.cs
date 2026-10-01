using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// What a patron's design does when it falls due (LORE.md §11.10, C; first wave, 2026-10-01): the harvest binds the clan
    /// as a vassal, the refining consumes the practitioner's foundation, the mark lets the patron see into the clan, the hidden
    /// flaw wounds and caps the practitioner, the hostage takes the most gifted child, the sincere patron forgives its debt
    /// and gives. A cleansed manual leaves a mark or a flaw nothing to hold.
    /// </summary>
    [TestFixture]
    public class PatronDesignEffectsTests
    {
        /// <summary>A design that demands openly waits for an answer: these tests let the clan yield.</summary>
        private static void YieldAll(TestWorld w)
        {
            foreach (var s in w.Sponsorships.Awaiting.ToList()) w.Sponsorships.Yield(s.Id);
        }

        private const string Patron = "Famille Bai";
        private const string Method = "silent-tide-sutra";

        private static PatronDesignSettings Settings => Fixtures.Content.Balance.PatronDesigns;

        private static TestWorld World(string design, out CharacterData practitioner, int patronPower = 5000)
        {
            var w = new TestWorld(new FixedRandom(0.0));
            w.Factions.AddFaction(new FactionData { Name = Patron, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 10,
                HighestRealm = CultivationRealm.GoldenCore, PowerLevel = patronPower, Techniques = { Method } });
            practitioner = w.Join(Fixtures.Cultivator(realm: CultivationRealm.PurpleMansion));
            practitioner.CultivationMethodId = Method;
            w.Accords.AddDebt(Patron, Method);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, design, 1, false) });
            return w;
        }

        private static void FallDue(TestWorld w, CharacterData practitioner)
        {
            w.Ctx.Events.TriggerBreakthroughSuccess(practitioner, CultivationRealm.PurpleMansion);
            YieldAll(w);
        }

        private static void FallDueAfterYears(TestWorld w, string design)
        {
            int years = Fixtures.Content.PatronDesigns.Single(d => d.Id == design).Years;
            w.Ctx.Clock.Restore(1 + years, GamePhase.Management);
            w.Ctx.Events.TriggerYearStarted(w.Ctx.Clock.Year);
            YieldAll(w);
        }

        [Test]
        public void TheHarvest_BindsTheClanAsAVassal_OfAStrongerPatron()
        {
            var w = World("harvest", out _);
            FallDueAfterYears(w, "harvest");
            Assert.IsTrue(w.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain && t.Faction == Patron));
        }

        [Test]
        public void TheHarvest_OfAWeakerPatron_OnlyTurnsItAgainstTheClan()
        {
            var w = World("harvest", out _, patronPower: 0);
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo));
            FallDueAfterYears(w, "harvest");
            Assert.IsFalse(w.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && t.Faction == Patron));
            Assert.AreEqual(10 - Settings.HarvestRelationLoss, w.Factions.GetFactionByName(Patron).RelationWithPlayer);
        }

        [Test]
        public void TheRefining_ConsumesThePractitionersFoundation()
        {
            var w = World("refining", out var practitioner);
            FallDue(w, practitioner);
            Assert.IsTrue(!practitioner.IsAlive && practitioner.CauseOfDeath == DeathCause.FoundationDevoured);
        }

        [Test]
        public void TheMark_LetsThePatronSeeIntoTheClan()
        {
            var w = World("mark", out var practitioner);
            FallDue(w, practitioner);
            Assert.AreEqual(Settings.MarkClues, w.Suspicion.MirrorClues(Patron));
        }

        [Test]
        public void ACleansedMark_HoldsNothing()
        {
            var w = World("mark", out var practitioner);
            w.Mirror.Restore(MirrorChronicles.Mirror.MirrorSystem.MaxMirrorPower, 0);
            w.Sponsorships.Cleanse("sp1");
            FallDue(w, practitioner);
            Assert.AreEqual(0, w.Suspicion.MirrorClues(Patron));
        }

        [Test]
        public void TheHiddenFlaw_WoundsAndCapsThePractitioner()
        {
            var w = World("hidden-flaw", out var practitioner);
            FallDue(w, practitioner);
            Assert.IsTrue(practitioner.DaoWounds == Settings.FlawWounds && practitioner.ProgressionSealed);
        }

        [Test]
        public void TheHostage_TakesTheMostGiftedChild()
        {
            var w = World("heir-hostage", out var practitioner);
            var dull = w.Join(Fixtures.Cultivator(age: 8, realm: CultivationRealm.Embryonic));
            dull.SpiritualRoot = 20;
            dull.FatherID = practitioner.ID;
            var gifted = Fixtures.Cultivator(age: 0, realm: CultivationRealm.Embryonic);
            gifted.SpiritualRoot = 90;
            gifted.FatherID = practitioner.ID;
            w.Join(gifted);
            w.Ctx.Events.TriggerCharacterBorn(gifted);
            YieldAll(w);
            Assert.IsTrue(gifted.Departed && !dull.Departed);
        }

        [Test]
        public void TheSincerePatron_ForgivesItsDebt_AndGives()
        {
            var w = World("sincere", out _);
            int stones = w.Resources.SpiritStones;
            FallDueAfterYears(w, "sincere");
            Assert.IsFalse(w.Accords.Debts.Any(d => d.Power == Patron));
            Assert.AreEqual(stones + Settings.SincereGift, w.Resources.SpiritStones);
        }
    }
}
