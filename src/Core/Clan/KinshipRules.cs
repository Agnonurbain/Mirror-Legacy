using System;
using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Pure kinship checks over the FatherID / MotherID links stored on CharacterData.
    /// </summary>
    public static class KinshipRules
    {
        /// <summary>
        /// Marriage is forbidden between relatives sharing an ancestor within this many generations.
        /// </summary>
        public const int MarriageForbiddenGenerations = 3;

        /// <summary>
        /// True when both characters share an ancestor within <paramref name="generations"/>
        /// generations, or one is an ancestor of the other within that range.
        /// </summary>
        /// <param name="findById">Resolves a parent ID to its CharacterData (e.g. BloodRegistry.GetCharacterByID).
        /// When null or unresolved, only the direct parent IDs are known.</param>
        public static bool AreCloseKin(CharacterData a, CharacterData b, int generations, Func<string, CharacterData> findById)
        {
            if (a == null || b == null) return false;

            return CollectLineage(a, generations, findById).Overlaps(CollectLineage(b, generations, findById));
        }

        /// <summary>
        /// Returns the IDs of the character and of every known ancestor up to <paramref name="generations"/> levels.
        /// </summary>
        private static HashSet<string> CollectLineage(CharacterData person, int generations, Func<string, CharacterData> findById)
        {
            var lineage = new HashSet<string> { person.ID };
            var currentGeneration = new List<CharacterData> { person };

            for (int depth = 1; depth <= generations && currentGeneration.Count > 0; depth++)
            {
                var parents = new List<CharacterData>();
                foreach (var member in currentGeneration)
                {
                    AddParent(member.FatherID, lineage, parents, findById);
                    AddParent(member.MotherID, lineage, parents, findById);
                }
                currentGeneration = parents;
            }

            return lineage;
        }

        private static void AddParent(string parentId, HashSet<string> lineage, List<CharacterData> parents, Func<string, CharacterData> findById)
        {
            if (string.IsNullOrEmpty(parentId) || !lineage.Add(parentId)) return;

            var parent = findById?.Invoke(parentId);
            if (parent != null) parents.Add(parent);
        }
    }
}
