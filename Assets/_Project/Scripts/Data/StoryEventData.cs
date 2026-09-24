using UnityEngine;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    public enum StoryTriggerType
    {
        FirstFoundation,
        FirstGoldenCore,
        PatriarchBetrayal,
        TenGenerationsRetrospective,
        FirstAscension,
        ClanExtinctionThreat
    }

    [CreateAssetMenu(fileName = "NewStoryEvent", menuName = "MirrorChronicles/Story Event")]
    public class StoryEventData : ScriptableObject
    {
        public string EventName;
        public StoryTriggerType TriggerType;
        [TextArea(3, 6)]
        public string NarrativeText;
        public List<StoryChoice> Choices = new List<StoryChoice>();
    }

    [System.Serializable]
    public class StoryChoice
    {
        public string Label;
        [TextArea(1, 3)]
        public string Tooltip;
        public StoryOutcome Outcome;
    }

    [System.Serializable]
    public class StoryOutcome
    {
        public int StabilityChange;
        public int SpiritStoneChange;
        public int RelationChange;
        public string FactionID;
    }
}
