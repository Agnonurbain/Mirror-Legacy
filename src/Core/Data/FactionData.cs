using System;

namespace MirrorChronicles.Data
{
    public enum FactionPersonality
    {
        Aggressive,
        Merchant,
        Isolationist,
        Expansionist,
        Manipulative
    }

    /// <summary>
    /// POCO representing a rival family or sect in the world.
    /// </summary>
    [Serializable]
    public class FactionData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public FactionPersonality Personality { get; set; }
        
        // Power Level represents their overall military/cultivation strength (e.g., 100 = weak, 10000 = major sect)
        public int PowerLevel { get; set; }
        public int Wealth { get; set; }
        
        // Relationship with the player's clan (-100 to +100)
        public int RelationWithPlayer { get; set; }

        /// <summary>An independent copy (saves and loads never share factions with a live game).</summary>
        public FactionData Clone() => (FactionData)MemberwiseClone();

        public FactionData()
        {
            ID = Guid.NewGuid().ToString();
            RelationWithPlayer = 0; // Neutral start
        }
    }
}
