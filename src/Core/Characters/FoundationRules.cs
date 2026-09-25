using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Pure rules of the immortal foundation (LORE.md §5.3): what it costs, its Dao Partners, and the Dao
    /// Heart that aligns with it (decision of 2026-09-25: a hereditary temperament).
    /// </summary>
    public static class FoundationRules
    {
        /// <summary>Portions of one's Qi the immortal foundation absorbs (§2.5: at least one for complex uses).</summary>
        public const int FoundationQiPortions = 1;

        private static readonly Temperament[] Temperaments =
            { Temperament.Dominant, Temperament.Solitary, Temperament.Patient, Temperament.Fiery, Temperament.Cunning, Temperament.Serene };

        /// <summary>The lineage of a foundation (« fruition-id:ability-id »), or null.</summary>
        public static FruitionDefinition FruitionOf(string foundationId, IReadOnlyList<FruitionDefinition> fruitions)
        {
            var (fruitionId, _) = FoundationRef.Parse(foundationId);
            return fruitionId == null ? null : fruitions.FirstOrDefault(f => f.Id == fruitionId);
        }

        /// <summary>
        /// The Dao Partners of a foundation: the lineage's other foundations (§5.3.3: the four others of the
        /// same Fruition, and any the lore adds). Knowing them before entering the Dao is vital.
        /// </summary>
        public static IReadOnlyList<DivineAbilityDefinition> DaoPartners(FruitionDefinition fruition, string abilityId) =>
            fruition?.Abilities.Where(a => a.Id != abilityId).ToList() ?? (IReadOnlyList<DivineAbilityDefinition>)Array.Empty<DivineAbilityDefinition>();

        /// <summary>
        /// Cultivation speed from the Dao Heart (§5.3.2): faster when the temper matches what the lineage
        /// favours, slower otherwise; neutral without a foundation or a known temper.
        /// </summary>
        public static double HeartAlignmentSpeed(Temperament temperament, FruitionDefinition fruition, BalanceSettings balance)
        {
            if (fruition == null || temperament == Temperament.None || fruition.Temperament == Temperament.None) return 1.0;
            return temperament == fruition.Temperament ? balance.HeartAlignedSpeed : balance.HeartMisalignedSpeed;
        }

        public static Temperament RandomTemperament(Random rng) => Temperaments[rng.Next(Temperaments.Length)];

        /// <summary>A newborn's temper: most often a parent's (father or mother alike), otherwise its own.</summary>
        public static Temperament InheritTemperament(CharacterData father, CharacterData mother, Random rng, double inheritanceChance)
        {
            if (rng.Chance(inheritanceChance))
            {
                var parent = rng.Chance(0.5) ? father : mother;
                if (parent != null && parent.Temperament != Temperament.None) return parent.Temperament;
            }
            return RandomTemperament(rng);
        }
    }
}
