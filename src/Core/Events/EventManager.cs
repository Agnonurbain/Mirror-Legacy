using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// The yearly random event of the Events phase: a weighted draw among the entries whose year and
    /// realm requirements are met. The Protective Formation softens natural disasters.
    /// </summary>
    public sealed class EventManager
    {
        public const double FormationReliefPerLevel = 0.15;
        public const int VisitRelationBoost = 10;
        public const int BetrayalThreshold = 30;
        public const int BetrayalStabilityLoss = 15;
        public const int EpidemicStabilityLoss = 5;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly DeductionEngine deduction;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly BuildingSystem buildings;
        private readonly List<RandomEventData> eventTable;

        public IReadOnlyList<RandomEventData> EventTable => eventTable;

        /// <param name="table">The event table; null uses the built-in placeholder table.</param>
        public EventManager(GameContext ctx, ClanManager clan, FactionManager factions, DeductionEngine deduction,
            ResourceManager resources, MentalStabilitySystem stability, BuildingSystem buildings,
            IEnumerable<RandomEventData> table = null)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.deduction = deduction;
            this.resources = resources;
            this.stability = stability;
            this.buildings = buildings;
            eventTable = table?.ToList() ?? DefaultTable();
        }

        public List<RandomEventData> GetEligibleEvents()
        {
            return eventTable.Where(e => e.Weight > 0
                && ctx.Clock.Year >= e.MinYear
                && (e.MinPatriarchRealm == CultivationRealm.Embryonic || clan.LivingMembers.Any(m => m.Realm >= e.MinPatriarchRealm)))
                .ToList();
        }

        /// <summary>Draws and resolves this year's event; null when nothing is eligible.</summary>
        public RandomEventData TriggerYearlyEvent()
        {
            var eligible = GetEligibleEvents();
            if (eligible.Count == 0)
            {
                ctx.Log.Info("[Events] A quiet year: nothing stirs.");
                return null;
            }

            var selected = WeightedDraw(eligible);
            ctx.Log.Info($"[Events] {selected.Name}: {selected.Description}");
            Execute(selected);
            ctx.Events.TriggerRandomEventOccurred(selected);
            return selected;
        }

        private RandomEventData WeightedDraw(IReadOnlyList<RandomEventData> events)
        {
            int roll = ctx.Rng.Next(0, events.Sum(e => e.Weight));
            int cumulative = 0;
            foreach (var e in events)
            {
                cumulative += e.Weight;
                if (roll < cumulative) return e;
            }
            return events[events.Count - 1];
        }

        private void Execute(RandomEventData evt)
        {
            switch (evt.EventType)
            {
                case RandomEventType.DiplomaticVisit:
                    var visitor = factions.RandomFaction();
                    if (visitor != null) factions.ChangeRelation(visitor.ID, VisitRelationBoost);
                    break;
                case RandomEventType.RuinsDiscovery:
                    deduction.AddFragment(ctx.Rng.NextElement(), ctx.Rng.Next(1, 4), "Ruin Fragment");
                    break;
                case RandomEventType.InternalBetrayal:
                    var troubled = clan.LivingMembers.OrderBy(m => m.MentalStability).FirstOrDefault();
                    if (troubled != null && troubled.MentalStability < BetrayalThreshold)
                    {
                        stability.ApplyModifier(troubled, -BetrayalStabilityLoss);
                        ctx.Log.Warning($"[Events] {troubled.FullName} plots betrayal!");
                    }
                    break;
                case RandomEventType.NaturalDisaster:
                    double relief = Math.Min(1.0, buildings.FormationLevel * FormationReliefPerLevel);
                    resources.ConsumeSpiritStones((int)Math.Round(ctx.Rng.Next(50, 200) * (1.0 - relief)));
                    break;
                case RandomEventType.Epidemic:
                    foreach (var m in clan.LivingMembers.Where(m => m.Realm <= CultivationRealm.QiRefinement).ToList())
                        stability.ApplyModifier(m, -EpidemicStabilityLoss);
                    break;
                    // Monster attacks, rival challenges, merchants, prodigies and marriage offers are
                    // narrated for now; their encounters arrive with combat (G3) and the living world (L6).
            }
        }

        /// <summary>Placeholder table until events move to data (phase G2).</summary>
        public static List<RandomEventData> DefaultTable() => new List<RandomEventData>
        {
            Entry("Monster Attack", RandomEventType.MonsterAttack, 20, "Wild beasts assault the domain."),
            Entry("Diplomatic Visit", RandomEventType.DiplomaticVisit, 12, "A faction sends envoys."),
            Entry("Ruins Discovery", RandomEventType.RuinsDiscovery, 10, "Ancient ruins found nearby."),
            Entry("Genius Birth", RandomEventType.GeniusBirth, 5, "A prodigy appears in the region.", minYear: 5),
            Entry("Internal Betrayal", RandomEventType.InternalBetrayal, 8, "A troubled member plots."),
            Entry("Natural Disaster", RandomEventType.NaturalDisaster, 10, "Flood, earthquake or storm."),
            Entry("Wandering Merchant", RandomEventType.WanderingMerchant, 15, "A merchant with rare wares."),
            Entry("Rival Challenge", RandomEventType.RivalChallenge, 8, "A rival issues a formal challenge.", minRealm: CultivationRealm.Foundation),
            Entry("Epidemic", RandomEventType.Epidemic, 7, "Disease sweeps the domain."),
            Entry("Marriage Opportunity", RandomEventType.MarriageOpportunity, 10, "A neighbouring clan proposes an alliance."),
            Entry("Peaceful Year", RandomEventType.PeacefulYear, 30, "Nothing of note happens.")
        };

        private static RandomEventData Entry(string name, RandomEventType type, int weight, string description,
            CultivationRealm minRealm = CultivationRealm.Embryonic, int minYear = 0) =>
            new RandomEventData { Name = name, EventType = type, Weight = weight, Description = description, MinPatriarchRealm = minRealm, MinYear = minYear };
    }
}
