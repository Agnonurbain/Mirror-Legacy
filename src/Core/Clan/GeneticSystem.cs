using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>Spiritual root and elemental affinity of newborns.</summary>
    public static class GeneticSystem
    {
        public const int MaxSpiritualRoot = 100;

        private const int UnknownParentRoot = 10;
        private const int MaxMutation = 15;
        private const double InheritAffinityChance = 0.7;

        private static readonly Element[] StandardElements =
            { Element.Fire, Element.Water, Element.Wood, Element.Metal, Element.Earth };

        /// <summary>Mean of the parents' roots (10 for an unknown parent) ± 15, clamped to 1-100.</summary>
        public static int GenerateSpiritualRoot(CharacterData father, CharacterData mother, Random rng)
        {
            int fatherRoot = father != null ? father.SpiritualRoot : UnknownParentRoot;
            int motherRoot = mother != null ? mother.SpiritualRoot : UnknownParentRoot;
            int mutation = rng.Next(-MaxMutation, MaxMutation + 1);
            return Math.Clamp((fatherRoot + motherRoot) / 2 + mutation, 1, MaxSpiritualRoot);
        }

        /// <summary>70% chance to inherit a parent's affinity; otherwise a mutation, rarer elements less often.</summary>
        public static Element GenerateAffinity(CharacterData father, CharacterData mother, Random rng)
        {
            if (rng.NextDouble() <= InheritAffinityChance && (father != null || mother != null))
            {
                bool fromFather = rng.NextDouble() > 0.5;
                if (fromFather && HasAffinity(father)) return father.Affinity;
                if (!fromFather && HasAffinity(mother)) return mother.Affinity;
                if (HasAffinity(father)) return father.Affinity;
                if (HasAffinity(mother)) return mother.Affinity;
            }

            return RandomAffinity(rng);
        }

        private static bool HasAffinity(CharacterData parent) => parent != null && parent.Affinity != Element.None;

        /// <summary>Five standard elements 80% (16% each), Lightning 16%, Light or Darkness 2% each.</summary>
        private static Element RandomAffinity(Random rng)
        {
            double roll = rng.NextDouble();
            if (roll < 0.80) return StandardElements[rng.Next(StandardElements.Length)];
            if (roll < 0.96) return Element.Lightning;
            return rng.NextDouble() > 0.5 ? Element.Light : Element.Darkness;
        }
    }
}
