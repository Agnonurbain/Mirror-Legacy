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
        public const double BeastKillChance = 0.3; // a raiding beast takes a mortal (🔎 audit §4.5)

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly DeductionEngine deduction;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly BuildingSystem buildings;
        private readonly List<RandomEventData> eventTable;

        public IReadOnlyList<RandomEventData> EventTable => eventTable;

        /// <param name="table">The event table; null draws from the game's content (events.json).</param>
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
            eventTable = (table ?? ctx.Content.RandomEvents).ToList();
        }

        public List<RandomEventData> GetEligibleEvents()
        {
            return eventTable.Where(e => e.Weight > 0
                && ctx.Clock.Year >= e.MinYear
                && (e.MinPatriarchRealm == CultivationRealm.Embryonic || clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Realm >= e.MinPatriarchRealm))) // a captive is not at home
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
                    deduction.AddFragment(ctx.Rng.NextElement(), ctx.Rng.Next(1, 4), "Fragment de ruine");
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
                case RandomEventType.MonsterAttack:
                    BeastRaid();
                    break;
                case RandomEventType.Epidemic:
                    foreach (var m in clan.LivingMembers.Where(m => m.Realm <= CultivationRealm.QiRefinement).ToList())
                        stability.ApplyModifier(m, -EpidemicStabilityLoss);
                    break;
                    // Rival challenges, merchants, prodigies and marriage offers are
                    // narrated for now; their encounters arrive with combat (G3) and the living world (L6).
            }
        }

        /// <summary>
        /// A beast of the mountains raids the domain — stones lost, perhaps a mortal taken — unless an ancient pact shelters the
        /// clan's land (📚 the Fox of the Qingyan mountains, audit §4.5).
        /// </summary>
        private void BeastRaid()
        {
            string home = ctx.Content.Clan.HomeRegion;
            var guardian = ctx.Content.Patrons.FirstOrDefault(p => p.Shelters.Contains(home));
            if (guardian != null)
            {
                ctx.Log.Info($"[Events] A beast nears the domain; {guardian.Name}'s ancient pact turns it away.");
                return;
            }
            resources.ConsumeSpiritStones(ctx.Rng.Next(50, 150));
            var mortal = clan.LivingMembers.Where(m => !Characters.SpiritualOrificeRules.CanCultivate(m) && m.CaptorFaction == null)
                .OrderBy(m => m.Age).FirstOrDefault();
            if (mortal != null && ctx.Rng.Chance(BeastKillChance)) clan.Kill(mortal, DeathCause.Combat);
            ctx.Log.Warning("[Events] A beast raids the domain.");
        }
    }
}