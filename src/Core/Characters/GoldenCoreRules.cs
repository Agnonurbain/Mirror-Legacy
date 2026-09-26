using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The pure rules of the Golden Core's breakthrough (LORE.md §5.5.1, §5.9 R1-R7): which position five
    /// divine abilities lead to in a lineage, the axiom of the positions, and the chances of the two steps —
    /// forging the metal essence, then being granted a position. Every figure comes from balance.json.
    /// </summary>
    public static class GoldenCoreRules
    {
        public const int AbilitiesToForge = 5;

        /// <summary>The fact subject of a lineage's specialised gold-seeking method (the three-two Intercalary, R5).</summary>
        public static string SpecialisedMethod(string fruitionId) => $"{fruitionId}:intercalary";

        /// <summary>
        /// The position five abilities lead to in the lineage: all five of it (Realization, or Surplus when a
        /// substitute is among them), or one or two of it and the rest of a single other lineage (Intercalary).
        /// </summary>
        public static PositionRoute RouteTo(IEnumerable<string> abilities, string fruitionId, IReadOnlyList<FruitionDefinition> fruitions)
        {
            var held = abilities?.Distinct().ToList();
            var target = fruitions?.FirstOrDefault(f => f.Id == fruitionId);
            if (held == null || held.Count != AbilitiesToForge || target == null) return PositionRoute.None;

            var definitions = held.Select(a => Definition(a, fruitions)).ToList();
            if (definitions.Any(d => d.Ability == null)) return PositionRoute.None;

            int inTarget = definitions.Count(d => d.Lineage == fruitionId);
            if (inTarget == AbilitiesToForge)
                return definitions.Any(d => d.Ability.Substitute) ? PositionRoute.Surplus : PositionRoute.Realization;

            bool oneOtherLineage = definitions.Where(d => d.Lineage != fruitionId).Select(d => d.Lineage).Distinct().Count() == 1;
            if (!oneOtherLineage) return PositionRoute.None;
            if (inTarget == 1) return PositionRoute.IntercalaryFourOne;
            if (inTarget == 2) return PositionRoute.IntercalaryThreeTwo;
            return PositionRoute.None;
        }

        /// <summary>The Golden Core standing a position route grants.</summary>
        public static GoldenCoreState PositionOf(PositionRoute route)
        {
            switch (route)
            {
                case PositionRoute.Realization: return GoldenCoreState.Realization;
                case PositionRoute.Surplus: return GoldenCoreState.Surplus;
                case PositionRoute.IntercalaryFourOne:
                case PositionRoute.IntercalaryThreeTwo: return GoldenCoreState.Intercalary;
                default: return GoldenCoreState.None;
            }
        }

        /// <summary>« An orthodox position knows no Intercalary; a gathered position knows no Surplus » — a danger, not a ban.</summary>
        public static bool BreaksTheAxiom(PositionRoute route, FruitionDefinition target)
        {
            if (target == null) return false;
            bool intercalary = route == PositionRoute.IntercalaryFourOne || route == PositionRoute.IntercalaryThreeTwo;
            return (intercalary && target.Manifestation == Manifestation.Orthodox)
                || (route == PositionRoute.Surplus && target.Manifestation == Manifestation.Gathered);
        }

        /// <summary>
        /// Chance (%) of forging the metal essence: shallow and grafted abilities weigh on it, the Life ability
        /// condensed last helps (§5.4.4), and so does talent.
        /// </summary>
        public static int ForgeChance(CharacterData member, GameContent content)
        {
            var s = content.Balance.GoldenCore;
            var abilities = member.DivineAbilities;
            int shallow = abilities.Count(a => member.ShallowAbilities.Contains(a));
            int grafted = abilities.Count(a => member.GraftedAbilities.Contains(a));
            bool lifeLast = abilities.Count > 0
                && Definition(abilities[abilities.Count - 1], content.Fruitions).Ability?.Types.Contains(AbilityType.Life) == true;

            return Clamp(s.ForgeBaseChance + RootBonus(member, content)
                - shallow * s.ShallowAbilityPenalty - grafted * s.GraftedAbilityPenalty + (lifeLast ? s.LifeLastBonus : 0));
        }

        /// <summary>Chance (%) of being granted the position: the route's own, talent, and the axiom's danger.</summary>
        public static int ClaimChance(CharacterData member, PositionRoute route, FruitionDefinition target, GameContent content)
        {
            var s = content.Balance.GoldenCore;
            int chance;
            switch (route)
            {
                case PositionRoute.Realization: chance = s.RealizationChance; break;
                case PositionRoute.Surplus: chance = s.SurplusChance; break;
                case PositionRoute.IntercalaryFourOne: chance = s.IntercalaryFourOneChance; break;
                case PositionRoute.IntercalaryThreeTwo: chance = s.IntercalaryThreeTwoChance; break;
                default: return 0;
            }
            return Clamp(chance + RootBonus(member, content) - (BreaksTheAxiom(route, target) ? s.AxiomPenalty : 0));
        }

        private static int RootBonus(CharacterData member, GameContent content)
        {
            var m = content.Balance.TrialModifiers;
            return (member.SpiritualRoot - m.AverageRoot) / m.RootPointsPerPercent;
        }

        private static int Clamp(int chance) => Math.Max(1, Math.Min(99, chance));

        private static (string Lineage, DivineAbilityDefinition Ability) Definition(string ability, IReadOnlyList<FruitionDefinition> fruitions)
        {
            var (lineage, abilityId) = FoundationRef.Parse(ability);
            var definition = fruitions.FirstOrDefault(f => f.Id == lineage)?.Abilities.FirstOrDefault(a => a.Id == abilityId);
            return (lineage, definition);
        }
    }
}
