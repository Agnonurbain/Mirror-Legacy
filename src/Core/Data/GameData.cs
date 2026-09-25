using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A saved game (JSON, enums by name). Version 2 holds the whole state; version 1 saves only had
    /// the clan, the year, the phase and the stones, and load with defaults for everything else.
    /// Version 2.1 adds the techniques of LORE.md §2 (knowledge, Qi); older saves receive the clan's
    /// starting knowledge on load. Version 2.2 adds the state of the Dao lineages (§6.8). Field names never
    /// change: older saves must keep loading.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public const string CurrentVersion = "2.2";

        public string SaveVersion { get; set; } = CurrentVersion;
        public int Seed { get; set; }

        // Time and clan
        public string ClanName { get; set; }
        public int CurrentYear { get; set; } = 1;
        public GamePhase CurrentPhase { get; set; }
        public string PatriarchID { get; set; }
        public List<CharacterData> HistoricalRecords { get; set; } = new List<CharacterData>(); // living and dead

        // Treasury (defaults match a new game, for version 1 saves)
        public int SpiritStones { get; set; } = 1000;
        public int MedicinalHerbs { get; set; } = 50;
        public int SpiritualOres { get; set; } = 30;
        public int Prestige { get; set; } = 10;
        public int TechniqueFragments { get; set; }

        // Mirror
        public int MirrorPower { get; set; } = 50;
        public int RestoredFragments { get; set; }
        public List<FragmentData> Fragments { get; set; }
        public List<TechniqueData> Techniques { get; set; } // deduced by the mirror

        // Techniques and Qi (2.1; null in older saves)
        public List<string> KnownTechniqueIds { get; set; }                 // catalog techniques the clan knows
        public Dictionary<string, int> SpiritualQi { get; set; }            // portions in store, by Qi
        public Dictionary<string, int> QiHarvestProgress { get; set; }      // years of work towards the next portion

        // The Dao lineages of this world (L4; null in older saves, drawn again from the seed)
        public Dictionary<string, FruitionState> FruitionStates { get; set; }

        // Lineage
        public int GenerationCount { get; set; } = 1;
        public int TotalBirths { get; set; }
        public int TotalDeaths { get; set; }
        public string LastPatriarchId { get; set; }
        public int AscendedAncestors { get; set; }

        // World
        public List<BuildingData> Buildings { get; set; }
        public List<FactionData> Factions { get; set; }
        public List<StoryTriggerType> TriggeredStoryEvents { get; set; }
        public List<StoryTriggerType> PendingStoryEvents { get; set; }

        // Outcome
        public bool GameWon { get; set; }
        public bool GameLost { get; set; }
    }
}
