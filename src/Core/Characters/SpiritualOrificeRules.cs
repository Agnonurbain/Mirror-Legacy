using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Hereditary spiritual orifice, mortals and Talisman Seeds (LORE.md §4, decision D3, §11.5).
    /// About 3 in 1000 commoners are born with an orifice; a parent who has one passes it on far
    /// more often, which is why marrying into cultivator lineages matters (the odds live in
    /// balance.json). The mirror's Talisman Seeds let a mortal cultivate anyway, within a capacity
    /// set by its restoration level.
    /// </summary>
    public static class SpiritualOrificeRules
    {
        public const int MortalMinLifespan = 60;
        public const int MortalMaxLifespan = PowerLadder.MortalMaxLifespan;

        public const int DetectionChakraStage = 5; // Summit Eye: first chakra that sees another's orifice

        public static double OrificeChance(int parentsWithOrifice, OrificeOdds odds) => OrificeChance((double)parentsWithOrifice, odds);

        /// <summary>The chance from the parents' weight (an endowed parent 1, a seeded one a share): between the steps, a straight line.</summary>
        public static double OrificeChance(double parentWeight, OrificeOdds odds)
        {
            if (parentWeight <= 0) return odds.Commoner;
            if (parentWeight <= 1) return odds.Commoner + (odds.OneParent - odds.Commoner) * parentWeight;
            return odds.OneParent + (odds.TwoParents - odds.OneParent) * Math.Min(1, parentWeight - 1);
        }

        /// <summary>
        /// What the parents pass on: an endowed parent counts whole, a seeded one by <see cref="OrificeOdds.SeedParentWeight"/> — the
        /// seed's channels leave a trace in the blood (the user's decision 2026-10-04; 📚 Li Xuanfeng, born with an orifice to a seeded father).
        /// </summary>
        public static double ParentWeight(CharacterData father, CharacterData mother, OrificeOdds odds)
        {
            double Weight(CharacterData p) => p == null ? 0 : p.HasSpiritualOrifice ? 1 : p.HasTalismanSeed ? odds.SeedParentWeight : 0;
            return Weight(father) + Weight(mother);
        }

        public static bool HasOrificeAtBirth(double parentWeight, double roll, OrificeOdds odds) => roll < OrificeChance(parentWeight, odds);

        /// <param name="roll">Uniform draw in [0, 1]; 1 never succeeds.</param>
        public static bool HasOrificeAtBirth(int parentsWithOrifice, double roll, OrificeOdds odds)
        {
            return roll < OrificeChance(parentsWithOrifice, odds);
        }

        /// <summary>The parents born with an orifice (a seed's share is <see cref="ParentWeight"/>'s).</summary>
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

        /// <param name="roll">Uniform draw in [0, 1]; 1 (possible with Unity's Random.value) is clamped to the maximum.</param>
        public static int MortalLifespan(double roll)
        {
            int span = MortalMaxLifespan - MortalMinLifespan + 1;
            return Math.Min(MortalMaxLifespan, MortalMinLifespan + (int)(roll * span));
        }

        /// <summary>
        /// A seed takes root in anyone alive without one (📚 the Li born with an orifice received seeds too: a hidden conduit,
        /// AUDIT_LORE.md §3.8), while the mirror sustains another.
        /// </summary>
        public static bool CanReceiveTalismanSeed(CharacterData character, int activeSeeds, int capacity)
        {
            return character.IsAlive && !character.HasTalismanSeed && character.Species == Species.Human && activeSeeds < capacity;
        }

        /// <summary>
        /// One's compatibility with the Profound Pearl seed, the halo the mirror's divine sense sees above one's head (📚: a halo
        /// of one chi is perfect, one cun about a tenth): stable for a person, between a tenth and a whole; few are perfect.
        /// </summary>
        public static double SeedCompatibility(CharacterData character)
        {
            uint hash = 2166136261;
            foreach (char c in character.ID ?? "") hash = (hash ^ c) * 16777619;
            double u = (hash % 1000) / 999.0;
            return Math.Round(0.1 + 0.9 * u * u, 2);
        }

        /// <summary>The halo as the mirror sees it: « un chi » when whole, else so many cun (French).</summary>
        public static string SeedHalo(double compatibility) =>
            compatibility >= 0.995 ? "un chi" : $"{Math.Max(1, (int)Math.Round(compatibility * 10))} cun";

        /// <summary>
        /// A seed's effect on cultivation: a mortal cultivates at its compatibility (one chi: as if born with an orifice); one born
        /// with an orifice is helped a little, the seed a conduit besides (AUDIT_LORE.md §3.8).
        /// </summary>
        public static double SeedSpeed(CharacterData character, double conduitBonus) =>
            !character.HasTalismanSeed ? 1.0 : character.HasSpiritualOrifice ? 1.0 + conduitBonus : SeedCompatibility(character);

        /// <summary>Each restored mirror fragment sustains one more active seed (LORE.md §11.5).</summary>
        public static int TalismanSeedCapacity(int mirrorFragments, int baseCapacity)
        {
            return baseCapacity + Math.Max(0, mirrorFragments);
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
