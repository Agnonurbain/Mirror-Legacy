using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// One-time story events raised by milestones (first Foundation, first Golden Core, an ascension,
    /// a mind on the brink, the last survivor). They wait in line until the player chooses.
    /// </summary>
    public sealed class StoryEventManager
    {
        public const int BetrayalThreshold = 20;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly FactionManager factions;
        private readonly List<StoryEventData> storyEvents;
        private readonly HashSet<StoryTriggerType> triggered = new HashSet<StoryTriggerType>();
        private readonly Queue<StoryEventData> pending = new Queue<StoryEventData>();

        public StoryEventData PendingEvent => pending.Count > 0 ? pending.Peek() : null;
        public IReadOnlyCollection<StoryTriggerType> TriggeredEvents => triggered;
        public IEnumerable<StoryTriggerType> PendingTriggers => pending.Select(e => e.TriggerType);

        /// <param name="events">The story events; null uses the game's content (story.json).</param>
        public StoryEventManager(GameContext ctx, ClanManager clan, ResourceManager resources,
            MentalStabilitySystem stability, FactionManager factions, IEnumerable<StoryEventData> events = null)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.stability = stability;
            this.factions = factions;
            storyEvents = (events ?? ctx.Content.StoryEvents).ToList();

            ctx.Events.OnBreakthroughSuccess += (c, realm) =>
            {
                if (realm == CultivationRealm.Foundation) TryTrigger(StoryTriggerType.FirstFoundation);
                else if (realm == CultivationRealm.GoldenCore) TryTrigger(StoryTriggerType.FirstGoldenCore);
                else if (realm == CultivationRealm.DaoEmbryo) TryTrigger(StoryTriggerType.FirstAscension);
            };
            ctx.Events.OnCharacterDied += (c, cause) =>
            {
                if (clan.LivingMembers.Count <= 1) TryTrigger(StoryTriggerType.ClanExtinctionThreat);
            };
            ctx.Events.OnPhaseChanged += phase =>
            {
                if (phase == GamePhase.Events && clan.LivingMembers.Any(m => m.MentalStability < BetrayalThreshold))
                    TryTrigger(StoryTriggerType.PatriarchBetrayal);
            };
        }

        /// <summary>Applies the chosen outcome of the first waiting event. Returns false for an invalid choice.</summary>
        public bool ResolveChoice(int choiceIndex)
        {
            var evt = PendingEvent;
            if (evt == null || choiceIndex < 0 || choiceIndex >= evt.Choices.Count) return false;

            pending.Dequeue();
            var choice = evt.Choices[choiceIndex];
            var outcome = choice.Outcome;

            if (outcome.StabilityChange != 0)
                foreach (var m in clan.LivingMembers.ToList())
                    stability.ApplyModifier(m, outcome.StabilityChange);

            if (outcome.SpiritStoneChange > 0) resources.AddSpiritStones(outcome.SpiritStoneChange);
            else if (outcome.SpiritStoneChange < 0) resources.ConsumeSpiritStones(-outcome.SpiritStoneChange);

            if (outcome.RelationChange != 0 && !string.IsNullOrEmpty(outcome.FactionID))
                factions.ChangeRelation(outcome.FactionID, outcome.RelationChange);

            ctx.Log.Info($"[Story] {evt.Name}: the clan chooses \"{choice.Label}\".");
            return true;
        }

        /// <summary>Restores which events already happened and which still wait for a choice.</summary>
        public void Restore(IEnumerable<StoryTriggerType> alreadyTriggered, IEnumerable<StoryTriggerType> waiting)
        {
            triggered.Clear();
            pending.Clear();
            foreach (var t in alreadyTriggered) triggered.Add(t);
            foreach (var t in waiting)
            {
                var evt = storyEvents.Find(e => e.TriggerType == t);
                if (evt != null) pending.Enqueue(evt);
            }
        }

        private void TryTrigger(StoryTriggerType type)
        {
            if (triggered.Contains(type)) return;
            var evt = storyEvents.Find(e => e.TriggerType == type);
            if (evt == null) return;

            triggered.Add(type);
            pending.Enqueue(evt);
            ctx.Log.Info($"[Story] {evt.Name}: {evt.NarrativeText}");
            ctx.Events.TriggerStoryEventRaised(evt);
        }
    }
}
