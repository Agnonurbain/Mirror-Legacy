using System;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Injuries after combat, from light to critical, and the permanent Dao wound that takes a fifth
    /// of a lifespan for good (it survives every breakthrough, see <see cref="PowerLadder.LifespanAfterAdvance"/>).
    /// </summary>
    public sealed class WoundSystem
    {
        public const double SevereDaoWoundChance = 0.2;
        public const double CriticalDaoWoundChance = 0.8;
        public const int DaoWoundStabilityLoss = 20;

        private readonly GameContext ctx;
        private readonly MentalStabilitySystem stability;

        public WoundSystem(GameContext ctx, MentalStabilitySystem stability)
        {
            this.ctx = ctx;
            this.stability = stability;
        }

        /// <summary>Called for each survivor at the end of a combat, with the vitality they kept.</summary>
        public void EvaluatePostCombatWounds(CharacterData character, int maxVitality, int currentVitality)
        {
            if (!character.IsAlive || maxVitality <= 0) return;

            double damageTaken = 1.0 - (double)currentVitality / maxVitality;
            if (damageTaken <= 0) return;

            if (damageTaken > 0.9) ApplyWound(character, "critical", 30, CriticalDaoWoundChance);
            else if (damageTaken > 0.6) ApplyWound(character, "severe", 15, SevereDaoWoundChance);
            else if (damageTaken > 0.3) ApplyWound(character, "moderate", 5, 0);
            else ctx.Log.Info($"[Wounds] {character.FullName} suffered a light wound.");
        }

        /// <summary>Permanent: recorded on the character, a fifth of the lifespan goes with a year of grace.</summary>
        public void ApplyDaoWound(CharacterData character)
        {
            character.DaoWounds++;
            character.MaxLifespan = Math.Max(character.Age + 1, PowerLadder.WoundedLifespan(character.MaxLifespan, 1));
            stability.ApplyModifier(character, -DaoWoundStabilityLoss);
            ctx.Log.Warning($"[Wounds] {character.FullName} suffers a permanent Dao wound.");
        }

        private void ApplyWound(CharacterData character, string severity, int stabilityLoss, double daoWoundChance)
        {
            ctx.Log.Info($"[Wounds] {character.FullName} suffered a {severity} wound.");
            stability.ApplyModifier(character, -stabilityLoss);
            if (daoWoundChance > 0 && ctx.Rng.NextDouble() < daoWoundChance)
                ApplyDaoWound(character);
        }
    }
}
