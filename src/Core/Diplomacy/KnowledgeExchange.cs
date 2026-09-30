using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Revealing what the clan does not know (L4c; LORE.md §11 P3 « knowledge is a resource »): a power that trusts
    /// the clan sells a technique it holds (LORE.md §2.4), for stones by grade; the mirror deciphers the Dao
    /// Partners of a foundation the clan knows. Ruins and the powers' own trading come with L6.
    /// </summary>
    public sealed class KnowledgeExchange
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly TechniqueLibrary techniques;
        private readonly ResourceManager resources;
        private readonly MirrorSystem mirror;

        private readonly TreatySystem treaties;

        public KnowledgeExchange(GameContext ctx, FactionManager factions, TechniqueLibrary techniques, ResourceManager resources, MirrorSystem mirror,
            TreatySystem treaties = null)
        {
            this.treaties = treaties;
            this.ctx = ctx;
            this.factions = factions;
            this.techniques = techniques;
            this.resources = resources;
            this.mirror = mirror;
        }

        private KnowledgeTradeSettings Settings => ctx.Content.Balance.KnowledgeTrade;

        /// <summary>The techniques a power holds that the clan lacks (whatever their mood: the price is another matter).</summary>
        public IReadOnlyList<TechniqueData> Offers(string factionName)
        {
            var power = factions.GetFactionByName(factionName);
            if (power == null) return new List<TechniqueData>();
            return power.Techniques.Where(id => !techniques.Knows(id))
                .Select(id => ctx.Content.Techniques.FirstOrDefault(t => t.ID == id))
                .Where(t => t != null)
                .ToList();
        }

        /// <summary>A technique's price: by its grade; cheaper from a trade partner.</summary>
        public int PriceOf(TechniqueData technique, string factionName = null)
        {
            int price = Settings.StonesPerGrade[technique.Grade - 1];
            return treaties != null && factionName != null && treaties.Has(factionName, TreatyKind.Trade)
                ? (int)(price * ctx.Content.Balance.Treaties.TradeDiscount)
                : price;
        }

        /// <summary>Why a power will not sell this technique now; null when it will.</summary>
        public string PurchaseRefusal(string factionName, string techniqueId) =>
            Refusal(factions.GetFactionByName(factionName), Offers(factionName).FirstOrDefault(t => t.ID == techniqueId));

        private string Refusal(FactionData power, TechniqueData technique)
        {
            if (power == null) return "puissance inconnue";
            if (technique == null) return "elle ne détient pas cet art, ou le clan le connaît déjà";
            if (technique.Kind == TechniqueKind.Cultivation && Characters.TechniqueRules.HasPurpleMansionSecret(technique))
                return "une méthode du Manoir Pourpre ne s'achète pas : elle s'obtient par un accord, une opportunité ou un complot";
            if (power.RelationWithPlayer < Settings.MinRelation) return $"la relation est trop froide ({Settings.MinRelation} requise)";
            if (resources.SpiritStones < PriceOf(technique, power.Name)) return $"il faut {PriceOf(technique, power.Name)} pierres spirituelles";
            return null;
        }

        /// <summary>Buys a technique a power holds, if it trusts the clan enough and the stones are there.</summary>
        public bool BuyTechnique(string factionName, string techniqueId)
        {
            var power = factions.GetFactionByName(factionName);
            var technique = Offers(factionName).FirstOrDefault(t => t.ID == techniqueId);
            if (Refusal(power, technique) != null || !resources.ConsumeSpiritStones(PriceOf(technique, power.Name))) // no technique: refused first
            {
                ctx.Log.Warning($"[Knowledge] {factionName} does not sell « {techniqueId} » to the clan.");
                return false;
            }
            techniques.Learn(techniqueId);
            ctx.Log.Info($"[Knowledge] The clan buys « {technique.Name} » from {power.Name}.");
            return true;
        }

        /// <summary>The mirror reads the Dao Partners of a foundation the clan knows (they are revealed with it).</summary>
        public bool DecipherDaoPartners(string foundation)
        {
            var knowledge = techniques.Knowledge;
            if (!knowledge.Knows(FactKind.Ability, foundation) || knowledge.Knows(FactKind.DaoPartners, foundation)
                || !mirror.ConsumePower(Settings.DaoPartnersMirrorCost)) return false;
            return knowledge.Reveal(FactKind.DaoPartners, foundation, KnowledgeSource.Mirror);
        }
    }
}
