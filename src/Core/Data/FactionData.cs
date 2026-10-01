using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<FactionElder> Elders { get; set; } = new List<FactionElder>(); // who it counts: they age, die and rise (2026-10-01)
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
            copy.Elders = (Elders ?? new List<FactionElder>()).Select(e => e.Clone()).ToList();
            copy.InterpretedFields = new List<string>(InterpretedFields ?? new List<string>());
            return copy;
        }

        public FactionData()
        {
            ID = Guid.NewGuid().ToString();
            RelationWithPlayer = 0; // Neutral start
        }
    }

    /// <summary>
    /// An elder of a power (the living world, step A, 2026-10-01): a named figure (figures.json) or one the world drew. Its
    /// age runs from its birth year; it dies at its lifespan's end; it rises in time; at the Purple Mansion's peak its own
    /// odds of the Golden Core decide whether — and when — it dares.
    /// </summary>
    public sealed class FactionElder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FigureId { get; set; }            // the named figure it is, or null
        public CultivationRealm Realm { get; set; }
        public int Stage { get; set; }
        public int BornYear { get; set; }
        public int MaxLifespan { get; set; }
        public int RealmSinceYear { get; set; }          // when it reached its realm
        public double GoldenCoreOdds { get; set; }       // at the Purple Mansion: its odds of forging and claiming a position

        public int Age(int year) => year - BornYear;

        public FactionElder Clone() => (FactionElder)MemberwiseClone();
    }
}
