using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Runs the trials of the power ladder (LORE.md §5): chakra trials and the Foundation wall.
    /// Every ready member attempts their trial during the Breakthrough phase.
    /// </summary>
    public sealed class BreakthroughSystem
    {
        public const int AncestralShieldBonus = 30;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly CultivationSystem cultivation;

        /// <summary>Set by the mirror's Ancestral Shield; protects the next attempt only.</summary>
        public bool AncestralShieldActive { get; set; }

        public BreakthroughSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.cultivation = cultivation;
        }

        /// <summary>Every living member ready for a trial attempts it. Returns the number of attempts.</summary>
        public int ProcessBreakthroughPhase()
        {
            int attempts = 0;
            foreach (var member in clan.LivingMembers.ToList())
            {
                if (!cultivation.IsReadyForTrial(member)) continue;
                AttemptBreakthrough(member);
                attempts++;
            }
            return attempts;
        }

        public int CalculateSuccessRate(CharacterData character) => BreakthroughRules.SuccessRate(character);

        /// <summary>Returns the outcome, or null when the character has no trial to attempt.</summary>
        public BreakthroughOutcome? AttemptBreakthrough(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            if (!step.IsAvailable || step.Trial == TrialKind.None)
            {
                ctx.Log.Warning($"[Breakthrough] {character.FullName} has no trial to attempt ({RankCatalog.DisplayName(character)}).");
                return null;
            }
            if (!cultivation.AllowsNextStep(character, step))
            {
                ctx.Log.Warning($"[Breakthrough] {character.FullName}'s method leads no further than {RankCatalog.DisplayName(character)}.");
                return null;
            }
            if (character.ProgressionSealed)
            {
                ctx.Log.Warning($"[Breakthrough] {character.FullName} consumed a Dao Partner and can progress no further.");
                return null;
            }
            if (!cultivation.PayTrialQi(character, step.Trial))
            {
                ctx.Log.Warning($"[Breakthrough] {character.FullName} lacks the portion of Qi the {step.Trial} absorbs.");
                return null;
            }

            int successRate = CalculateSuccessRate(character);
            if (AncestralShieldActive)
            {
                successRate = Math.Min(99, successRate + AncestralShieldBonus);
                AncestralShieldActive = false;
            }

            int roll = ctx.Rng.Next(1, 101);
            int severityRoll = ctx.Rng.Next(1, 101);
            var outcome = BreakthroughRules.Resolve(step.Trial, successRate, character.Age, roll, severityRoll);
            int required = PowerLadder.XpForNextStage(character.Realm);

            ctx.Log.Info($"[Breakthrough] {character.FullName} attempts the {step.Trial} trial ({successRate}%, roll {roll}): {outcome}.");

            switch (outcome)
            {
                case BreakthroughOutcome.Success:
                    character.CultivationXP = Math.Max(0, character.CultivationXP - required);
                    cultivation.ApplyStep(character, step);
                    ctx.Events.TriggerBreakthroughSuccess(character, character.Realm);
                    return outcome;
                case BreakthroughOutcome.MinorFailure:
                    character.CultivationXP = Math.Max(0, character.CultivationXP - required / 2);
                    break;
                case BreakthroughOutcome.MajorFailure:
                    character.CultivationXP = 0;
                    break;
                case BreakthroughOutcome.QiDeviationDeath:
                    clan.Kill(character, DeathCause.QiDeviation);
                    return outcome; // no failure event for the dead
                case BreakthroughOutcome.SpiritualDissolution:
                    clan.Kill(character, DeathCause.SpiritualDissolution);
                    return outcome;
            }

            ctx.Events.TriggerBreakthroughFailed(character);
            return outcome;
        }
    }
}
