using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>Hereditary spiritual orifice, mortals and Talisman Seeds. TDD stub.</summary>
    public static class SpiritualOrificeRules
    {
        public static double OrificeChance(int parentsWithOrifice) => -1;
        public static bool HasOrificeAtBirth(int parentsWithOrifice, double roll) => false;
        public static int CountParentsWithOrifice(CharacterData father, CharacterData mother) => -1;
        public static bool CanCultivate(CharacterData character) => false;
        public static bool CanDetectOrifice(CharacterData examiner) => false;
        public static int MortalLifespan(double roll) => 0;
        public static bool CanReceiveTalismanSeed(CharacterData character, int activeSeeds, int capacity) => false;
        public static int TalismanSeedCapacity(int mirrorFragments) => 0;
        public static void Normalize(CharacterData character) { }
        public static int RevealOrifices(IReadOnlyList<CharacterData> members) => -1;
    }
}
