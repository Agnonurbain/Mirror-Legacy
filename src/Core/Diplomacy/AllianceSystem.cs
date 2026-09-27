using System;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>Diplomatic acts the player initiates: tribute and war (treaties: <see cref="TreatySystem"/>).</summary>
    public sealed class AllianceSystem
    {
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

        /// <summary>War: the relation sinks by a hundred. Answers with its refusal, or null when done.</summary>
        public string DeclareWar(string factionId)
        {
            var faction = factions.GetFactionByID(factionId);
            if (faction == null) return "puissance inconnue";

            factions.ChangeRelation(factionId, -100);
            ctx.Log.Warning($"[Alliances] The clan declares war on {faction.Name}!");
            return null;
        }
    }
}
