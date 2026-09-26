using System;
using NUnit.Framework;
using MirrorChronicles.Data;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>Spies steal fragments from rival factions, at the risk of being caught.</summary>
    [TestFixture]
    public class EspionageSystemTests
    {
        private static (TestWorld world, FactionData target, CharacterData spy) Mission(Random rng)
        {
            var w = new TestWorld(rng);
            var target = new FactionData { Name = "Rival", Personality = FactionPersonality.Aggressive, PowerLevel = 500 };
            w.Factions.AddFaction(target);
            var spy = w.Join(Fixtures.Cultivator());
            return (w, target, spy);
        }

        [Test]
        public void AttemptEspionage_StealsAFragment_WhenTheRollSucceeds()
        {
            var (w, target, spy) = Mission(new FixedRandom(0.0));
            var result = w.Espionage.AttemptEspionage(spy, target);
            Assert.IsTrue(result.Success && w.Deduction.Fragments.Count == 1);
        }

        [Test]
        public void AttemptEspionage_SoursRelationsAndShakesTheSpy_WhenCaught()
        {
            var (w, target, spy) = Mission(new FixedRandom(0.99));
            var result = w.Espionage.AttemptEspionage(spy, target);
            Assert.IsTrue(!result.Success && target.RelationWithPlayer == -20 && spy.MentalStability == 60);
        }

        [Test]
        public void AttemptEspionage_DoesNothing_WithADeadSpy()
        {
            var (w, target, spy) = Mission(new FixedRandom(0.0));
            spy.IsAlive = false;
            var result = w.Espionage.AttemptEspionage(spy, target);
            Assert.IsTrue(!result.Success && w.Deduction.Fragments.Count == 0);
        }

        // ---- Stealing a manual the power holds (L5b; LORE.md §2.4, L3b) ----

        [Test]
        public void AttemptEspionage_MayStealAManualThePowerHolds()
        {
            var (w, target, spy) = Mission(new FixedRandom(0.0));
            target.Techniques.Add("upstream-brook-method");

            var result = w.Espionage.AttemptEspionage(spy, target);

            Assert.AreEqual("upstream-brook-method", result.StolenTechniqueId);
            Assert.IsTrue(w.Techniques.Knows("upstream-brook-method"));
            Assert.AreEqual(0, w.Deduction.Fragments.Count, "a manual, not a fragment");
        }

        [Test]
        public void AttemptEspionage_TakesAFragment_WhenThePowerHoldsNothingNew()
        {
            var (w, target, spy) = Mission(new FixedRandom(0.0));
            w.Techniques.Learn("clear-spring-sutra");
            target.Techniques.Add("clear-spring-sutra"); // the clan knows it already

            var result = w.Espionage.AttemptEspionage(spy, target);

            Assert.IsNull(result.StolenTechniqueId);
            Assert.AreEqual(1, w.Deduction.Fragments.Count);
        }
    }
}
