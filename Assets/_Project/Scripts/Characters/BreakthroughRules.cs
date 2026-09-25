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
        private const int RootPointsPerPercent = 5;
        private const int LowStabilityThreshold = 50;
        private const int LowStabilityPenalty = 20;
        private const float OldAgeLifespanRatio = 0.8f;
        private const int OldAgePenalty = 10;
        private const int MinorFailureMaxRoll = 70;
        private const int MajorFailureMaxRoll = 95;

        /// <summary>
        /// Success chance (1-99) of the trial gating the character's next step,
        /// or 0 when no trial is due or the step is not available yet.
        /// </summary>
        public static int SuccessRate(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            if (!step.IsAvailable || step.Trial == TrialKind.None) return 0;

            int rate = PowerLadder.BaseTrialChance(step.Trial, character.Age);

            int minimumRoot = MinimumRoot(step.Trial);
            if (character.SpiritualRoot > minimumRoot)
                rate += (character.SpiritualRoot - minimumRoot) / RootPointsPerPercent;

            if (character.MentalStability < LowStabilityThreshold)
                rate -= LowStabilityPenalty;

            int lifespan = character.MaxLifespan > 0
                ? character.MaxLifespan
                : PowerLadder.MaxLifespan(character.Realm, character.RealmStage);
            if (character.Age > lifespan * OldAgeLifespanRatio)
                rate -= OldAgePenalty;

            return Math.Max(1, Math.Min(99, rate));
        }

        /// <summary>
        /// Outcome of an attempt: roll 1-100 against the rate; on failure, the severity roll 1-100
        /// picks the consequence (spiritual dissolution for the Foundation wall, the Qi deviation table otherwise).
        /// </summary>
        public static BreakthroughOutcome Resolve(TrialKind trial, int successRate, int age, int roll, int severityRoll)
        {
            if (roll <= successRate) return BreakthroughOutcome.Success;

            if (trial == TrialKind.FoundationWall)
            {
                return severityRoll <= PowerLadder.DissolutionChanceOnFailure(age)
                    ? BreakthroughOutcome.SpiritualDissolution
                    : BreakthroughOutcome.MajorFailure;
            }

            if (severityRoll <= MinorFailureMaxRoll) return BreakthroughOutcome.MinorFailure;
            if (severityRoll <= MajorFailureMaxRoll) return BreakthroughOutcome.MajorFailure;
            return BreakthroughOutcome.QiDeviationDeath;
        }

        /// <summary>Spiritual root above which the root bonus applies.</summary>
        private static int MinimumRoot(TrialKind trial)
        {
            return trial == TrialKind.FoundationWall ? 30 : 10;
        }
    }
}
