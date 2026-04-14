using System;
using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// The root object that gets serialized to JSON.
    /// Contains all the necessary data to reconstruct the game state.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public string SaveVersion = "1.0";
        public string ClanName;
        public int CurrentYear;
        public GamePhase CurrentPhase;
        public int SpiritStones;
        
        // We save the entire history. Active members can be filtered by IsAlive == true.
        public List<CharacterData> HistoricalRecords = new List<CharacterData>();
    }
}
