using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The historical database of the clan. Stores every member, living or dead.
    /// Used to build the family tree UI.
    /// </summary>
    public class BloodRegistry : MonoBehaviour
    {
        public static BloodRegistry Instance { get; private set; }

        public List<CharacterData> HistoricalRecords { get; private set; } = new List<CharacterData>();

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
            GameEvents.OnCharacterBorn += RegisterBirth;
            // Note: We don't need to listen to OnCharacterDied to add them, 
            // because they are already in the registry from birth. 
            // The CharacterData reference is updated automatically.
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterBorn -= RegisterBirth;
        }

        private void RegisterBirth(CharacterData newMember)
        {
            if (!HistoricalRecords.Contains(newMember))
            {
                HistoricalRecords.Add(newMember);
                Debug.Log($"[BloodRegistry] Registered new entry: {newMember.FullName}");
            }
        }

        /// <summary>
        /// Retrieves a character's data by their ID. Useful for finding parents/spouses.
        /// </summary>
        public CharacterData GetCharacterByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return HistoricalRecords.Find(c => c.ID == id);
        }

        /// <summary>
        /// Retrieves all direct children of a specific character.
        /// </summary>
        public List<CharacterData> GetChildrenOf(string parentId)
        {
            return HistoricalRecords.FindAll(c => c.FatherID == parentId || c.MotherID == parentId);
        }
    }
}
