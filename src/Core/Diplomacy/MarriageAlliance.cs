using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// The marriage alliance (user decision 2026-09-27; LORE.md D3, D7). The clan offers a member in marriage to a power,
    /// which accepts when it gains: the kind's base and its temper, its relation, the member's realm, less what it
    /// suspects. The spouse it sends joins the clan — its eyes, perhaps (a spy). The bond of blood is a treaty: the power
    /// spares the clan (no ambush, no strike without proof), grows closer each year, betrays far less — its blood is here,
    /// a hostage. It ends with the couple; a repudiation breaks it: the spouse goes home, the power resents it, and the
    /// clan's word is worth less. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class MarriageAlliance
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TreatySystem treaties;
        private readonly MarriageSystem marriages;
        private readonly SuspicionLedger suspicion;

        public MarriageAlliance(GameContext ctx, ClanManager clan, FactionManager factions, TreatySystem treaties, MarriageSystem marriages,
            SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.treaties = treaties;
            this.marriages = marriages;
            this.suspicion = suspicion;
        }

        private TreatySettings Settings => ctx.Content.Balance.Treaties;

        /// <summary>How much a power wants to wed this member: the treaty's willingness, and the member's realm.</summary>
        public int Willingness(FactionData power, CharacterData member) =>
            TreatyRules.Willingness(power, TreatyKind.Marriage, false, treaties.ClanStrongest, suspicion.OfClan(power.Name), Settings)
            + (int)member.Realm * Settings.MarriageWorthPerRealm;

        public string Propose(string faction, string memberId)
        {
            string refusal = Refusal(faction, memberId);
            if (refusal != null) return refusal;
            var power = factions.GetFactionByName(faction);
            var member = clan.FindById(memberId);
            if (!marriages.HandleArrangedMarriage(member, power.ID, isForced: false)) return "le mariage n'a pu se faire";
            treaties.Conclude(new Treaty($"treaty-{power.ID}-Marriage-{ctx.Clock.Year}-{member.ID}", TreatyKind.Marriage, faction,
                ctx.Clock.Year, null, false, false, false) { SpouseId = member.SpouseID });
            ctx.Log.Info($"[Treaties] {member.FullName} weds into {faction}: a bond of blood.");
            return null;
        }

        /// <summary>Why this marriage cannot be now; null when the power would accept it.</summary>
        public string Refusal(string faction, string memberId)
        {
            var power = factions.GetFactionByName(faction);
            var member = clan.FindById(memberId);
            if (power == null) return "puissance inconnue";
            if (member == null || !MarriageMatchmaker.IsEligible(member) || !clan.LivingMembers.Contains(member))
                return "ce membre ne peut pas se marier";
            if (treaties.Has(faction, TreatyKind.Marriage)) return "un lien de sang les unit déjà";
            int min = Settings.MinRelation.TryGetValue(TreatyKind.Marriage, out var m) ? m : 0;
            if (power.RelationWithPlayer < min) return $"la relation est trop froide ({min} requise)";
            return Willingness(power, member) < Settings.AcceptThreshold ? "elle n'y trouve pas son compte" : null;
        }

        /// <summary>The clan sends a spouse from a power back home: the bond is broken, at a price.</summary>
        public string Repudiate(string spouseId)
        {
            var spouse = clan.FindById(spouseId);
            var treaty = treaties.All.FirstOrDefault(t => t.Kind == TreatyKind.Marriage && t.SpouseId == spouseId);
            if (spouse == null || treaty == null) return "ce conjoint ne vient pas d'une alliance";
            treaties.Break(treaty.Id); // a broken word
            var power = factions.GetFactionByName(treaty.Faction);
            if (power != null) factions.ChangeRelation(power.ID, Settings.RepudiationRelation); // and an insult
            var partner = clan.FindById(spouse.SpouseID);
            if (partner != null) partner.SpouseID = null;
            spouse.SpouseID = null;
            clan.Depart(spouse);
            ctx.Log.Warning($"[Treaties] The clan sends {spouse.FullName} back to {treaty.Faction}.");
            return null;
        }
    }
}
