using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Marriages for love (within the clan or with a wandering cultivator), political marriages with a
    /// faction, and the annual marriages that keep the lineage alive. Every spouse joins the clan.
    /// </summary>
    public sealed class MarriageSystem
    {
        public const int LoveStability = 10;
        public const int ForcedStability = -15;
        public const int WillingStability = 5;
        public const int ArrangedRelationBoost = 25;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly MentalStabilitySystem stability;

        public MarriageSystem(GameContext ctx, ClanManager clan, FactionManager factions, MentalStabilitySystem stability)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.stability = stability;
        }

        /// <summary>
        /// Called during the Events phase (before Inheritance, so this year's newlyweds can have children).
        /// Returns how many couples were formed.
        /// </summary>
        public int ProcessAnnualMarriages()
        {
            var plans = MarriageMatchmaker.PlanAnnualMarriages(
                clan.LivingMembers.ToList(), clan.FindById, MarriageMatchmaker.AnnualMarriageChance, ctx.Rng);

            int married = 0;
            foreach (var plan in plans)
            {
                if (HandleLoveMarriage(plan.Member, plan.Spouse)) married++;
                else ctx.Log.Warning($"[Marriage] The match of {plan.Member.FullName} and {plan.Spouse.FullName} failed validation.");
            }
            return married;
        }

        /// <summary>Both alive, adult, unmarried, and no common ancestor within three generations.</summary>
        public bool CanMarry(CharacterData a, CharacterData b)
        {
            if (a == null || b == null || !a.IsAlive || !b.IsAlive) return false;
            if (a.Age < MarriageMatchmaker.MinMarriageAge || b.Age < MarriageMatchmaker.MinMarriageAge) return false;
            if (!string.IsNullOrEmpty(a.SpouseID) || !string.IsNullOrEmpty(b.SpouseID)) return false;
            return !KinshipRules.AreCloseKin(a, b, KinshipRules.MarriageForbiddenGenerations, clan.FindById);
        }

        public bool HandleLoveMarriage(CharacterData member, CharacterData spouse)
        {
            if (!CanMarry(member, spouse)) return false;

            Wed(member, spouse);
            stability.ApplyModifier(member, LoveStability);
            stability.ApplyModifier(spouse, LoveStability);
            ctx.Log.Info($"[Marriage] {member.FullName} and {spouse.FullName} marry for love.");
            return true;
        }

        /// <summary>
        /// A political marriage: the faction sends a cultivator it trained (Qi Refinement, examined orifice).
        /// Relations warm; a forced member suffers, a willing one is content.
        /// </summary>
        public bool HandleArrangedMarriage(CharacterData member, string factionId, bool isForced)
        {
            if (member == null || !member.IsAlive || member.Age < MarriageMatchmaker.MinMarriageAge
                || !string.IsNullOrEmpty(member.SpouseID))
                return false;

            var faction = factions.GetFactionByID(factionId);
            if (faction == null) return false;

            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(member, faction.Name.Split(' ')[0], ctx.Rng);
            spouse.Realm = CultivationRealm.QiRefinement;
            spouse.RealmStage = 1;
            spouse.HasSpiritualOrifice = true; // a faction only trains those it has examined
            spouse.OrificeKnown = true;
            spouse.MaxLifespan = PowerLadder.MaxLifespan(spouse.Realm, spouse.RealmStage);

            Wed(member, spouse);
            factions.ChangeRelation(factionId, ArrangedRelationBoost);
            stability.ApplyModifier(member, isForced ? ForcedStability : WillingStability);
            ctx.Log.Info($"[Marriage] {member.FullName} marries into {faction.Name}{(isForced ? " against their will" : "")}.");
            return true;
        }

        private void Wed(CharacterData member, CharacterData spouse)
        {
            member.SpouseID = spouse.ID;
            spouse.SpouseID = member.ID;
            if (clan.FindById(spouse.ID) == null)
                clan.AddMember(spouse); // the outsider joins the clan
        }
    }
}
