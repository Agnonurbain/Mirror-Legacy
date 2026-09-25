using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Yearly cultivation on the power ladder (LORE.md §5) with the method each member practises (§2):
    /// its grade sets the speed and the realm where it stops. Sub-levels without a trial advance on their
    /// own; trials wait for the Breakthrough phase; entering Qi Cultivation absorbs the method's Qi.
    /// </summary>
    public sealed class CultivationSystem
    {
        public const int BaseYearlyXp = 10;
        public const int LowStabilityThreshold = 50;
        public const double LowStabilityMultiplier = 0.8;

        /// <summary>Methods below this grade stop at Qi Cultivation: the elders never impose one on a member's behalf.</summary>
        public const int LowestGradeChosenForAMember = 3;

        private readonly GameContext ctx;
        private readonly ClanKarmaSystem karma;
        private readonly TechniqueLibrary techniques;
        private readonly ResourceManager resources;

        public CultivationSystem(GameContext ctx, ClanKarmaSystem karma, TechniqueLibrary techniques, ResourceManager resources)
        {
            this.ctx = ctx;
            this.karma = karma;
            this.techniques = techniques;
            this.resources = resources;
        }

        /// <summary>
        /// A cultivating member gains 10 + half their root, plus the clan's karma, at the speed of their
        /// method (common breathing in Embryonic without one); low stability slows them.
        /// </summary>
        public void ProcessYearlyCultivation(CharacterData character)
        {
            if (!character.IsAlive || character.CurrentTask != TaskType.Cultivation) return;
            if (!SpiritualOrificeRules.CanCultivate(character)) return; // a mortal gathers no Qi

            double speed = TechniqueRules.CultivationSpeed(techniques.MethodOf(character), character.Realm,
                ctx.Content.Balance.TechniqueSpeedByGrade);
            if (speed <= 0) return; // no method guides this realm

            int gain = BaseYearlyXp + character.SpiritualRoot / 2 + karma.GetBonusXP();
            double multiplier = (1.0 + karma.GetCultivationSpeedBonus()) * speed;
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

        /// <summary>
        /// Spends XP on every sub-level that needs no trial; stops before a trial, an unavailable step, a realm
        /// the method does not lead to, or Qi Cultivation without the method's Qi.
        /// </summary>
        public void AdvanceSubLevels(CharacterData character)
        {
            if (!SpiritualOrificeRules.CanCultivate(character)) return;

            while (true)
            {
                int required = PowerLadder.XpForNextStage(character.Realm);
                var step = PowerLadder.Next(character.Realm, character.RealmStage);
                if (required <= 0 || character.CultivationXP < required || !step.IsAvailable) return;
                if (step.Trial != TrialKind.None) return; // the Breakthrough phase decides

                if (EntersQiCultivation(character, step))
                {
                    if (!TryAbsorbQi(character)) return; // waits for a portion of the method's Qi
                }
                else if (!AllowsNextStep(character, step))
                {
                    return;
                }

                character.CultivationXP -= required;
                ApplyStep(character, step);
            }
        }

        /// <summary>True when the character has the XP for a step gated by a trial that can be attempted now.</summary>
        public bool IsReadyForTrial(CharacterData character)
        {
            var step = PowerLadder.Next(character.Realm, character.RealmStage);
            return character.IsAlive
                && SpiritualOrificeRules.CanCultivate(character)
                && step.IsAvailable
                && step.Trial != TrialKind.None
                && AllowsNextStep(character, step)
                && character.CultivationXP >= PowerLadder.XpForNextStage(character.Realm);
        }

        /// <summary>True when the member's method leads to the step (LORE.md §2.2); Embryonic Breathing needs no manual.</summary>
        public bool AllowsNextStep(CharacterData character, AdvancementStep step) =>
            TechniqueRules.AllowsAdvance(techniques.MethodOf(character), character.Realm, step.TargetRealm);

        /// <summary>
        /// Moves the character to the step; the new realm's reach raises the lifespan, Dao wounds persist,
        /// and a flawed method keeps its toll.
        /// </summary>
        public void ApplyStep(CharacterData character, AdvancementStep step)
        {
            character.Realm = step.TargetRealm;
            character.RealmStage = step.TargetStage;
            character.MaxLifespan = TechniqueRules.LifespanWithMethod(PowerLadder.LifespanAfterAdvance(character), techniques.MethodOf(character));

            ctx.Log.Info($"[Cultivation] {character.FullName} reaches {RankCatalog.DisplayName(character)}.");
        }

        private static bool EntersQiCultivation(CharacterData character, AdvancementStep step) =>
            character.Realm == CultivationRealm.Embryonic && step.TargetRealm == CultivationRealm.QiRefinement;

        /// <summary>
        /// The Premier Souffle (LORE.md §5.1-5.2): absorb a spiritual Qi and cultivate it with the matching
        /// method. The member's own method, when it leads there; otherwise the elders choose the best known
        /// method whose Qi is in store (never a capped one). The portion is spent, the Qi binds the cultivator.
        /// </summary>
        private bool TryAbsorbQi(CharacterData character)
        {
            var method = techniques.MethodOf(character);
            if (!TechniqueRules.Covers(method, CultivationRealm.QiRefinement))
            {
                method = techniques.MethodsFor(character)
                    .Where(m => m.Grade >= LowestGradeChosenForAMember)
                    .FirstOrDefault(m => CanEnter(m));
                if (method == null || !techniques.AssignMethod(character, method.ID)) return false;
            }

            var qi = techniques.FindQi(method.RequiredQiId);
            if (!TechniqueRules.CanEnterQiCultivation(method, qi, resources.QiPortions(qi?.Id))) return false;

            resources.ConsumeQi(qi.Id, TechniqueRules.QiPortionsToEnter(qi));
            character.QiId = qi.Id;
            if (!character.KnownTechniqueIDs.Contains(method.ID)) character.KnownTechniqueIDs.Add(method.ID);
            ctx.Log.Info($"[Cultivation] {character.FullName} absorbs the {qi.Name} with the {method.Name}.");
            return true;
        }

        private bool CanEnter(TechniqueData method)
        {
            var qi = techniques.FindQi(method.RequiredQiId);
            return TechniqueRules.CanEnterQiCultivation(method, qi, resources.QiPortions(qi?.Id));
        }
    }
}
