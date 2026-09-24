using UnityEngine;
using MirrorChronicles.Events;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// Manages the clan's resources: Spirit Stones, Medicinal Herbs,
    /// Spiritual Ores, Prestige, and Technique Fragments.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public int SpiritStones { get; private set; } = 1000;
        public int MedicinalHerbs { get; private set; } = 50;
        public int SpiritualOres { get; private set; } = 30;
        public int Prestige { get; private set; } = 10;
        public int TechniqueFragments { get; private set; } = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // --- Spirit Stones ---
        public void AddSpiritStones(int amount)
        {
            if (amount <= 0) return;
            SpiritStones += amount;
            Debug.Log($"[ResourceManager] +{amount} Spirit Stones. Total: {SpiritStones}");
            GameEvents.TriggerSpiritStonesChanged(SpiritStones);
        }

        public bool ConsumeSpiritStones(int amount)
        {
            if (amount <= 0) return true;
            if (SpiritStones < amount)
            {
                Debug.LogWarning($"[ResourceManager] Not enough Spirit Stones! Need {amount}, have {SpiritStones}");
                return false;
            }
            SpiritStones -= amount;
            Debug.Log($"[ResourceManager] -{amount} Spirit Stones. Total: {SpiritStones}");
            GameEvents.TriggerSpiritStonesChanged(SpiritStones);
            return true;
        }

        public void SetSpiritStones(int amount)
        {
            SpiritStones = Mathf.Max(0, amount);
            GameEvents.TriggerSpiritStonesChanged(SpiritStones);
        }

        // --- Medicinal Herbs ---
        public void AddHerbs(int amount)
        {
            if (amount <= 0) return;
            MedicinalHerbs += amount;
            Debug.Log($"[ResourceManager] +{amount} Herbs. Total: {MedicinalHerbs}");
        }

        public bool ConsumeHerbs(int amount)
        {
            if (amount <= 0) return true;
            if (MedicinalHerbs < amount) return false;
            MedicinalHerbs -= amount;
            return true;
        }

        // --- Spiritual Ores ---
        public void AddOres(int amount)
        {
            if (amount <= 0) return;
            SpiritualOres += amount;
            Debug.Log($"[ResourceManager] +{amount} Ores. Total: {SpiritualOres}");
        }

        public bool ConsumeOres(int amount)
        {
            if (amount <= 0) return true;
            if (SpiritualOres < amount) return false;
            SpiritualOres -= amount;
            return true;
        }

        // --- Prestige (non-material) ---
        public void AddPrestige(int amount)
        {
            Prestige += amount;
            Debug.Log($"[ResourceManager] Prestige {(amount >= 0 ? "+" : "")}{amount}. Total: {Prestige}");
        }

        // --- Technique Fragments ---
        public void AddTechniqueFragments(int amount)
        {
            if (amount <= 0) return;
            TechniqueFragments += amount;
            Debug.Log($"[ResourceManager] +{amount} Technique Fragments. Total: {TechniqueFragments}");
        }

        public bool ConsumeTechniqueFragments(int amount)
        {
            if (amount <= 0) return true;
            if (TechniqueFragments < amount) return false;
            TechniqueFragments -= amount;
            return true;
        }
    }
}
