using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Tests.Events
{
    /// <summary>The yearly random event: a weighted draw among the eligible entries of the table.</summary>
    [TestFixture]
    public class EventManagerTests
    {
        private static RandomEventData Entry(RandomEventType type, int weight = 10, int minYear = 0,
            CultivationRealm minRealm = CultivationRealm.Embryonic) =>
            new RandomEventData { Name = type.ToString(), EventType = type, Weight = weight, MinYear = minYear, MinPatriarchRealm = minRealm };

        private static (TestWorld world, EventManager events) With(Random rng, params RandomEventData[] table)
        {
            var w = new TestWorld(rng);
            var events = new EventManager(w.Ctx, w.Clan, w.Factions, w.Deduction, w.Resources, w.Stability, w.Buildings, table);
            return (w, events);
        }

        [Test]
        public void DefaultTable_HoldsElevenEvents()
        {
            var w = new TestWorld();
            var events = new EventManager(w.Ctx, w.Clan, w.Factions, w.Deduction, w.Resources, w.Stability, w.Buildings);
            Assert.AreEqual(11, events.EventTable.Count);
        }

        [Test]
        public void GetEligibleEvents_WaitsForTheMinimumYear()
        {
            var (_, events) = With(new Random(1), Entry(RandomEventType.GeniusBirth, minYear: 5));
            Assert.IsEmpty(events.GetEligibleEvents());
        }

        [Test]
        public void GetEligibleEvents_WaitsForAMemberOfTheMinimumRealm()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.RivalChallenge, minRealm: CultivationRealm.Foundation));
            w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement));
            Assert.IsEmpty(events.GetEligibleEvents());
        }

        [TestCase(0.0, RandomEventType.PeacefulYear)]
        [TestCase(0.5, RandomEventType.WanderingMerchant)]
        public void TriggerYearlyEvent_DrawsByWeight(double sample, RandomEventType expected)
        {
            var (_, events) = With(new FixedRandom(sample),
                Entry(RandomEventType.PeacefulYear, weight: 1), Entry(RandomEventType.WanderingMerchant, weight: 3));
            Assert.AreEqual(expected, events.TriggerYearlyEvent().EventType);
        }

        [Test]
        public void TriggerYearlyEvent_ReturnsNull_WhenNothingIsEligible()
        {
            var (_, events) = With(new Random(1));
            Assert.IsNull(events.TriggerYearlyEvent());
        }

        [Test]
        public void TriggerYearlyEvent_RaisesTheEvent()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.PeacefulYear));
            RandomEventData raised = null;
            w.Ctx.Events.OnRandomEventOccurred += e => raised = e;
            events.TriggerYearlyEvent();
            Assert.AreEqual(RandomEventType.PeacefulYear, raised.EventType);
        }

        [Test]
        public void NaturalDisaster_CostsStones()
        {
            var (w, events) = With(new FixedRandom(0.0), Entry(RandomEventType.NaturalDisaster));
            events.TriggerYearlyEvent();
            Assert.AreEqual(950, w.Resources.SpiritStones); // loss 50-199, here 50
        }

        [Test]
        public void NaturalDisaster_IsSoftenedByTheProtectiveFormation()
        {
            var (w, events) = With(new FixedRandom(0.0), Entry(RandomEventType.NaturalDisaster));
            w.Buildings.Restore(new[] { new BuildingData(BuildingType.ProtectiveFormation) { Level = 2 } });
            events.TriggerYearlyEvent();
            Assert.AreEqual(965, w.Resources.SpiritStones); // 50 × (1 − 2 × 15%)
        }

        [Test]
        public void Epidemic_ShakesTheLowRealms()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.Epidemic));
            var disciple = w.Join(Fixtures.Cultivator(realm: CultivationRealm.QiRefinement));
            var master = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation));
            events.TriggerYearlyEvent();
            Assert.IsTrue(disciple.MentalStability == 65 && master.MentalStability == 70);
        }

        [Test]
        public void RuinsDiscovery_YieldsAFragment()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.RuinsDiscovery));
            events.TriggerYearlyEvent();
            Assert.AreEqual(1, w.Deduction.Fragments.Count);
        }

        [Test]
        public void DiplomaticVisit_WarmsAFaction()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.DiplomaticVisit));
            var faction = new FactionData { Name = "Visitors" };
            w.Factions.AddFaction(faction);
            events.TriggerYearlyEvent();
            Assert.AreEqual(10, faction.RelationWithPlayer);
        }

        [Test]
        public void InternalBetrayal_DeepensTheMostUnstableMembersFall()
        {
            var (w, events) = With(new Random(1), Entry(RandomEventType.InternalBetrayal));
            var troubled = w.Join(Fixtures.Cultivator());
            troubled.MentalStability = 25;
            w.Join(Fixtures.Cultivator());
            events.TriggerYearlyEvent();
            Assert.AreEqual(10, troubled.MentalStability);
        }
    }
}
