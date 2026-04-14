using UnityEngine;
using MirrorChronicles.Events;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// Manages the clan's resources (Spirit Stones, Herbs, Ores).
    /// For Phase 1, we focus on Spirit Stones.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        public int SpiritStones { get; private set; } = 1000; // Starting amount

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddSpiritStones(int amount)
        {
            if (amount <= 0) return;
            
            SpiritStones += amount;
            Debug.Log($"[ResourceManager] Added {amount} Spirit Stones. Total: {SpiritStones}");
            GameEvents.TriggerSpiritStonesChanged(SpiritStones);
        }

        public bool ConsumeSpiritStones(int amount)
        {
            if (amount <= 0) return true;

            if (SpiritStones >= amount)
            {
                SpiritStones -= amount;
                Debug.Log($"[ResourceManager] Consumed {amount} Spirit Stones. Total: {SpiritStones}");
                GameEvents.TriggerSpiritStonesChanged(SpiritStones);
                return true;
            }

            Debug.LogWarning($"[ResourceManager] Not enough Spirit Stones! Needed: {amount}, Have: {SpiritStones}");
            return false;
        }

        // Used by SaveSystem to restore state
        public void SetSpiritStones(int amount)
        {
            SpiritStones = Mathf.Max(0, amount);
            GameEvents.TriggerSpiritStonesChanged(SpiritStones);
        }
    }
}
