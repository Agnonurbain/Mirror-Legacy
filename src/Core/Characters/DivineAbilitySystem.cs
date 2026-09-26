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
    /// The divine abilities of the Purple Mansion (LORE.md §5.4.3-5.4.4). After the first (the foundation), each
    /// is condensed from a Dao Partner of one's lineage: by cultivating a technique aligned on it to the Purple
    /// Mansion (the realm's XP and a portion of its Qi), by resources (half the time, shallow foundations), or
    /// by the Dao Graft (a clan member's foundation consumed, completed with spiritual objects). The fourth is
    /// the Threshold of Immortality. The stage follows the count: 1-2 early, 3 middle, 4 late, 5 Grand Perfection.
    /// Only abilities the lore names and the clan knows can be pursued (P3); abilities of other lineages come
    /// with the Golden Core's Intercalary (L4b).
    /// </summary>
    public sealed class DivineAbilitySystem
    {
        public const int MaxAbilities = 5;
        private const int ThresholdAbility = 4; // the fourth: the Threshold of Immortality

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly TechniqueLibrary techniques;
        private readonly ResourceManager resources;

        public DivineAbilitySystem(GameContext ctx, ClanManager clan, TechniqueLibrary techniques, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.techniques = techniques;
            this.resources = resources;
        }

        private DivineAbilitySettings Settings => ctx.Content.Balance.DivineAbilities;

        private TrialModifiers Modifiers => ctx.Content.Balance.TrialModifiers;

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        /// <summary>Sets the ability a Purple Mansion cultivator condenses next.</summary>
        public bool Pursue(CharacterData member, string ability)
        {
            if (!CanTake(member, ability))
            {
                ctx.Log.Warning($"[Abilities] {member.FullName} cannot pursue \"{ability}\".");
                return false;
            }
            member.PursuedAbility = ability;
            return true;
        }

        /// <summary>
        /// Each Breakthrough phase: whoever has the realm's XP condenses the pursued ability, if the clan knows a
        /// technique aligned on it and holds a portion of its Qi. Members without a choice take the first
        /// revealed partner they can cultivate.
        /// </summary>
        public void ProcessBreakthroughPhase()
        {
            foreach (var member in clan.LivingMembers.Where(m => m.Realm == CultivationRealm.PurpleMansion && m.Retreat == Retreat.None).ToList())
            {
                if (member.DivineAbilities.Count >= MaxAbilities || member.CultivationXP < Xp) continue;
                if (member.PursuedAbility == null || !CanTake(member, member.PursuedAbility))
                    member.PursuedAbility = FirstCultivable(member);

                var qi = AlignedQi(member.PursuedAbility);
                if (qi == null || !resources.ConsumeQi(qi.Id, ctx.Content.Balance.Techniques.AlignedQiPortions)) continue; // no aligned technique, or its Qi is lacking

                // The Qi and the XP go into the attempt: they are spent even if the Threshold stops it
                member.CultivationXP -= Xp;
                if (!PassThreshold(member)) continue;
                Condense(member, member.PursuedAbility);
            }
        }

        /// <summary>Rare spiritual objects and pills condense the ability at half the XP, but its foundations stay shallow.</summary>
        public bool CondenseWithResources(CharacterData member, string ability)
        {
            var s = Settings;
            if (!CanTake(member, ability) || member.CultivationXP < Xp / 2
                || resources.SpiritStones < s.ResourceStones || resources.MedicinalHerbs < s.ResourceHerbs || resources.SpiritualOres < s.ResourceOres)
            {
                ctx.Log.Warning($"[Abilities] {member.FullName} cannot condense \"{ability}\" with resources.");
                return false;
            }

            resources.ConsumeSpiritStones(s.ResourceStones);
            resources.ConsumeHerbs(s.ResourceHerbs);
            resources.ConsumeOres(s.ResourceOres);
            member.CultivationXP -= Xp / 2;
            if (!PassThreshold(member)) return true; // paid and tried: the threshold stopped them

            Condense(member, ability);
            member.ShallowAbilities.Add(ability);
            return true;
        }

        /// <summary>
        /// The Dao Graft (§5.4.3), the norm in the South: a clan member's foundation, a Dao Partner of the
        /// cultivator's lineage, is consumed and completed with spiritual objects into a divine ability. The
        /// donor loses all cultivation and has only a few years left (user decision, 2026-09-25: one to five).
        /// </summary>
        public bool GraftDaoPartner(CharacterData member, CharacterData donor)
        {
            if (donor == null || donor == member || !donor.IsAlive || donor.Realm != CultivationRealm.Foundation
                || !CanTake(member, donor.FoundationId) || resources.SpiritualOres < Settings.GraftOres)
            {
                ctx.Log.Warning($"[Abilities] {member.FullName} cannot graft {donor?.FullName}'s foundation.");
                return false;
            }

            // The donor's foundation is consumed during the attempt: it is lost even if the Threshold then fails
            resources.ConsumeOres(Settings.GraftOres);
            string ability = donor.FoundationId;
            ctx.Events.TriggerHarm(member, donor); // an oath between them is broken
            FoundationRules.StripCultivation(donor);
            donor.MaxLifespan = donor.Age + ctx.Rng.Next(Settings.GraftDonorMinYearsLeft, Settings.GraftDonorMaxYearsLeft + 1);
            ctx.Log.Info($"[Abilities] {member.FullName} consumes {donor.FullName}'s foundation in a Dao Graft.");
            if (!PassThreshold(member)) return true;

            Condense(member, ability);
            member.GraftedAbilities.Add(ability);
            return true;
        }

        /// <summary>A revealed ability of the cultivator's own lineage, not held yet, while fewer than five are.</summary>
        private bool CanTake(CharacterData member, string ability)
        {
            if (member == null || !member.IsAlive || member.Realm != CultivationRealm.PurpleMansion || member.Retreat != Retreat.None) return false;
            if (ability == null || member.DivineAbilities.Count >= MaxAbilities || member.DivineAbilities.Contains(ability)) return false;

            var (lineage, abilityId) = FoundationRef.Parse(ability);
            var (ownLineage, _) = FoundationRef.Parse(member.FoundationId ?? member.DivineAbilities.FirstOrDefault());
            if (lineage == null || lineage != ownLineage) return false;

            var definition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage)?.Abilities.FirstOrDefault(a => a.Id == abilityId);
            return definition?.Name != null                                        // the lore names it,
                && techniques.Knowledge.Knows(FactKind.Ability, ability);           // and the clan knows it (P3)
        }

        /// <summary>The Qi of a known technique aligned on the ability (its Qi builds that foundation), or null.</summary>
        private QiDefinition AlignedQi(string ability) =>
            ability == null ? null : techniques.Known
                .Where(t => t.Kind == TechniqueKind.Cultivation && t.RequiredQiId != null)
                .Select(t => techniques.FindQi(t.RequiredQiId))
                .FirstOrDefault(q => q?.Foundation == ability);

        private string FirstCultivable(CharacterData member)
        {
            var (lineage, _) = FoundationRef.Parse(member.FoundationId ?? member.DivineAbilities.FirstOrDefault());
            var fruition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            return fruition?.Abilities.Select(a => $"{lineage}:{a.Id}")
                .FirstOrDefault(a => CanTake(member, a) && AlignedQi(a) != null);
        }

        /// <summary>The fourth ability, the Threshold of Immortality, stops most cultivators before the Golden Core.</summary>
        private bool PassThreshold(CharacterData member)
        {
            if (member.DivineAbilities.Count + 1 != ThresholdAbility) return true;

            int chance = Math.Max(1, Math.Min(99, Settings.ThresholdBaseChance + (member.SpiritualRoot - Modifiers.AverageRoot) / Modifiers.RootPointsPerPercent));
            if (ctx.Rng.Next(1, 101) <= chance) return true;

            member.CultivationXP = 0;
            ctx.Log.Info($"[Abilities] {member.FullName} fails at the Threshold of Immortality ({chance}%).");
            return false;
        }

        private void Condense(CharacterData member, string ability)
        {
            member.DivineAbilities.Add(ability);
            techniques.Knowledge.Reveal(FactKind.Ability, ability, KnowledgeSource.Condensed);
            member.PursuedAbility = null;
            member.RealmStage = PowerLadder.PurpleMansionStageFromAbilities(member.DivineAbilities.Count);
            ctx.Log.Info($"[Abilities] {member.FullName} condenses a divine ability ({member.DivineAbilities.Count}/{MaxAbilities}).");
            ctx.Events.TriggerBreakthroughSuccess(member, member.Realm);
        }
    }
}
