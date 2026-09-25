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

        /// <param name="events">The story events; null uses the built-in placeholder set.</param>
        public StoryEventManager(GameContext ctx, ClanManager clan, ResourceManager resources,
            MentalStabilitySystem stability, FactionManager factions, IEnumerable<StoryEventData> events = null)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.stability = stability;
            this.factions = factions;
            storyEvents = events?.ToList() ?? DefaultStoryEvents();

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

        /// <summary>Placeholder story until the narrative moves to data (phase G2).</summary>
        public static List<StoryEventData> DefaultStoryEvents() => new List<StoryEventData>
        {
            Story(StoryTriggerType.FirstFoundation, "A New Foundation",
                "For the first time in the clan's history, a member has established their Foundation. The path of cultivation opens wider.",
                Choice("Celebrate (+5 stability for all)", stability: 5),
                Choice("Push harder (no bonus)")),
            Story(StoryTriggerType.FirstGoldenCore, "Golden Core Formed",
                "A Golden Core cultivator walks among the clan. Other sects take notice.",
                Choice("Display strength (+200 stones)", stones: 200),
                Choice("Remain humble (+10 stability for all)", stability: 10)),
            Story(StoryTriggerType.PatriarchBetrayal, "Seeds of Betrayal",
                "A clan member's mind teeters on the edge of madness. They whisper of leaving — or worse.",
                Choice("Confront them (−100 stones, +15 stability for all)", stones: -100, stability: 15),
                Choice("Ignore it for now (−10 stability for all)", stability: -10)),
            Story(StoryTriggerType.FirstAscension, "Ascension!",
                "One of the clan has touched the threshold of the Dao. The heavens tremble.",
                Choice("Bask in glory (+500 stones, +20 stability for all)", stones: 500, stability: 20)),
            Story(StoryTriggerType.ClanExtinctionThreat, "On the Brink",
                "The clan is nearly extinct. Only one member remains. This may be the end…",
                Choice("Fight on (+30 stability)", stability: 30))
        };

        private static StoryEventData Story(StoryTriggerType type, string name, string text, params StoryChoice[] choices) =>
            new StoryEventData { TriggerType = type, Name = name, NarrativeText = text, Choices = choices.ToList() };

        private static StoryChoice Choice(string label, int stability = 0, int stones = 0) =>
            new StoryChoice { Label = label, Outcome = new StoryOutcome { StabilityChange = stability, SpiritStoneChange = stones } };
    }
}
