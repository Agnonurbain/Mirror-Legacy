using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>Pure odds of the four trials of the Purple Mansion (LORE.md §5.4.1), from balance.json.</summary>
    public static class PurpleMansionRules
    {
        private const int RootPointsPerPercent = 5;
        private const int AverageRoot = 50;
        private const int LowStabilityThreshold = 50;
        private const int LowStabilityPenalty = 20;
        private const int ManifestationPerGrade = 5;
        private const int ManifestationPerTechnique = 2;
        private const int ManifestationTechniqueCap = 5;
        private const int ReferenceGrade = 5;

        /// <summary>The Ascent to the Shenyang Mansion: talent carries it, a troubled mind betrays it.</summary>
        public static int AscentChance(CharacterData c, PurpleMansionSettings s)
        {
            int chance = s.AscentBaseChance + (c.SpiritualRoot - AverageRoot) / RootPointsPerPercent;
            if (c.MentalStability < LowStabilityThreshold) chance -= LowStabilityPenalty;
            return Clamp(chance);
        }

        /// <summary>
        /// The Manifestation: « the higher the foundation's degree, the more secret techniques cultivated and the
        /// deeper the Dao, the easier » — the method's grade, the techniques one knows, one's talent.
        /// </summary>
        public static int ManifestationChance(CharacterData c, TechniqueData method, PurpleMansionSettings s)
        {
            int grade = method?.Grade ?? ReferenceGrade;
            int techniques = Math.Min(ManifestationTechniqueCap, Math.Max(0, (c.KnownTechniqueIDs?.Count ?? 0) - 1));
            int chance = s.ManifestationBaseChance + (grade - ReferenceGrade) * ManifestationPerGrade
                + techniques * ManifestationPerTechnique + (c.SpiritualRoot - AverageRoot) / RootPointsPerPercent;
            return Clamp(chance);
        }

        /// <summary>The Illusions: forgetting oneself in the dark takes a steady mind.</summary>
        public static int IllusionsChance(CharacterData c, PurpleMansionSettings s) =>
            Clamp(s.IllusionsBaseChance + (c.MentalStability - AverageRoot) / 2);

        /// <summary>How long the Great Void holds a cultivator: years of a band, or for life past the last band.</summary>
        public static (int Years, bool ForLife) DrawVoid(Random rng, IReadOnlyList<VoidBand> bands)
        {
            double roll = rng.NextDouble();
            double cumulative = 0;
            foreach (var band in bands)
            {
                cumulative += band.Chance;
                if (roll < cumulative)
                    return (band.MinYears == band.MaxYears ? band.MinYears : rng.Next(band.MinYears, band.MaxYears + 1), false);
            }
            return (0, true);
        }

        private static int Clamp(int chance) => Math.Max(1, Math.Min(99, chance));
    }

    /// <summary>
    /// The breakthrough to the Purple Mansion (LORE.md §5.4.1), resolved at each Breakthrough phase: the
    /// Ascent (death on failure), then the Manifestation retreat (about six years; a failure falls back to
    /// the Foundation's apogee), the Great Void (days to decades, or for life), the Illusions (a failure costs
    /// one's whole cultivation). Success brings the Purple Mansion with the foundation as first divine ability.
    /// </summary>
    public sealed class PurpleMansionSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly CultivationSystem cultivation;
        private readonly TechniqueLibrary techniques;

        public PurpleMansionSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation, TechniqueLibrary techniques)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.cultivation = cultivation;
            this.techniques = techniques;
        }

        private PurpleMansionSettings Settings => ctx.Content.Balance.PurpleMansion;

        /// <summary>Advances every retreat, then sends into retreat whoever is ready to ascend.</summary>
        public void ProcessBreakthroughPhase()
        {
            foreach (var member in clan.LivingMembers.Where(m => m.Retreat != Retreat.None).ToList())
                AdvanceRetreat(member);

            foreach (var member in clan.LivingMembers.Where(IsReadyToAscend).ToList())
                Ascend(member);
        }

        private bool IsReadyToAscend(CharacterData member) =>
            member.Retreat == Retreat.None
            && PowerLadder.Next(member.Realm, member.RealmStage).Trial == TrialKind.PurpleMansionAscension
            && cultivation.IsReadyForTrial(member);

        private void Ascend(CharacterData member)
        {
            int chance = PurpleMansionRules.AscentChance(member, Settings);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                ctx.Log.Info($"[Purple Mansion] {member.FullName} is exhausted before the Shenyang Mansion ({chance}%).");
                clan.Kill(member, DeathCause.AscentCollapse);
                return;
            }

            member.Retreat = Retreat.Manifestation;
            member.RetreatYearsLeft = Settings.ManifestationYears;
            member.CurrentTask = TaskType.None;
            ctx.Log.Info($"[Purple Mansion] {member.FullName} reaches the Shenyang point and withdraws to manifest their divine power.");
        }

        private void AdvanceRetreat(CharacterData member)
        {
            if (member.ImprisonedInVoid) return; // lost in the darkness until the end of their life
            if (--member.RetreatYearsLeft > 0) return;

            if (member.Retreat == Retreat.Manifestation) EndManifestation(member);
            else EndGreatVoid(member);
        }

        private void EndManifestation(CharacterData member)
        {
            int chance = PurpleMansionRules.ManifestationChance(member, techniques.MethodOf(member), Settings);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                member.Retreat = Retreat.None;
                member.RetreatYearsLeft = 0;
                member.CultivationXP = 0;
                ctx.Log.Info($"[Purple Mansion] {member.FullName} fails to manifest their divine power and falls back to the Foundation's apogee.");
                return;
            }

            var (years, forLife) = PurpleMansionRules.DrawVoid(ctx.Rng, Settings.VoidBands);
            member.Retreat = Retreat.GreatVoid;
            member.ImprisonedInVoid = forLife;
            member.RetreatYearsLeft = Math.Max(1, years); // resolved at the next Breakthrough phase at the soonest
            ctx.Log.Info(forLife
                ? $"[Purple Mansion] {member.FullName} enters the Great Void and will never find the way out."
                : $"[Purple Mansion] {member.FullName} propels the Shenyang Mansion into the Great Void.");
        }

        private void EndGreatVoid(CharacterData member)
        {
            member.Retreat = Retreat.None;
            member.RetreatYearsLeft = 0;

            int chance = PurpleMansionRules.IllusionsChance(member, Settings);
            if (ctx.Rng.Next(1, 101) > chance)
            {
                LoseAllCultivation(member);
                ctx.Log.Info($"[Purple Mansion] {member.FullName} is lost among the illusions and loses all cultivation.");
                return;
            }

            var step = PowerLadder.Next(member.Realm, member.RealmStage);
            member.CultivationXP = Math.Max(0, member.CultivationXP - PowerLadder.XpForNextStage(member.Realm));
            member.DivineAbilities.Clear();
            if (member.FoundationId != null) member.DivineAbilities.Add(member.FoundationId); // the foundation becomes the first ability
            cultivation.ApplyStep(member, step);
            member.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(member.DivineAbilities.Count);
            ctx.Events.TriggerBreakthroughSuccess(member, member.Realm);
        }

        /// <summary>Back to a body with an orifice and nothing more, with a mortal's lifespan.</summary>
        private void LoseAllCultivation(CharacterData member)
        {
            member.Realm = CultivationRealm.Embryonic;
            member.RealmStage = 0;
            member.CultivationXP = 0;
            member.FoundationId = null;
            member.QiId = null;
            member.DivineAbilities.Clear();
            member.MaxLifespan = PowerLadder.WoundedLifespan(SpiritualOrificeRules.MortalLifespan(ctx.Rng.NextDouble()), member.DaoWounds);
        }
    }
}
