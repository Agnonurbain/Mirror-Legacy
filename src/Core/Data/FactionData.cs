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
        public List<ArtifactInstance> Artifacts { get; set; } = new List<ArtifactInstance>(); // forged, stolen, bought or borrowed from the clan (L4f)
        public List<RankDesignation> Designations { get; set; } = new List<RankDesignation>(); // its holders' treasures mortgaged on their Fruitions
        public string SovereignId { get; set; }       // a kingdom's sovereign, its highest elder (the Imperial Way, 2026-10-03)
        public int ImperialMerit { get; set; }        // what governing has added to its sovereign's odds (points)
        public double DomainStrength { get; set; }   // what its treasures, Designations and artifacts add to its war strength (recomputed yearly)
        public List<string> Techniques { get; set; } = new List<string>(); // techniques.json ids it holds (LORE.md §2.4)
        public List<FactionElder> Elders { get; set; } = new List<FactionElder>(); // who it counts: they age, die and rise (2026-10-01)
        public int BaselinePower { get; set; }       // its size when its elders were first weighed (PowerEconomy)
        public double BaselineWeight { get; set; }   // and their weight then: the size its elders can lead scales from these
        public string Notes { get; set; }
        public Provenance Provenance { get; set; }
        public List<string> InterpretedFields { get; set; } = new List<string>(); // to replace when a source speaks
        public FactionPersonality Personality { get; set; }
        
        // Power Level represents their overall military/cultivation strength (e.g., 100 = weak, 10000 = major sect)
        public int PowerLevel { get; set; }
        public int Wealth { get; set; }
        
        // Relationship with the player's clan (-100 to +100)
        public int RelationWithPlayer { get; set; }

        public int FormationLevel { get; set; }         // its Protective Formation (audit §2.3, parity): it guards against thefts and sabotages
        public int EssencePills { get; set; }           // its Essence Gathering Pills for its elders' Foundation wall (audit §2.6, parity)

        /// <summary>An independent copy (saves and loads never share factions with a live game).</summary>
        public FactionData Clone()
        {
            var copy = (FactionData)MemberwiseClone();
            copy.Techniques = new List<string>(Techniques ?? new List<string>());
            copy.Artifacts = new List<ArtifactInstance>(Artifacts ?? new List<ArtifactInstance>());
            copy.Designations = new List<RankDesignation>(Designations ?? new List<RankDesignation>());
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
        public bool Ancient { get; set; }                // there when the world began: an ancient being, whose lore is old
        public bool Perfected { get; set; }              // reached the Grand Perfection (five abilities): only then may it try the Golden Core
        public string FruitionId { get; set; }           // the lineage whose Realization it holds, or null (2026-10-01)
        public bool HasDharmaTreasure { get; set; }      // a True Monarch's treasure of its foundation (the world's, 2026-10-03)
        public bool TreasureBound { get; set; }          // bound for life to a Spiritual Treasure: a Purple Mansion's power, never further
        public bool ImperialCore { get; set; }           // its Golden Core forged by governing its kingdom: no Realization asked
        public bool Reborn { get; set; }                 // a True Monarch come back from rebirth: it bends lesser minds (audit §1.8)
        public bool ThrallOfClan { get; set; }           // its mind bent by the clan's returned ancestor: the clan's eyes inside (audit §1.8)

        public int Age(int year) => year - BornYear;

        public FactionElder Clone() => (FactionElder)MemberwiseClone();
    }
}
