using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The minor abilities of former True Monarchs (LORE.md §5.5.1, R2; L4e, user decisions 2026-10-03): five abilities of a
    /// lineage with a minor one lead to its Surplus. They are rare knowledge: a tomb or ruins reveal one; the mirror deduces
    /// those of a lineage whose five orthodox abilities the clan knows; a power whose elder holds the lineage may teach one,
    /// for stones and good relations, or be robbed of it — at the risk of being caught.
    /// </summary>
    public sealed class MinorAbilities
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly MirrorSystem mirror;
        private readonly KnowledgeBase knowledge;
        private readonly KnowledgeAccords accords;

        public MinorAbilities(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            MirrorSystem mirror, KnowledgeBase knowledge, KnowledgeAccords accords)
        {
            this.accords = accords;
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.mirror = mirror;
            this.knowledge = knowledge;
            ctx.Events.OnTombLooted += () => { if (ctx.Rng.Chance(Settings.TombRevealChance)) RevealOne(null, KnowledgeSource.Event); };
            ctx.Events.OnRandomEventOccurred += e => { if (e.EventType == RandomEventType.RuinsDiscovery) SearchTheRuins(); };
        }

        private MinorAbilitySettings Settings => ctx.Content.Balance.MinorAbilities;

        private IEnumerable<string> Minors(string lineage) =>
            ctx.Content.Fruitions.Where(f => lineage == null || f.Id == lineage)
                .SelectMany(f => f.Abilities.Where(a => a.Substitute).Select(a => $"{f.Id}:{a.Id}"));

        private IReadOnlyList<string> Unknown(string lineage) => Minors(lineage).Where(m => !knowledge.Knows(FactKind.Ability, m)).ToList();

        /// <summary>One minor ability not yet known (of a lineage, or of any), revealed; false when none is left.</summary>
        private bool RevealOne(string lineage, KnowledgeSource source)
        {
            var unknown = Unknown(lineage);
            if (unknown.Count == 0) return false;
            string minor = unknown[ctx.Rng.Next(unknown.Count)];
            knowledge.Reveal(FactKind.Ability, minor, source);
            ctx.Log.Info($"[Minors] The clan learns the minor ability {minor}.");
            ctx.Events.TriggerMinorAbilityLearnt(minor);
            return true;
        }

        public void SearchTheRuins()
        {
            if (ctx.Rng.Chance(Settings.RuinsRevealChance)) RevealOne(null, KnowledgeSource.Event);
        }

        /// <summary>The mirror deduces the minor abilities of a lineage whose five orthodox the clan knows. Null when done, else why not (French).</summary>
        public string Deduce(string lineage)
        {
            var fruition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            if (fruition == null) return "lignée inconnue";
            if (fruition.Abilities.Where(a => !a.Substitute).Count(a => knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}")) < GoldenCoreRules.AbilitiesToForge)
                return "il faut connaître ses cinq capacités orthodoxes";
            if (Unknown(lineage).Count == 0) return "le clan connaît déjà ses capacités mineures";
            if (mirror.PayRefusal(Settings.MirrorCost) is { } why) return why;
            mirror.ConsumePower(Settings.MirrorCost);
            while (RevealOne(lineage, KnowledgeSource.Mirror)) { }
            return null;
        }

        private FactionData Holder(string lineage, string powerName)
        {
            var power = factions.GetFactionByName(powerName);
            return power != null && power.Elders.Any(e => e.FruitionId == lineage) ? power : null;
        }

        /// <summary>
        /// A power whose elder holds the lineage teaches one of its minor abilities — for what it wants in kind, never for
        /// stones: too precious (the user's rule, 2026-10-03). Null when done, else why not (French).
        /// </summary>
        public string Buy(string lineage, string powerName, IReadOnlyList<AccordTerm> terms)
        {
            var power = Holder(lineage, powerName);
            if (power == null) return "seule une puissance qui tient la lignée connaît ses capacités mineures";
            if (Unknown(lineage).Count == 0) return "le clan connaît déjà ses capacités mineures";
            if (power.RelationWithPlayer < Settings.BuyRelation) return $"{power.Name} ne livre pas ce savoir au clan";
            if (accords.BarterRefusal(power.Name, Settings.Worth, precious: true, terms) is { } why) return why;
            accords.Barter(power.Name, terms, $"une capacité mineure de {lineage}");
            RevealOne(lineage, KnowledgeSource.Trade);
            return null;
        }

        /// <summary>The clan steals a minor ability from a power holding the lineage. Null when done, else why not (French).</summary>
        public string Steal(string lineage, string powerName, IReadOnlyList<string> teamIds)
        {
            var power = Holder(lineage, powerName);
            if (power == null) return "seule une puissance qui tient la lignée connaît ses capacités mineures";
            if (Unknown(lineage).Count == 0) return "le clan connaît déjà ses capacités mineures";
            var team = (teamIds ?? new List<string>()).Distinct().Select(clan.FindById).ToList();
            var ops = ctx.Content.Balance.Shards;
            if (team.Count == 0 || team.Count > ops.ExpeditionMaxTeam || team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null
                || m.Realm < CultivationRealm.QiRefinement || m.LastOperationYear == ctx.Clock.Year))
                return $"un vol se fait à 1 à {ops.ExpeditionMaxTeam} cultivateurs libres";
            foreach (var m in team) m.LastOperationYear = ctx.Clock.Year;
            var powers = team.Select(m => (double)HuntRules.Power(m)).OrderByDescending(p => p).ToList();
            double strength = powers[0] + powers.Skip(1).Sum() * 0.3;
            double chance = System.Math.Clamp(ops.TheftBaseChance + (strength - HuntRules.Power(power.HighestRealm, 5)) * ops.TheftChancePerPower,
                ops.TheftMinChance, ops.TheftMaxChance);
            if (ctx.Rng.Chance(chance))
            {
                RevealOne(lineage, KnowledgeSource.Espionage);
                return null;
            }
            suspicion.AddEvidence(power.Name, Settings.CaughtEvidence);
            factions.ChangeRelation(power.ID, Settings.CaughtRelation);
            return "le vol échoue, et le clan est vu";
        }
    }
}
