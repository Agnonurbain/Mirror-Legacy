using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Hereditary spiritual orifice, mortals and Talisman Seeds (LORE.md §4, decision D3, §11.5).
    /// Only 3 in 1000 commoners are born with an orifice; a parent who has one passes it on far
    /// more often, which is why marrying into cultivator lineages matters. The mirror's Talisman
    /// Seeds let a mortal cultivate anyway, within a capacity set by its restoration level.
    /// </summary>
    public static class SpiritualOrificeRules
    {
        public const double CommonerOrificeChance = 0.003;
        public const double OneParentOrificeChance = 0.35;
        public const double TwoParentsOrificeChance = 0.5;

        public const int MortalMinLifespan = 60;
        public const int MortalMaxLifespan = PowerLadder.MortalMaxLifespan;

        public const int DetectionChakraStage = 5; // Summit Eye: first chakra that sees another's orifice
        public const int BaseTalismanSeedCapacity = 2;

        public static double OrificeChance(int parentsWithOrifice)
        {
            if (parentsWithOrifice <= 0) return CommonerOrificeChance;
            return parentsWithOrifice == 1 ? OneParentOrificeChance : TwoParentsOrificeChance;
        }

        /// <param name="roll">Uniform draw in [0, 1).</param>
        public static bool HasOrificeAtBirth(int parentsWithOrifice, double roll)
        {
            return roll < OrificeChance(parentsWithOrifice);
        }

        /// <summary>A Talisman Seed grafts artificial channels; it is not inherited.</summary>
        public static int CountParentsWithOrifice(CharacterData father, CharacterData mother)
        {
            int count = 0;
            if (father != null && father.HasSpiritualOrifice) count++;
            if (mother != null && mother.HasSpiritualOrifice) count++;
            return count;
        }

        /// <summary>Humans need an orifice or a Talisman Seed; beasts, spirits and dragons awaken on their own.</summary>
        public static bool CanCultivate(CharacterData character)
        {
            return character.HasSpiritualOrifice || character.HasTalismanSeed || character.Species != Species.Human;
        }

        /// <summary>Only a confirmed cultivator — Summit Eye chakra or beyond — can see another's orifice.</summary>
        public static bool CanDetectOrifice(CharacterData examiner)
        {
            if (!examiner.IsAlive || !CanCultivate(examiner)) return false;
            return examiner.Realm > CultivationRealm.Embryonic || examiner.RealmStage >= DetectionChakraStage;
        }

        /// <param name="roll">Uniform draw in [0, 1).</param>
        public static int MortalLifespan(double roll)
        {
            int span = MortalMaxLifespan - MortalMinLifespan + 1;
            return Math.Min(MortalMaxLifespan, MortalMinLifespan + (int)(roll * span));
        }

        public static bool CanReceiveTalismanSeed(CharacterData character, int activeSeeds, int capacity)
        {
            return character.IsAlive && !CanCultivate(character) && activeSeeds < capacity;
        }

        /// <summary>Each restored mirror fragment sustains one more active seed (LORE.md §11.5).</summary>
        public static int TalismanSeedCapacity(int mirrorFragments)
        {
            return BaseTalismanSeedCapacity + Math.Max(0, mirrorFragments);
        }

        /// <summary>
        /// Saves written before orifices existed hold cultivators without one: anyone who already
        /// opened a chakra evidently has a known orifice (unless a seed explains it).
        /// </summary>
        public static void Normalize(CharacterData character)
        {
            bool hasCultivated = character.Realm > CultivationRealm.Embryonic || character.RealmStage > 0;
            if (!hasCultivated) return;

            if (!CanCultivate(character)) character.HasSpiritualOrifice = true;
            character.OrificeKnown = true;
        }

        /// <summary>
        /// When a living member can detect orifices, every living member's status becomes known.
        /// Returns how many members were newly examined.
        /// </summary>
        public static int RevealOrifices(IReadOnlyList<CharacterData> members)
        {
            if (!members.Any(CanDetectOrifice)) return 0;

            int revealed = 0;
            foreach (var member in members)
            {
                if (!member.IsAlive || member.OrificeKnown) continue;
                member.OrificeKnown = true;
                revealed++;
            }
            return revealed;
        }
    }
}
