using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Tests.Events
{
    /// <summary>One-time story events raised by milestones, and the player's choices.</summary>
    [TestFixture]
    public class StoryEventManagerTests
    {
        private static (TestWorld world, StoryEventManager story) Build()
        {
            var w = new TestWorld();
            return (w, new StoryEventManager(w.Ctx, w.Clan, w.Resources, w.Stability, w.Factions));
        }

        [Test]
        public void FirstFoundation_QueuesItsStoryEvent()
        {
            var (w, story) = Build();
            w.Ctx.Events.TriggerBreakthroughSuccess(w.Join(Fixtures.Cultivator()), CultivationRealm.Foundation);
            Assert.AreEqual(StoryTriggerType.FirstFoundation, story.PendingEvent.TriggerType);
        }

        [Test]
        public void AStoryEvent_OnlyHappensOnce()
        {
            var (w, story) = Build();
            var c = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);
            story.ResolveChoice(0);
            w.Ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);
            Assert.IsNull(story.PendingEvent);
        }

        [Test]
        public void StoryEvents_WaitInLine()
        {
            var (w, story) = Build();
            var c = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation);
            w.Ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.GoldenCore);
            story.ResolveChoice(0);
            Assert.AreEqual(StoryTriggerType.FirstGoldenCore, story.PendingEvent.TriggerType);
        }

        [Test]
        public void ResolveChoice_SteadiesEveryone_WhenTheClanCelebrates()
        {
            var (w, story) = Build();
            var c = w.Join(Fixtures.Cultivator());
            w.Ctx.Events.TriggerBreakthroughSuccess(c, CultivationRealm.Foundation); // +10 from the breakthrough itself
            story.ResolveChoice(0);                                                  // Celebrate: +5 for all
            Assert.AreEqual(85, c.MentalStability);
        }

        [Test]
        public void ResolveChoice_PaysTheStones_ItCosts()
        {
            var (w, story) = Build();
            var troubled = w.Join(Fixtures.Cultivator());
            troubled.MentalStability = 10;
            w.Ctx.Events.TriggerPhaseChanged(GamePhase.Events); // betrayal looms
            story.ResolveChoice(0);                             // Confront: −100 stones
            Assert.AreEqual(900, w.Resources.SpiritStones);
        }

        [Test]
        public void LastSurvivor_RaisesTheExtinctionThreat()
        {
            var (w, story) = Build();
            var doomed = w.Join(Fixtures.Cultivator());
            w.Join(Fixtures.Cultivator());
            w.Clan.Kill(doomed, DeathCause.Illness);
            Assert.AreEqual(StoryTriggerType.ClanExtinctionThreat, story.PendingEvent.TriggerType);
        }

        [Test]
        public void StoryEvent_IsAnnouncedOnTheBus()
        {
            var (w, story) = Build();
            StoryEventData raised = null;
            w.Ctx.Events.OnStoryEventRaised += e => raised = e;
            w.Ctx.Events.TriggerBreakthroughSuccess(w.Join(Fixtures.Cultivator()), CultivationRealm.GoldenCore);
            Assert.AreEqual(StoryTriggerType.FirstGoldenCore, raised.TriggerType);
        }
    }
}
