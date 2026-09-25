using System;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>Diplomatic acts the player initiates: tribute, non-aggression pacts, war.</summary>
    public sealed class AllianceSystem
    {
        public const int NonAggressionBonus = 10;
        private const double MerchantTributeMultiplier = 1.5;

        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly ResourceManager resources;

        public AllianceSystem(GameContext ctx, FactionManager factions, ResourceManager resources)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.resources = resources;
        }

        /// <summary>+5 relation plus 1 per 100 stones; merchants value it half as much again.</summary>
        public bool OfferTribute(string factionId, int spiritStones)
        {
            var faction = factions.GetFactionByID(factionId);
            if (faction == null || !resources.ConsumeSpiritStones(spiritStones)) return false;

            int boost = 5 + spiritStones / 100;
            if (faction.Personality == FactionPersonality.Merchant)
                boost = (int)Math.Round(boost * MerchantTributeMultiplier);

            factions.ChangeRelation(factionId, boost);
            ctx.Log.Info($"[Alliances] Tribute of {spiritStones} stones to {faction.Name} (+{boost}).");
            return true;
        }

        /// <summary>Accepted from neutral or better relations.</summary>
        public bool ProposeNonAggression(string factionId)
        {
            var faction = factions.GetFactionByID(factionId);
            if (faction == null || faction.RelationWithPlayer < 0) return false;

            factions.ChangeRelation(factionId, NonAggressionBonus);
            ctx.Log.Info($"[Alliances] {faction.Name} accepts a non-aggression pact.");
            return true;
        }

        public void DeclareWar(string factionId)
        {
            var faction = factions.GetFactionByID(factionId);
            if (faction == null) return;

            factions.ChangeRelation(factionId, -100);
            ctx.Log.Warning($"[Alliances] The clan declares war on {faction.Name}!");
        }
    }
}
