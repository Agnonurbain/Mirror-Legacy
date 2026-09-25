using System;
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

    /// <summary>A one-time story event: a narrative and the choices offered to the player.</summary>
    [Serializable]
    public class StoryEventData
    {
        public string Name { get; set; }
        public StoryTriggerType TriggerType { get; set; }
        public string NarrativeText { get; set; }
        public List<StoryChoice> Choices { get; set; } = new List<StoryChoice>();
    }

    [Serializable]
    public class StoryChoice
    {
        public string Label { get; set; }
        public string Tooltip { get; set; }
        public StoryOutcome Outcome { get; set; } = new StoryOutcome();
    }

    /// <summary>What a choice changes: stability of every member, stones, relation with one faction.</summary>
    [Serializable]
    public class StoryOutcome
    {
        public int StabilityChange { get; set; }
        public int SpiritStoneChange { get; set; }
        public int RelationChange { get; set; }
        public string FactionID { get; set; }
    }
}
