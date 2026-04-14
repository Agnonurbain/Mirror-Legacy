using System;

namespace MirrorChronicles.Data
{
    public enum TechniqueType
    {
        CultivationMethod, // Boosts XP gain
        MartialArt,        // Used in combat
        SupportArt         // Healing or buffing
    }

    /// <summary>
    /// Represents a fragment of knowledge found in ruins or stolen from rivals.
    /// Used in the Deduction Engine to create new techniques.
    /// </summary>
    [Serializable]
    public class FragmentData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public Element Element { get; set; }
        public int Quality { get; set; } // 1 (Common) to 5 (Divine)

        public FragmentData()
        {
            ID = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Represents a fully realized technique created by the Deduction Engine.
    /// </summary>
    [Serializable]
    public class TechniqueData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public TechniqueType Type { get; set; }
        public Element DominantElement { get; set; }
        public CultivationRealm RequiredRealm { get; set; }
        
        // Stats
        public int PowerModifier { get; set; } // Damage or XP boost
        public int QiCost { get; set; }
        public int RiskFactor { get; set; } // 0-100% chance of Qi Deviation when practicing

        public TechniqueData()
        {
            ID = Guid.NewGuid().ToString();
        }
    }
}
