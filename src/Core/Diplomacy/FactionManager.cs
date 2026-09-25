using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// The world of a new game: a copy of every faction of the content (factions.json), each with a
        /// seeded ID. (The renamed factions of LORE.md §7 arrive with phase L5.)
        /// </summary>
        public void InitializeFactions()
        {
            foreach (var template in ctx.Content.Factions)
            {
                var faction = template.Clone(); // the content is shared: never mutate it
                faction.ID = ctx.Rng.NextId();
                AddFaction(faction);
            }
        }

        public FactionData GetFactionByID(string id) => factions.Find(f => f.ID == id);

        /// <summary>The faction as named in the content (the stable key content refers to).</summary>
        public FactionData GetFactionByName(string name) =>
            string.IsNullOrEmpty(name) ? null : factions.Find(f => f.Name == name);

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

        /// <summary>Restores saved factions; a save from before FamilyName gets it back from the content.</summary>
        public void Restore(IEnumerable<FactionData> saved)
        {
            factions.Clear();
            factions.AddRange(saved);
            foreach (var faction in factions.Where(f => f.FamilyName == null))
                faction.FamilyName = ctx.Content.Factions.FirstOrDefault(t => t.Name == faction.Name)?.FamilyName;
        }
    }
}
