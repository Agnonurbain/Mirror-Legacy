using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Characters;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Handles espionage missions: stealing technique fragments from rival factions.
    /// Success depends on the spy's SpiritualRoot vs the faction's PowerLevel.
    /// </summary>
    public class EspionageSystem : MonoBehaviour
    {
        public static EspionageSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Attempts to steal a fragment from a target faction.
        /// Success = SpiritualRoot × 0.5% − FactionPower / 100, clamped [5%, 60%].
        /// </summary>
        public EspionageResult AttemptEspionage(CharacterData spy, FactionData target)
        {
            if (spy == null || target == null || !spy.IsAlive)
                return new EspionageResult { Success = false };

            float successChance = spy.SpiritualRoot * 0.005f - target.PowerLevel / 100f;
            successChance = Mathf.Clamp(successChance, 0.05f, 0.60f);

            bool success = Random.value < successChance;
            var result = new EspionageResult { Success = success, TargetFaction = target.Name };

            if (success)
            {
                int quality = Mathf.Clamp(target.PowerLevel / 250, 1, 5);
                Element element = (Element)Random.Range(1, 8);

                if (DeductionEngine.Instance != null)
                {
                    DeductionEngine.Instance.AddFragment(element, quality,
                        $"Stolen from {target.Name} by {spy.FullName}");
                    result.FragmentQuality = quality;
                    result.FragmentElement = element;
                }

                Debug.Log($"[EspionageSystem] {spy.FullName} stole a Q{quality} {element} fragment from {target.Name}!");
            }
            else
            {
                // Caught: relation penalty + mental stability loss
                if (FactionManager.Instance != null)
                    FactionManager.Instance.ChangeRelation(target.ID, -20);

                if (MentalStabilitySystem.Instance != null)
                    MentalStabilitySystem.Instance.ApplyModifier(spy, -10);

                result.RelationPenalty = -20;
                result.StabilityPenalty = -10;

                Debug.LogWarning($"[EspionageSystem] {spy.FullName} was caught by {target.Name}! (-20 relation, -10 MS)");

                // 10% chance of combat if caught
                if (Random.value < 0.10f)
                {
                    result.TriggeredCombat = true;
                    Debug.LogWarning($"[EspionageSystem] {target.Name} retaliates with force!");
                }
            }

            return result;
        }
    }

    public struct EspionageResult
    {
        public bool Success;
        public string TargetFaction;
        public int FragmentQuality;
        public Element FragmentElement;
        public int RelationPenalty;
        public int StabilityPenalty;
        public bool TriggeredCombat;
    }
}
