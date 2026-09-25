using System;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Yearly cultivation on the power ladder (LORE.md §5). Sub-levels without a trial advance on their
    /// own; trials wait for the Breakthrough phase.
    /// </summary>
    public sealed class CultivationSystem
    {
        public const int BaseYearlyXp = 10;
        public const int LowStabilityThreshold = 50;
        public const double LowStabilityMultiplier = 0.8;

        private readonly GameContext ctx;
        private readonly ClanKarmaSystem karma;

        public CultivationSystem(GameContext ctx, ClanKarmaSystem karma)
        {
            this.ctx = ctx;
            this.karma = karma;
        }

        /// <summary>A cultivating member gains 10 + half their root, plus the clan's karma; low stability slows them.</summary>
        public void ProcessYearlyCultivation(CharacterData character)
        {
            if (!character.IsAlive || character.CurrentTask != TaskType.Cultivation) return;
            if (!SpiritualOrificeRules.CanCultivate(character)) return; // a mortal gathers no Qi

            int gain = BaseYearlyXp + character.SpiritualRoot / 2 + karma.GetBonusXP();
            double multiplier = 1.0 + karma.GetCultivationSpeedBonus();
            if (character.MentalStability < LowStabilityThreshold)
                multiplier *= LowStabilityMultiplier;

            GrantXp(character, (int)Math.Round(gain * multiplier));
        }

        /// <summary>Adds XP from any source (cultivation, study, teaching, buildings) and climbs free sub-levels.</summary>
        public void GrantXp(CharacterData character, int amount)
        {
            if (amount <= 0 || !SpiritualOrificeRules.CanCultivate(character)) return;
            character.CultivationXP += amount;
            AdvanceSubLevels(character);
        }

        /// <summary>Spends XP on every sub-level that needs no trial; stops before a trial or an unavailable step.</summary>
        public void AdvanceSubLevels(CharacterData character)
        {
            if (!SpiritualOrificeRules.CanCultivate(character)) return;

            while (true)
            {
                int required = PowerLadder.XpForNextStage(character.Realm);
                var step = PowerLadder.Next(character.Realm, character.RealmStage);
                if (required <= 0 || character.CultivationXP < required || !step.IsAvailable) return;
                if (step.Trial != TrialKind.None) return; // the Breakthrough phase decides

                character.CultivationXP -= required;
                ApplyStep(character, step);
            }
        }

        /// <summary>True when the character has the XP for a step gated by a trial that can be attempted now.</summary>
        public static bool IsReadyForTrial(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            return character.IsAlive
                && SpiritualOrificeRules.CanCultivate(character)
                && step.IsAvailable
                && step.Trial != TrialKind.None
                && character.CultivationXP >= PowerLadder.XpForNextStage(character.Realm);
        }

        /// <summary>Moves the character to the step; the new realm's reach raises the lifespan, Dao wounds persist.</summary>
        public void ApplyStep(CharacterData character, AdvancementStep step)
        {
            character.Realm = step.TargetRealm;
            character.RealmStage = step.TargetStage;
            character.MaxLifespan = PowerLadder.LifespanAfterAdvance(character);

            ctx.Log.Info($"[Cultivation] {character.FullName} reaches {RankCatalog.DisplayName(character)}.");
        }
    }
}
