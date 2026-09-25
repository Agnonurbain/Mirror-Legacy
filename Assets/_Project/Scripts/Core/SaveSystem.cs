using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Handles saving and loading the game state to a JSON file.
    /// Uses Newtonsoft.Json so auto-properties on CharacterData serialize round-trip
    /// (JsonUtility only handles public fields, which would drop every { get; set; }).
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const int MaxSlots = 3;
        private string SaveFilePath => GetSlotPath(0);

        private string GetSlotPath(int slot)
        {
            if (slot <= 0) return Path.Combine(Application.persistentDataPath, "ironman_save.json");
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            Converters = { new StringEnumConverter() }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Auto-save at the end of the year (when transitioning from Inheritance back to Management)
            GameEvents.OnYearStarted += HandleYearStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= HandleYearStarted;
        }

        private void HandleYearStarted(int newYear)
        {
            // Don't save on the very first year initialization
            if (newYear > 1)
            {
                SaveGame();
            }
        }

        public void SaveGame()
        {
            Debug.Log("[SaveSystem] Initiating Auto-Save...");

            GameData data = BuildGameData();

            try
            {
                string json = JsonConvert.SerializeObject(data, SerializerSettings);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Game saved successfully to: {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {e.Message}");
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning("[SaveSystem] No save file found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                GameData data = JsonConvert.DeserializeObject<GameData>(json, SerializerSettings);

                if (data == null)
                {
                    Debug.LogError("[SaveSystem] Save file is empty or corrupt.");
                    return false;
                }

                RestoreGameState(data);
                Debug.Log("[SaveSystem] Game loaded successfully.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load game: {e.Message}");
                return false;
            }
        }

        public void SaveToSlot(int slot)
        {
            if (slot < 1 || slot > MaxSlots)
            {
                Debug.LogWarning($"[SaveSystem] Invalid slot {slot}. Use 1-{MaxSlots}.");
                return;
            }

            string path = GetSlotPath(slot);
            GameData data = BuildGameData();

            try
            {
                string json = JsonConvert.SerializeObject(data, SerializerSettings);
                File.WriteAllText(path, json);
                Debug.Log($"[SaveSystem] Saved to slot {slot}: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Save to slot {slot} failed: {e.Message}");
            }
        }

        public bool LoadFromSlot(int slot)
        {
            if (slot < 1 || slot > MaxSlots) return false;
            string path = GetSlotPath(slot);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] Slot {slot} is empty.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                GameData data = JsonConvert.DeserializeObject<GameData>(json, SerializerSettings);
                if (data == null) return false;
                RestoreGameState(data);
                Debug.Log($"[SaveSystem] Loaded from slot {slot}.");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Load from slot {slot} failed: {e.Message}");
                return false;
            }
        }

        public bool SlotExists(int slot)
        {
            if (slot < 1 || slot > MaxSlots) return false;
            return File.Exists(GetSlotPath(slot));
        }

        private GameData BuildGameData()
        {
            return new GameData
            {
                ClanName = ClanManager.Instance.ClanName,
                CurrentYear = TimeManager.Instance.CurrentYear,
                CurrentPhase = TimeManager.Instance.CurrentPhase,
                SpiritStones = ResourceManager.Instance.SpiritStones,
                HistoricalRecords = BloodRegistry.Instance.HistoricalRecords
            };
        }

        private void RestoreGameState(GameData data)
        {
            // 1. Restore Economy
            ResourceManager.Instance.SetSpiritStones(data.SpiritStones);

            // 2. Restore Registry & Clan
            BloodRegistry.Instance.HistoricalRecords.Clear();
            ClanManager.Instance.LivingMembers.Clear();

            foreach (var character in data.HistoricalRecords)
            {
                MirrorChronicles.Characters.PowerLadder.Normalize(character); // saves made before realm stages
                character.MaxLifespan = MirrorChronicles.Characters.PowerLadder.MaxLifespan(character.Realm, character.RealmStage);
                BloodRegistry.Instance.HistoricalRecords.Add(character);
                
                if (character.IsAlive)
                {
                    ClanManager.Instance.LivingMembers.Add(character);
                }
            }

            // 3. Restore Time (Note: TimeManager needs a way to set these without triggering events immediately if loading)
            // For a robust implementation, TimeManager should have a LoadState(year, phase) method.
            Debug.Log($"[SaveSystem] Restored Year {data.CurrentYear}, Phase {data.CurrentPhase}. Active Members: {ClanManager.Instance.LivingMembers.Count}");
        }
    }
}
