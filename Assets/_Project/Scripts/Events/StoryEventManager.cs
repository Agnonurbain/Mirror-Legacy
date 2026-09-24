using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Economy;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// Monitors game state and triggers one-time story events when conditions are met.
    /// </summary>
    public class StoryEventManager : MonoBehaviour
    {
        public static StoryEventManager Instance { get; private set; }

        [Header("Story Events")]
        public List<StoryEventData> StoryEvents = new List<StoryEventData>();

        public StoryEventData PendingEvent { get; private set; }

        private readonly HashSet<StoryTriggerType> _triggeredEvents = new HashSet<StoryTriggerType>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnBreakthroughSuccess += HandleBreakthroughSuccess;
            GameEvents.OnCharacterDied += HandleDeath;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBreakthroughSuccess -= HandleBreakthroughSuccess;
            GameEvents.OnCharacterDied -= HandleDeath;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandleBreakthroughSuccess(CharacterData character, CultivationRealm newRealm)
        {
            if (newRealm == CultivationRealm.Foundation)
                TryTrigger(StoryTriggerType.FirstFoundation);
            else if (newRealm == CultivationRealm.GoldenCore)
                TryTrigger(StoryTriggerType.FirstGoldenCore);
            else if (newRealm == CultivationRealm.DaoEmbryo)
                TryTrigger(StoryTriggerType.FirstAscension);
        }

        private void HandleDeath(CharacterData character, DeathCause cause)
        {
            if (ClanManager.Instance != null && ClanManager.Instance.LivingMembers.Count <= 1)
                TryTrigger(StoryTriggerType.ClanExtinctionThreat);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Events) return;

            // Check for betrayal: any member with MS < 20
            if (ClanManager.Instance != null)
            {
                foreach (var m in ClanManager.Instance.LivingMembers)
                {
                    if (m.MentalStability < 20)
                    {
                        TryTrigger(StoryTriggerType.PatriarchBetrayal);
                        break;
                    }
                }
            }
        }

        private void TryTrigger(StoryTriggerType triggerType)
        {
            if (_triggeredEvents.Contains(triggerType)) return;

            var storyEvent = StoryEvents.Find(e => e != null && e.TriggerType == triggerType);
            if (storyEvent == null)
            {
                storyEvent = CreateDefaultStoryEvent(triggerType);
                if (storyEvent == null) return;
            }

            _triggeredEvents.Add(triggerType);
            PendingEvent = storyEvent;
            Debug.Log($"[StoryEventManager] STORY EVENT: {storyEvent.EventName} — {storyEvent.NarrativeText}");
        }

        public void ResolveChoice(int choiceIndex)
        {
            if (PendingEvent == null || choiceIndex < 0 || choiceIndex >= PendingEvent.Choices.Count) return;

            var choice = PendingEvent.Choices[choiceIndex];
            var outcome = choice.Outcome;

            if (outcome.StabilityChange != 0 && ClanManager.Instance != null)
            {
                foreach (var m in ClanManager.Instance.LivingMembers)
                    MentalStabilitySystem.Instance?.ApplyModifier(m, outcome.StabilityChange);
            }

            if (outcome.SpiritStoneChange > 0)
                ResourceManager.Instance?.AddSpiritStones(outcome.SpiritStoneChange);
            else if (outcome.SpiritStoneChange < 0)
                ResourceManager.Instance?.ConsumeSpiritStones(-outcome.SpiritStoneChange);

            if (outcome.RelationChange != 0 && !string.IsNullOrEmpty(outcome.FactionID))
                Diplomacy.FactionManager.Instance?.ChangeRelation(outcome.FactionID, outcome.RelationChange);

            Debug.Log($"[StoryEventManager] Player chose: {choice.Label}");
            PendingEvent = null;
        }

        private StoryEventData CreateDefaultStoryEvent(StoryTriggerType type)
        {
            var evt = ScriptableObject.CreateInstance<StoryEventData>();
            evt.TriggerType = type;

            switch (type)
            {
                case StoryTriggerType.FirstFoundation:
                    evt.EventName = "A New Foundation";
                    evt.NarrativeText = "For the first time in your clan's history, a member has established their Foundation! The path of cultivation opens wider.";
                    evt.Choices.Add(new StoryChoice { Label = "Celebrate (+5 MS all)", Outcome = new StoryOutcome { StabilityChange = 5 } });
                    evt.Choices.Add(new StoryChoice { Label = "Push harder (no bonus)", Outcome = new StoryOutcome() });
                    break;

                case StoryTriggerType.FirstGoldenCore:
                    evt.EventName = "Golden Core Formed";
                    evt.NarrativeText = "A Golden Core cultivator walks among your clan! Other sects take notice.";
                    evt.Choices.Add(new StoryChoice { Label = "Display strength (+200 stones)", Outcome = new StoryOutcome { SpiritStoneChange = 200 } });
                    evt.Choices.Add(new StoryChoice { Label = "Remain humble (+10 MS)", Outcome = new StoryOutcome { StabilityChange = 10 } });
                    break;

                case StoryTriggerType.PatriarchBetrayal:
                    evt.EventName = "Seeds of Betrayal";
                    evt.NarrativeText = "A clan member's mind teeters on the edge of madness. They whisper of leaving — or worse.";
                    evt.Choices.Add(new StoryChoice { Label = "Confront them (-100 stones, +15 MS)", Outcome = new StoryOutcome { SpiritStoneChange = -100, StabilityChange = 15 } });
                    evt.Choices.Add(new StoryChoice { Label = "Ignore for now (-10 MS all)", Outcome = new StoryOutcome { StabilityChange = -10 } });
                    break;

                case StoryTriggerType.FirstAscension:
                    evt.EventName = "Ascension!";
                    evt.NarrativeText = "One of your own has touched the threshold of the Dao! The heavens tremble.";
                    evt.Choices.Add(new StoryChoice { Label = "Bask in glory (+500 stones, +20 MS)", Outcome = new StoryOutcome { SpiritStoneChange = 500, StabilityChange = 20 } });
                    break;

                case StoryTriggerType.ClanExtinctionThreat:
                    evt.EventName = "On the Brink";
                    evt.NarrativeText = "The clan is nearly extinct. Only one member remains. This may be the end...";
                    evt.Choices.Add(new StoryChoice { Label = "Fight on (+30 MS)", Outcome = new StoryOutcome { StabilityChange = 30 } });
                    break;

                default:
                    return null;
            }

            return evt;
        }
    }
}
