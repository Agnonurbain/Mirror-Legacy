using UnityEngine;

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

    [CreateAssetMenu(fileName = "NewEvent", menuName = "MirrorChronicles/Random Event")]
    public class RandomEventData : ScriptableObject
    {
        public string EventName;
        public RandomEventType EventType;
        [TextArea(2, 4)]
        public string Description;
        [Range(1, 100)]
        public int Weight = 10;
        public CultivationRealm MinPatriarchRealm;
        public int MinYear;
    }
}
