using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>Result of a breakthrough attempt.</summary>
    public enum BreakthroughOutcome { Success, MinorFailure, MajorFailure, QiDeviationDeath, SpiritualDissolution }

    /// <summary>
    /// Pure breakthrough odds and outcomes for the trials of the power ladder (LORE.md §5.1-5.3).
    /// </summary>
    public static class BreakthroughRules
    {
        /// <summary>
        /// Success chance (1-99) of the trial gating the character's next step, or 0 when no trial is due or the
        /// step is not available yet. The figures come from balance.json (trials, trialModifiers).
        /// </summary>
        public static int SuccessRate(CharacterData character, BalanceSettings balance)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            if (!step.IsAvailable || step.Trial == TrialKind.None) return 0;

            var trials = balance.Trials;
            var modifiers = balance.TrialModifiers;
            int rate = PowerLadder.BaseTrialChance(step.Trial, character.Age, trials);

            int minimumRoot = step.Trial == TrialKind.FoundationWall ? trials.WallMinimumRoot : trials.ChakraMinimumRoot;
            if (character.SpiritualRoot > minimumRoot)
                rate += (character.SpiritualRoot - minimumRoot) / modifiers.RootPointsPerPercent;

            if (character.MentalStability < modifiers.LowStabilityThreshold)
                rate -= modifiers.LowStabilityPenalty;

            if (character.Age > PowerLadder.LifespanLimit(character) * trials.OldAgeLifespanRatio)
                rate -= trials.OldAgePenalty;

            return Math.Max(1, Math.Min(99, rate));
        }

        /// <summary>
        /// Outcome of an attempt: roll 1-100 against the rate; on failure, the severity roll 1-100
        /// picks the consequence (spiritual dissolution for the Foundation wall, the Qi deviation table otherwise).
        /// </summary>
        public static BreakthroughOutcome Resolve(TrialKind trial, int successRate, int age, int roll, int severityRoll, TrialSettings trials)
        {
            if (roll <= successRate) return BreakthroughOutcome.Success;

            if (trial == TrialKind.FoundationWall)
            {
                return severityRoll <= PowerLadder.DissolutionChanceOnFailure(age, trials)
                    ? BreakthroughOutcome.SpiritualDissolution
                    : BreakthroughOutcome.MajorFailure;
            }

            if (severityRoll <= trials.MinorFailureMaxRoll) return BreakthroughOutcome.MinorFailure;
            if (severityRoll <= trials.MajorFailureMaxRoll) return BreakthroughOutcome.MajorFailure;
            return BreakthroughOutcome.QiDeviationDeath;
        }
    }
}
