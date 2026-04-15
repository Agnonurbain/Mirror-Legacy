using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Events;
using MirrorChronicles.Economy;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Handles the inheritance of items, wealth, and knowledge when a clan member dies.
    /// </summary>
    public class LegacySystem : MonoBehaviour
    {
        public static LegacySystem Instance { get; private set; }

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
            GameEvents.OnCharacterDied += HandleLegacy;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterDied -= HandleLegacy;
        }

        private void HandleLegacy(CharacterData deceased, DeathCause cause)
        {
            Debug.Log($"<color=lightblue>[Legacy]</color> Processing inheritance for {deceased.FirstName}...");
            
            // 1. Transfer personal wealth to Clan Treasury
            int wealthToTransfer = 50; // Placeholder for personal wealth
            if (ResourceManager.Instance != null && wealthToTransfer > 0)
            {
                ResourceManager.Instance.AddSpiritStones(wealthToTransfer);
                Debug.Log($"Recovered {wealthToTransfer} Spirit Stones from {deceased.FirstName}'s spatial ring.");
            }

            // 2. If they were a high realm cultivator, they leave behind a Dao Fragment
            if (deceased.Realm >= CultivationRealm.GoldenCore)
            {
                Debug.Log($"<color=magenta>[Profound Legacy]</color> {deceased.FirstName} left behind a profound Dao Fragment of {deceased.Affinity}!");
                // Here we would add it to the DeductionEngine's inventory
            }
        }
    }
}
