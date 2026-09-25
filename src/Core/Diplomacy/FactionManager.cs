using System;
using System.Collections.Generic;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The world's factions and how each one treats the clan, year after year.</summary>
    public sealed class FactionManager
    {
        public const int MinRelation = -100;
        public const int MaxRelation = 100;

        private readonly GameContext ctx;
        private readonly List<FactionData> factions = new List<FactionData>();

        public IReadOnlyList<FactionData> Factions => factions;

        public FactionManager(GameContext ctx)
        {
            this.ctx = ctx;
        }

        public void AddFaction(FactionData faction) => factions.Add(faction);

        /// <summary>Placeholder world until the renamed factions of LORE.md §7 move to data (phases G2 and L5).</summary>
        public void InitializeDefaultFactions()
        {
            AddFaction(new FactionData { Name = "Wang Family", Personality = FactionPersonality.Aggressive, PowerLevel = 500, Wealth = 1000, RelationWithPlayer = -20 });
            AddFaction(new FactionData { Name = "Zhao Merchant Guild", Personality = FactionPersonality.Merchant, PowerLevel = 200, Wealth = 5000, RelationWithPlayer = 10 });
            AddFaction(new FactionData { Name = "Azure Cloud Sect", Personality = FactionPersonality.Isolationist, PowerLevel = 5000, Wealth = 2000, RelationWithPlayer = 0 });
            AddFaction(new FactionData { Name = "Iron Fist Hall", Personality = FactionPersonality.Aggressive, PowerLevel = 800, Wealth = 600, RelationWithPlayer = -10 });
            AddFaction(new FactionData { Name = "Jade Phoenix Pavilion", Personality = FactionPersonality.Merchant, PowerLevel = 300, Wealth = 8000, RelationWithPlayer = 15 });
            AddFaction(new FactionData { Name = "Shadow Veil Sect", Personality = FactionPersonality.Manipulative, PowerLevel = 1200, Wealth = 1500, RelationWithPlayer = -5 });
            AddFaction(new FactionData { Name = "Verdant Bamboo Hermitage", Personality = FactionPersonality.Isolationist, PowerLevel = 700, Wealth = 400, RelationWithPlayer = 5 });
            AddFaction(new FactionData { Name = "Golden Sun Empire", Personality = FactionPersonality.Expansionist, PowerLevel = 10000, Wealth = 20000, RelationWithPlayer = 0 });
        }

        public FactionData GetFactionByID(string id) => factions.Find(f => f.ID == id);

        public FactionData RandomFaction() => ctx.Rng.Pick(factions);

        public void ChangeRelation(string factionId, int amount)
        {
            var faction = GetFactionByID(factionId);
            if (faction == null) return;
            faction.RelationWithPlayer = Math.Clamp(faction.RelationWithPlayer + amount, MinRelation, MaxRelation);
        }

        /// <summary>Each faction acts on its personality during the Events phase.</summary>
        public void ProcessYearlyFactionAI()
        {
            foreach (var faction in factions)
            {
                switch (faction.Personality)
                {
                    case FactionPersonality.Aggressive:
                        if (faction.RelationWithPlayer <= -80)
                            ctx.Log.Warning($"[Factions] {faction.Name} declares war!");
                        else if (faction.RelationWithPlayer < -30)
                            ChangeRelation(faction.ID, -5);
                        break;
                    case FactionPersonality.Merchant:
                        if (faction.RelationWithPlayer >= 50)
                            ctx.Log.Info($"[Factions] {faction.Name} offers a trade deal.");
                        else
                            ChangeRelation(faction.ID, +2);
                        break;
                    case FactionPersonality.Manipulative:
                        if (faction.RelationWithPlayer < 0)
                        {
                            ctx.Log.Warning($"[Factions] {faction.Name} spreads rumours about the clan.");
                            ChangeRelation(faction.ID, -3);
                        }
                        break;
                    case FactionPersonality.Expansionist:
                        faction.PowerLevel += 100;
                        if (faction.RelationWithPlayer < -50)
                            ctx.Log.Warning($"[Factions] {faction.Name} eyes the clan's territory.");
                        break;
                    case FactionPersonality.Isolationist:
                        if (faction.RelationWithPlayer > 0) ChangeRelation(faction.ID, -1);
                        else if (faction.RelationWithPlayer < 0) ChangeRelation(faction.ID, +1);
                        break;
                }
            }
        }

        public void Restore(IEnumerable<FactionData> saved)
        {
            factions.Clear();
            factions.AddRange(saved);
        }
    }
}
