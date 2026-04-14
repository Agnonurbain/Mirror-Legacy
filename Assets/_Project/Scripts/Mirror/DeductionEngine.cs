using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The core crafting system of the Mirror.
    /// Combines FragmentData to procedurally generate TechniqueData.
    /// </summary>
    public class DeductionEngine : MonoBehaviour
    {
        public static DeductionEngine Instance { get; private set; }

        [Header("Inventory")]
        public List<FragmentData> FragmentInventory = new List<FragmentData>();
        public List<TechniqueData> ClanTechniques = new List<TechniqueData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Add some starting fragments for testing
            AddFragment(Element.Fire, 1, "Scorched Scroll");
            AddFragment(Element.Wood, 1, "Bamboo Slip");
        }

        public void AddFragment(Element element, int quality, string name = "Unknown Fragment")
        {
            FragmentInventory.Add(new FragmentData
            {
                Element = element,
                Quality = quality,
                Name = name
            });
        }

        /// <summary>
        /// Attempts to deduce a new technique using 2 to 5 fragments.
        /// Costs Mirror Power.
        /// </summary>
        public TechniqueData AttemptDeduction(List<FragmentData> inputs)
        {
            if (inputs == null || inputs.Count < 2)
            {
                Debug.LogWarning("[DeductionEngine] Need at least 2 fragments to deduce a technique.");
                return null;
            }

            if (inputs.Count > 5)
            {
                Debug.LogWarning("[DeductionEngine] Cannot use more than 5 fragments at once.");
                return null;
            }

            int mirrorPowerCost = inputs.Count * 10;
            if (!MirrorSystem.Instance.ConsumeMirrorPower(mirrorPowerCost))
            {
                Debug.LogWarning("[DeductionEngine] Not enough Mirror Power for deduction.");
                return null;
            }

            // Remove used fragments from inventory
            foreach (var frag in inputs)
            {
                FragmentInventory.Remove(frag);
            }

            // Calculate resulting technique
            TechniqueData newTechnique = GenerateTechnique(inputs);
            ClanTechniques.Add(newTechnique);

            Debug.Log($"[DeductionEngine] Successfully deduced [{newTechnique.Name}]! Element: {newTechnique.DominantElement}, Power: {newTechnique.PowerModifier}, Risk: {newTechnique.RiskFactor}%");

            return newTechnique;
        }

        private TechniqueData GenerateTechnique(List<FragmentData> inputs)
        {
            int totalQuality = 0;
            Dictionary<Element, int> elementCounts = new Dictionary<Element, int>();

            foreach (var frag in inputs)
            {
                totalQuality += frag.Quality;
                if (elementCounts.ContainsKey(frag.Element))
                    elementCounts[frag.Element]++;
                else
                    elementCounts[frag.Element] = 1;
            }

            // Determine dominant element
            Element dominant = Element.None;
            int maxCount = 0;
            foreach (var kvp in elementCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    dominant = kvp.Key;
                }
            }

            // Calculate Risk Factor based on elemental conflicts (e.g., Water + Fire)
            int risk = CalculateElementalConflictRisk(elementCounts);

            // Determine Type randomly for now
            TechniqueType type = (TechniqueType)UnityEngine.Random.Range(0, 3);

            // Generate procedural name
            string techName = GenerateProceduralName(dominant, type, totalQuality);

            return new TechniqueData
            {
                Name = techName,
                Type = type,
                DominantElement = dominant,
                RequiredRealm = (CultivationRealm)Mathf.Clamp(totalQuality / 3, 0, 9), // Higher quality = higher realm requirement
                PowerModifier = totalQuality * 5,
                QiCost = totalQuality * 2,
                RiskFactor = risk
            };
        }

        private int CalculateElementalConflictRisk(Dictionary<Element, int> counts)
        {
            int risk = 5; // Base risk

            // Simple conflict logic: Water vs Fire
            if (counts.ContainsKey(Element.Water) && counts.ContainsKey(Element.Fire))
                risk += 30;
            
            // Metal vs Wood
            if (counts.ContainsKey(Element.Metal) && counts.ContainsKey(Element.Wood))
                risk += 30;

            // Synergy logic: Wood + Fire
            if (counts.ContainsKey(Element.Wood) && counts.ContainsKey(Element.Fire))
                risk = Mathf.Max(0, risk - 10);

            return risk;
        }

        private string GenerateProceduralName(Element element, TechniqueType type, int quality)
        {
            string prefix = element switch
            {
                Element.Fire => "Blazing",
                Element.Water => "Flowing",
                Element.Wood => "Verdant",
                Element.Metal => "Piercing",
                Element.Earth => "Unshakable",
                Element.Lightning => "Heavenly",
                Element.Ice => "Frostbite",
                _ => "Mystic"
            };

            string suffix = type switch
            {
                TechniqueType.CultivationMethod => "Mantra",
                TechniqueType.MartialArt => "Fist",
                TechniqueType.SupportArt => "Aura",
                _ => "Art"
            };

            string adjective = quality > 10 ? "Divine " : quality > 5 ? "Profound " : "";

            return $"{adjective}{prefix} {suffix}";
        }
    }
}
