using System;
using System.Collections.Generic;

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

    /// <summary>What a power of the world is (LORE.md §7-§10).</summary>
    public enum FactionKind { Family, Sect, Gate, ImmortalIsland, Temple, Order, State }

    /// <summary>
    /// A power of the world (factions.json, LORE.md §7-§10): a family, a sect, a gate, a temple, an order or a
    /// state, where it lives on the map, its Dao, the highest realm it reaches and the techniques it holds.
    /// </summary>
    [Serializable]
    public class FactionData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; } // the ruling family's surname, given to the spouses it sends; null for sects
        public FactionKind Kind { get; set; }
        public string RegionId { get; set; }                  // a regions.json id
        public CultivationPath Path { get; set; }
        public CultivationRealm HighestRealm { get; set; }    // the strongest cultivator it counts
        public List<string> Techniques { get; set; } = new List<string>(); // techniques.json ids it holds (LORE.md §2.4)
        public string Notes { get; set; }
        public Provenance Provenance { get; set; }
        public List<string> InterpretedFields { get; set; } = new List<string>(); // to replace when a source speaks
        public FactionPersonality Personality { get; set; }
        
        // Power Level represents their overall military/cultivation strength (e.g., 100 = weak, 10000 = major sect)
        public int PowerLevel { get; set; }
        public int Wealth { get; set; }
        
        // Relationship with the player's clan (-100 to +100)
        public int RelationWithPlayer { get; set; }

        /// <summary>An independent copy (saves and loads never share factions with a live game).</summary>
        public FactionData Clone()
        {
            var copy = (FactionData)MemberwiseClone();
            copy.Techniques = new List<string>(Techniques ?? new List<string>());
            copy.InterpretedFields = new List<string>(InterpretedFields ?? new List<string>());
            return copy;
        }

        public FactionData()
        {
            ID = Guid.NewGuid().ToString();
            RelationWithPlayer = 0; // Neutral start
        }
    }
}
