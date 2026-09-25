using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The breakthrough to the Purple Mansion in four trials (LORE.md §5.4.1): Ascent, Manifestation (about
    /// six years), the Great Void (days to decades, sometimes for life), the Illusions. Draws, in order: the
    /// ascent's roll; at the manifestation's end its roll, then the void's band (and its years in a range);
    /// at the void's end the illusions' roll.
    /// </summary>
    [TestFixture]
    public class PurpleMansionTests
    {
        private const double Pass = 0.0;   // every roll succeeds
        private const double Fail = 0.995; // every roll fails

        /// <summary>A Foundation cultivator at the apogee with the XP to ascend, practising the Inner Sun Manual (grade 5).</summary>
        private static CharacterData AtTheApogee(TestWorld w, string method = "inner-sun-manual", string qi = "inner-sun-qi")
        {
            var c = Fixtures.Cultivator(age: 120, realm: CultivationRealm.Foundation, stage: 4);
            c.CultivationMethodId = method;
            c.QiId = qi;
            c.FoundationId = "bright-yang:first-dawn-pass";
            c.CultivationXP = PowerLadder.XpForNextStage(CultivationRealm.Foundation);
            return w.Join(c);
        }

        private static void PassYears(TestWorld w, int years)
        {
            for (int y = 0; y < years; y++) w.PurpleMansion.ProcessBreakthroughPhase();
        }

        // ---- Who may ascend ----

        [Test]
        public void Ladder_OpensTheAscentAtTheFoundationsApogee()
        {
            var step = PowerLadder.Next(CultivationRealm.Foundation, 4);
            Assert.IsTrue(step.IsAvailable && step.Trial == TrialKind.PurpleMansionAscension && step.TargetRealm == CultivationRealm.PurpleMansion);
        }

        [Test]
        public void Ascent_NeedsTheSecretOfTheMethod()
        {
            // §5.3.4: grade 4 and below lack the secret technique of ascent; most stop at the Foundation's apogee
            var w = new TestWorld();
            Assert.IsTrue(w.Cultivation.IsReadyForTrial(AtTheApogee(w)));
            Assert.IsFalse(w.Cultivation.IsReadyForTrial(AtTheApogee(w, Fixtures.ClanMethod, Fixtures.ClanQi)));
        }

        [Test]
        public void Ascent_IsOpenToTheAncestralSutraTheLoreGivesTheSecret()
        {
            var w = new TestWorld();
            Assert.IsTrue(w.Cultivation.IsReadyForTrial(AtTheApogee(w, "elder-knocking-sutra", "red-dust-court-qi")));
        }

        // ---- The four trials ----

        [Test]
        public void Ascent_Succeeds_IntoTheManifestationRetreat()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = AtTheApogee(w);

            w.PurpleMansion.ProcessBreakthroughPhase();

            Assert.AreEqual(Retreat.Manifestation, c.Retreat);
            Assert.AreEqual(w.Ctx.Content.Balance.PurpleMansion.ManifestationYears, c.RetreatYearsLeft);
            Assert.AreEqual(CultivationRealm.Foundation, c.Realm);
        }

        [Test]
        public void Ascent_Fails_AndTheCultivatorDiesOnTheSpot()
        {
            // §5.4.1: exhausted before Shenyang, one dies at once
            var w = new TestWorld(new FixedRandom(Fail));
            var c = AtTheApogee(w);

            w.PurpleMansion.ProcessBreakthroughPhase();

            Assert.IsTrue(!c.IsAlive && c.CauseOfDeath == DeathCause.AscentCollapse);
        }

        [Test]
        public void Retreat_LeavesNoOtherTask()
        {
            var w = new TestWorld(new FixedRandom(Pass));
            var c = AtTheApogee(w);
            w.PurpleMansion.ProcessBreakthroughPhase();

            CollectionAssert.AreEqual(new[] { TaskType.None }, TaskRules.AllowedTasks(c));
        }

        [Test]
        public void Manifestation_AfterItsYears_EntersTheGreatVoid()
        {
            var w = new TestWorld(new SequenceRandom(Pass, Pass, 0.1)); // the ascent, the manifestation, then a short void
            var c = AtTheApogee(w);

            PassYears(w, 1 + w.Ctx.Content.Balance.PurpleMansion.ManifestationYears);

            Assert.AreEqual(Retreat.GreatVoid, c.Retreat);
        }

        [Test]
        public void Manifestation_Fails_BackToTheFoundationsApogee()
        {
            // 🔎 « the main cause of falls »: the breakthrough fails, the cultivator lives
            var w = new TestWorld(new SequenceRandom(Pass, Fail)); // the ascent passes, the manifestation fails
            var c = AtTheApogee(w);

            PassYears(w, 1 + w.Ctx.Content.Balance.PurpleMansion.ManifestationYears);

            Assert.IsTrue(c.IsAlive && c.Retreat == Retreat.None && c.Realm == CultivationRealm.Foundation && c.RealmStage == 4 && c.CultivationXP == 0);
        }

        [Test]
        public void GreatVoid_CanHoldACultivatorForLife()
        {
            var w = new TestWorld(new SequenceRandom(Pass, Pass, 0.999)); // past the last band: prisoner for life
            var c = AtTheApogee(w);

            PassYears(w, 1 + w.Ctx.Content.Balance.PurpleMansion.ManifestationYears + 100);

            Assert.IsTrue(c.ImprisonedInVoid && c.Retreat == Retreat.GreatVoid && c.Realm == CultivationRealm.Foundation);
        }

        [Test]
        public void Illusions_Pass_IntoThePurpleMansion_WithTheFoundationAsFirstAbility()
        {
            var w = new TestWorld(new FixedRandom(Pass)); // every trial passes; the void lasts less than a year
            var c = AtTheApogee(w);

            PassYears(w, 2 + w.Ctx.Content.Balance.PurpleMansion.ManifestationYears);

            Assert.IsTrue(c.Realm == CultivationRealm.PurpleMansion && c.RealmStage == 1 && c.Retreat == Retreat.None);
            CollectionAssert.AreEqual(new[] { "bright-yang:first-dawn-pass" }, c.DivineAbilities);
        }

        [Test]
        public void Illusions_Fail_AndTheCultivatorLosesAllCultivation()
        {
            // §5.4.1: failing the illusions costs one's whole cultivation
            var w = new TestWorld(new SequenceRandom(Pass, Pass, 0.1, Fail)); // a short void, then the illusions fail
            var c = AtTheApogee(w);

            PassYears(w, 2 + w.Ctx.Content.Balance.PurpleMansion.ManifestationYears);

            Assert.IsTrue(c.Realm == CultivationRealm.Embryonic && c.RealmStage == 0 && c.FoundationId == null && c.Retreat == Retreat.None);
        }
    }
}
