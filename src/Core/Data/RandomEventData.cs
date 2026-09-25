using System;

namespace MirrorChronicles.Data
{
    public enum RandomEventType
    {
        MonsterAttack,
        DiplomaticVisit,
        RuinsDiscovery,
        GeniusBirth,
        InternalBetrayal,
        NaturalDisaster,
        WanderingMerchant,
        RivalChallenge,
        Epidemic,
        MarriageOpportunity,
        PeacefulYear
    }

    /// <summary>One entry of the yearly random event table (weighted draw in the Events phase).</summary>
    [Serializable]
    public class RandomEventData
    {
        public string Name { get; set; }
        public RandomEventType EventType { get; set; }
        public string Description { get; set; }
        public int Weight { get; set; } = 10;                 // 1-100
        public CultivationRealm MinPatriarchRealm { get; set; } // some member must have reached it
        public int MinYear { get; set; }
    }
}
