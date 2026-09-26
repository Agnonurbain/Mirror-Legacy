using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

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
            if (character.ProgressionSealed) return;                     // a consumed Dao Partner: no further, even cultivating

            double multiplier = SpeedOf(character);
            if (multiplier <= 0) return; // no method guides this realm

            int gain = BaseYearlyXp + character.SpiritualRoot / 2 + karma.GetBonusXP();
            GrantXp(character, (int)Math.Round(gain * multiplier));
        }

        /// <summary>
        /// How fast a member cultivates: the method's speed for the realm (0 when none guides it), the clan's karma,
        /// the Dao Heart's alignment, a talisman Qi, a Heart Demon, a troubled mind.
        /// </summary>
        public double SpeedOf(CharacterData character)
        {
            double speed = TechniqueRules.CultivationSpeed(techniques.MethodOf(character), character.Realm, ctx.Content.Balance);
            if (speed <= 0) return 0;

            double heart = FoundationRules.HeartAlignmentSpeed(character.Temperament,
                FoundationRules.FruitionOf(character.FoundationId, ctx.Content.Fruitions), ctx.Content.Balance);
            double multiplier = (1.0 + karma.GetCultivationSpeedBonus()) * speed * heart;
            multiplier *= Mirror.TalismanRules.Of(character, ctx.Content.Talismans)?.CultivationSpeed ?? 1.0; // a talisman Qi (§11.5)
            if (character.HeartDemonYearsLeft > 0) multiplier *= ctx.Content.Balance.Oaths.HeartDemonSpeed; // an oath broken (L4d)
            if (character.MentalStability < LowStabilityThreshold)
                multiplier *= LowStabilityMultiplier;
            return multiplier;
        }

        /// <summary>Adds XP from any source (cultivation, study, teaching, buildings) and climbs free sub-levels.</summary>
        public void GrantXp(CharacterData character, int amount)
        {
            if (amount <= 0 || !SpiritualOrificeRules.CanCultivate(character) || character.ProgressionSealed) return;
            character.CultivationXP += amount;
            AdvanceSubLevels(character);
        }

        /// <summary>
        /// Spends XP on every sub-level that needs no trial; stops before a trial, an unavailable step, a realm
        /// the method does not lead to, or Qi Cultivation without the method's Qi.
        /// </summary>
        public void AdvanceSubLevels(CharacterData character)
        {
            if (!SpiritualOrificeRules.CanCultivate(character) || character.ProgressionSealed) return;

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
                && !character.ProgressionSealed
                && AllowsNextStep(character, step)
                && HasTrialQi(character, step.Trial)
                && character.CultivationXP >= PowerLadder.XpForNextStage(character.Realm);
        }

        /// <summary>The Foundation wall absorbs a portion of the cultivator's own Qi (LORE.md §2.5); other trials need none.</summary>
        public bool HasTrialQi(CharacterData character, TrialKind trial) =>
            trial != TrialKind.FoundationWall || resources.QiPortions(character.QiId) >= ctx.Content.Balance.Techniques.FoundationQiPortions;

        /// <summary>Spends what the trial absorbs, whatever its outcome; false when the Qi is lacking.</summary>
        public bool PayTrialQi(CharacterData character, TrialKind trial) =>
            trial != TrialKind.FoundationWall || resources.ConsumeQi(character.QiId, ctx.Content.Balance.Techniques.FoundationQiPortions);

        /// <summary>True when the member's method leads to the step (LORE.md §2.2); Embryonic Breathing needs no manual.</summary>
        public bool AllowsNextStep(CharacterData character, AdvancementStep step)
        {
            var method = techniques.MethodOf(character);
            if (TechniqueRules.AllowsAdvance(method, character.Realm, step.TargetRealm)) return true;
            // a Realization's descendant reaches at least the Purple Mansion, secret or not (§5.5.2)
            return character.TransformedLineage && step.TargetRealm == CultivationRealm.PurpleMansion && method != null;
        }

        /// <summary>
        /// Moves the character to the step; entering the Foundation forms the foundation of their Qi; the new
        /// realm's reach raises the lifespan, Dao wounds persist, and a flawed method keeps its toll.
        /// </summary>
        public void ApplyStep(CharacterData character, AdvancementStep step)
        {
            if (character.Realm == CultivationRealm.QiRefinement && step.TargetRealm == CultivationRealm.Foundation)
            {
                character.FoundationId = techniques.FindQi(character.QiId)?.Foundation; // the chakras fuse into the Qi's foundation (§5.3.1)
                techniques.Knowledge.Reveal(FactKind.Ability, character.FoundationId, KnowledgeSource.Formed);
                character.BodyTrait = FoundationRules.FruitionOf(character.FoundationId, ctx.Content.Fruitions)?.BodyTrait ?? character.BodyTrait; // inhuman in the Dao's image (§5.3.2)
            }
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
                    .Where(m => m.Grade >= ctx.Content.Balance.Techniques.LowestGradeChosenForAMember) // never a capped method on their behalf
                    .FirstOrDefault(m => CanEnter(m));
                if (method == null || !techniques.AssignMethod(character, method.ID)) return false;
            }

            var qi = techniques.FindQi(method.RequiredQiId);
            if (!TechniqueRules.CanEnterQiCultivation(method, qi, resources.QiPortions(qi?.Id), ctx.Content.Balance.Techniques)) return false;

            resources.ConsumeQi(qi.Id, TechniqueRules.QiPortionsToEnter(qi, ctx.Content.Balance.Techniques));
            character.QiId = qi.Id;
            if (!character.KnownTechniqueIDs.Contains(method.ID)) character.KnownTechniqueIDs.Add(method.ID);
            ctx.Log.Info($"[Cultivation] {character.FullName} absorbs the {qi.Name} with the {method.Name}.");
            return true;
        }

        private bool CanEnter(TechniqueData method)
        {
            var qi = techniques.FindQi(method.RequiredQiId);
            return TechniqueRules.CanEnterQiCultivation(method, qi, resources.QiPortions(qi?.Id), ctx.Content.Balance.Techniques);
        }
    }
}
