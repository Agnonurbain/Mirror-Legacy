using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// What the other seventeen patron designs do when they fall due (LORE.md §11.10, C; second wave, 2026-10-01).
    /// </summary>
    [TestFixture]
    public class PatronDesignEffectsWave2Tests
    {
        private const string Patron = "Famille Bai";
        private const string Other = "Famille Tao";
        private const string Method = "silent-tide-sutra";

        private static PatronDesignSettings Settings => Fixtures.Content.Balance.PatronDesigns;

        private static TestWorld World(string design, out CharacterData practitioner, double roll = 0.0, CultivationRealm realm = CultivationRealm.PurpleMansion)
        {
            var w = new TestWorld(new FixedRandom(roll));
            w.Factions.AddFaction(new FactionData { Name = Patron, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 10,
                HighestRealm = CultivationRealm.GoldenCore, PowerLevel = 5000, Techniques = { Method } });
            w.Factions.AddFaction(new FactionData { Name = Other, Kind = FactionKind.Family, RegionId = "jingshui-lake", PowerLevel = 3000 });
            w.Factions.AddFaction(new FactionData { Name = "Empire de Kun", Kind = FactionKind.State, RegionId = "kun" });
            practitioner = w.Join(Fixtures.Cultivator(realm: realm));
            practitioner.CultivationMethodId = Method;
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, design, 1, false) });
            return w;
        }

        private static void Due(TestWorld w, string design, CharacterData practitioner)
        {
            var d = Fixtures.Content.PatronDesigns.Single(x => x.Id == design);
            switch (d.Due)
            {
                case DesignDue.PurpleMansion: w.Ctx.Events.TriggerBreakthroughSuccess(practitioner, CultivationRealm.PurpleMansion); break;
                case DesignDue.GoldenCore: w.Ctx.Events.TriggerBreakthroughSuccess(practitioner, CultivationRealm.GoldenCore); break;
                case DesignDue.Years:
                    w.Ctx.Clock.Restore(1 + d.Years, GamePhase.Management);
                    w.Ctx.Events.TriggerYearStarted(w.Ctx.Clock.Year);
                    break;
                case DesignDue.HeirBorn:
                    var child = Fixtures.Cultivator(age: 0, realm: CultivationRealm.Embryonic);
                    child.FatherID = practitioner.ID;
                    child.SpiritualRoot = 10;
                    w.Join(child);
                    w.Ctx.Events.TriggerCharacterBorn(child);
                    break;
            }
        }

        private static CharacterData Child(TestWorld w, int root)
        {
            var c = w.Join(Fixtures.Cultivator(age: 10, realm: CultivationRealm.Embryonic));
            c.SpiritualRoot = root;
            return c;
        }

        [Test]
        public void EveryDesign_HasAnEffect()
        {
            Assert.IsTrue(Fixtures.Content.PatronDesigns.All(d => PatronDesignEffects.Handles(d.Id)));
        }

        [Test]
        public void HumanPills_TakeCultivatorsOfTheClan()
        {
            var w = World("human-pills", out var p);
            for (int i = 0; i < 3; i++) w.Join(Fixtures.Cultivator());
            int before = w.Clan.LivingMembers.Count;
            Due(w, "human-pills", p);
            Assert.AreEqual(before - Settings.HumanPillsTaken, w.Clan.LivingMembers.Count);
        }

        [Test]
        public void TheVessel_ReplacesThePractitionersSoul()
        {
            var w = World("vessel", out var p, realm: CultivationRealm.GoldenCore);
            Due(w, "vessel", p);
            Assert.AreEqual(DeathCause.SoulReplaced, p.CauseOfDeath);
        }

        [Test]
        public void TheFatedOne_LosesHisDestiny()
        {
            var w = World("fated-one", out var p);
            var gifted = Child(w, 90);
            Due(w, "fated-one", p);
            Assert.AreEqual((int)(90 * Settings.FatedRootShare), gifted.SpiritualRoot);
        }

        [Test]
        public void TheFruitionVassal_IsSealedToThePatronsLeave()
        {
            var w = World("fruition-vassal", out var p, realm: CultivationRealm.GoldenCore);
            Due(w, "fruition-vassal", p);
            Assert.IsTrue(p.ProgressionSealed);
        }

        [Test]
        public void TheBorrowingPyramid_BindsTheSoul()
        {
            var w = World("borrowing-pyramid", out var p);
            p.MentalStability = 80;
            Due(w, "borrowing-pyramid", p);
            Assert.AreEqual(80 + MirrorChronicles.Characters.MentalStabilitySystem.BreakthroughSuccessBonus - Settings.PyramidStability, p.MentalStability);
            Assert.Greater(w.Suspicion.MirrorClues(Patron), 0);
        }

        [Test]
        public void ThePawn_TurnsAThirdPowerAgainstTheClan()
        {
            var w = World("pawn", out var p);
            Due(w, "pawn", p);
            Assert.AreEqual(Settings.PawnSuspicion, w.Suspicion.OfClan(Other));
        }

        [Test]
        public void TheScapegoat_DrawsEveryonesSuspicion()
        {
            var w = World("scapegoat", out var p);
            Due(w, "scapegoat", p);
            Assert.IsTrue(w.Factions.Factions.Where(f => f.Name != Patron).All(f => w.Suspicion.OfClan(f.Name) == Settings.ScapegoatSuspicion));
        }

        [Test]
        public void TheBulwark_WoundsTheStrongest_ForPrestige()
        {
            var w = World("bulwark", out var p);
            int prestige = w.Resources.Prestige;
            Due(w, "bulwark", p);
            Assert.IsTrue(p.DaoWounds == 1 && w.Resources.Prestige == prestige + Settings.BulwarkPrestige);
        }

        [Test]
        public void TheAtmosphere_RuinsTheRegion()
        {
            var w = World("atmosphere", out var p);
            w.Resources.SetSpiritStones(1000);
            Due(w, "atmosphere", p);
            Assert.AreEqual(1000 - (int)(1000 * Settings.AtmosphereStonesShare), w.Resources.SpiritStones);
        }

        [Test]
        public void TheImperialBlood_DrawsTheStatesEyes()
        {
            var w = World("imperial-blood", out var p);
            Due(w, "imperial-blood", p);
            Assert.IsTrue(w.Suspicion.OfClan("Empire de Kun") == Settings.ImperialSuspicion && w.Suspicion.OfClan(Other) == 0);
        }

        [Test]
        public void TheVacantFruition_BindsTheClanThroughItsTrueMonarch()
        {
            var w = World("vacant-fruition", out var p, realm: CultivationRealm.GoldenCore);
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.DaoEmbryo)); // stronger than the patron: still bound
            Due(w, "vacant-fruition", p);
            Assert.IsTrue(w.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain && t.Faction == Patron));
        }

        [Test]
        public void TheIntercalaryBridge_CostsADivineAbility()
        {
            var w = World("intercalary-bridge", out var p, realm: CultivationRealm.GoldenCore);
            p.DivineAbilities.AddRange(new[] { "a", "b" });
            Due(w, "intercalary-bridge", p);
            CollectionAssert.AreEqual(new[] { "a" }, p.DivineAbilities);
        }

        [TestCase("underworld-essence")]
        [TestCase("demon-to-feed")]
        public void AnAscentThatSucceeds_ThwartsTheDesign_AndThePatronResentsIt(string design)
        {
            var w = World(design, out var p, realm: CultivationRealm.GoldenCore);
            Due(w, design, p);
            Assert.AreEqual(10 - Settings.HarvestRelationLoss, w.Factions.GetFactionByName(Patron).RelationWithPlayer);
        }

        [Test]
        public void TheVoidKey_MayKillThePractitioner()
        {
            var w = World("void-key", out var p, roll: 0.0);
            Due(w, "void-key", p);
            Assert.IsFalse(p.IsAlive);
        }

        [Test]
        public void TheVoidKey_MayBringATreasureBack()
        {
            var w = World("void-key", out var p, roll: 0.99);
            int stones = w.Resources.SpiritStones;
            Due(w, "void-key", p);
            Assert.IsTrue(p.IsAlive && w.Resources.SpiritStones == stones + Settings.VoidKeyTreasure);
        }

        [Test]
        public void TheMirrorBait_TightensThePatronsHunt()
        {
            var w = World("mirror-bait", out var p);
            Due(w, "mirror-bait", p);
            int expected = w.Lore.Knows(Patron) ? Fixtures.Content.Balance.Plots.DoubtClues : Settings.MarkClues; // a knower comes to doubt at once
            Assert.AreEqual(expected, w.Suspicion.MirrorClues(Patron));
        }

        [Test]
        public void TheBrokenPact_EndsThePactWithTheVixen()
        {
            var w = World("broken-pact", out var p);
            w.Patrons.RestorePacts(new[] { new PatronPact("qingyan-vixen", 1, 50) });
            Due(w, "broken-pact", p);
            Assert.IsEmpty(w.Patrons.Pacts);
        }
    }
}
