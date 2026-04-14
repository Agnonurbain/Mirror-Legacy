using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Static utility class handling the generation of genetics (Spiritual Root, Affinity) for newborns.
    /// </summary>
    public static class GeneticSystem
    {
        /// <summary>
        /// Calculates the Spiritual Root of a child based on parents' roots and a random mutation factor.
        /// </summary>
        public static int GenerateSpiritualRoot(CharacterData father, CharacterData mother)
        {
            int fatherRoot = father != null ? father.SpiritualRoot : 10; // Default low if unknown
            int motherRoot = mother != null ? mother.SpiritualRoot : 10;

            int baseRoot = (fatherRoot + motherRoot) / 2;
            int mutation = Random.Range(-15, 16); // -15 to +15

            int finalRoot = Mathf.Clamp(baseRoot + mutation, 1, 100);
            return finalRoot;
        }

        /// <summary>
        /// Determines the elemental affinity of a child. 
        /// 70% chance to inherit from a parent, 30% chance to mutate.
        /// </summary>
        public static Element GenerateAffinity(CharacterData father, CharacterData mother)
        {
            float roll = Random.value;

            if (roll <= 0.7f && (father != null || mother != null))
            {
                // Inherit from parent
                bool inheritFromFather = Random.value > 0.5f;
                if (inheritFromFather && father != null && father.Affinity != Element.None)
                    return father.Affinity;
                if (!inheritFromFather && mother != null && mother.Affinity != Element.None)
                    return mother.Affinity;
                
                // Fallback if the chosen parent has no affinity but the other does
                if (father != null && father.Affinity != Element.None) return father.Affinity;
                if (mother != null && mother.Affinity != Element.None) return mother.Affinity;
            }

            // 30% Mutation or fallback if parents have no affinity
            return GenerateRandomAffinity();
        }

        private static Element GenerateRandomAffinity()
        {
            float roll = Random.value;
            
            // Standard elements (80% chance total, 16% each)
            if (roll < 0.80f)
            {
                Element[] standards = { Element.Fire, Element.Water, Element.Wood, Element.Metal, Element.Earth };
                return standards[Random.Range(0, standards.Length)];
            }
            // Rare elements (16% chance)
            else if (roll < 0.96f)
            {
                return Element.Lightning;
            }
            // Very rare elements (4% chance total, 2% each)
            else
            {
                return Random.value > 0.5f ? Element.Light : Element.Darkness;
            }
        }
    }
}
