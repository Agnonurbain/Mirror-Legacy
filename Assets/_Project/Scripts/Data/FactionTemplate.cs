using UnityEngine;

namespace MirrorChronicles.Data
{
    [CreateAssetMenu(fileName = "NewFaction", menuName = "MirrorChronicles/Faction Template")]
    public class FactionTemplate : ScriptableObject
    {
        public string FactionName;
        public FactionPersonality Personality;
        public int PowerLevel = 500;
        public int Wealth = 1000;
        public int StartingRelation;
        [TextArea(1, 3)]
        public string Description;
    }
}
