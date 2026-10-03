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
    /// in kind and to the trusted. An ability is no thing to steal: its knowledge is given, found or deduced (the user's
    /// decision, 2026-10-03).
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

        /// <summary>The minor abilities of a lineage the clan knows, and those it does not.</summary>
        public IReadOnlyList<string> Known(string lineage) => Minors(lineage).Where(m => knowledge.Knows(FactKind.Ability, m)).ToList();
        public int UnknownCount(string lineage) => Unknown(lineage).Count;

        /// <summary>The powers whose elder holds the lineage: they know its minor abilities.</summary>
        public IReadOnlyList<FactionData> HoldersOf(string lineage) => factions.Factions.Where(f => f.Elders.Any(e => e.FruitionId == lineage)).ToList();

        /// <summary>Why the mirror cannot deduce the lineage's minor abilities now (French), or null.</summary>
        public string DeduceRefusal(string lineage)
        {
            var fruition = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            if (fruition == null) return "lignée inconnue";
            if (Unknown(lineage).Count == 0) return "le clan connaît déjà ses capacités mineures";
            if (fruition.Abilities.Where(a => !a.Substitute).Count(a => knowledge.Knows(FactKind.Ability, $"{lineage}:{a.Id}")) < GoldenCoreRules.AbilitiesToForge)
                return "il faut connaître ses cinq capacités orthodoxes";
            return mirror.PayRefusal(Settings.MirrorCost);
        }

        /// <summary>The mirror deduces the minor abilities of a lineage whose five orthodox the clan knows. Null when done, else why not (French).</summary>
        public string Deduce(string lineage)
        {
            if (DeduceRefusal(lineage) is { } why) return why;
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
    }
}
