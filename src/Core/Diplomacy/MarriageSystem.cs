using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
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
        private readonly ResourceManager resources;

        public MarriageSystem(GameContext ctx, ClanManager clan, FactionManager factions, MentalStabilitySystem stability, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.stability = stability;
            this.resources = resources;
        }

        private LineageSettings Lineage => ctx.Content.Balance.Lineage;

        /// <summary>Why these two members may not be wed, or null when they may.</summary>
        public string MarriageRefusal(CharacterData a, CharacterData b)
        {
            if (a == null || b == null || !a.IsAlive || !b.IsAlive || a == b) return "introuvable";
            if (a.IsMale == b.IsMale) return "il faut un homme et une femme";
            if (a.CaptorFaction != null || b.CaptorFaction != null) return "captif ailleurs";
            if (a.Age < MarriageMatchmaker.MinMarriageAge || b.Age < MarriageMatchmaker.MinMarriageAge) return "trop jeune";
            if (!string.IsNullOrEmpty(a.SpouseID) || !string.IsNullOrEmpty(b.SpouseID)) return "déjà marié";
            return KinshipRules.AreCloseKin(a, b, KinshipRules.MarriageForbiddenGenerations, clan.FindById) ? "trop proche parenté" : null;
        }

        /// <summary>The clan arranges a marriage between two of its members (2026-09-29): to keep the cultivating line.</summary>
        public string Arrange(string aId, string bId)
        {
            var a = clan.FindById(aId);
            var b = clan.FindById(bId);
            var refusal = MarriageRefusal(a, b);
            if (refusal != null) return refusal;
            Wed(a, b);
            stability.ApplyModifier(a, WillingStability);
            stability.ApplyModifier(b, WillingStability);
            ctx.Log.Info($"[Marriage] The clan weds {a.FullName} and {b.FullName}.");
            return null;
        }

        /// <summary>Why the clan cannot seek a cultivator spouse abroad for this member now, or null when it can.</summary>
        public string SeekRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive) return "introuvable";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Age < MarriageMatchmaker.MinMarriageAge) return "trop jeune";
            if (!string.IsNullOrEmpty(member.SpouseID)) return "déjà marié";
            if (resources.SpiritStones < Lineage.SeekStones) return $"il faut {Lineage.SeekStones} pierres spirituelles";
            return null;
        }

        /// <summary>
        /// The clan seeks abroad a cultivator willing to wed this member (2026-09-29): it pays whatever the outcome; a
        /// wandering cultivator of Qi Refinement, examined, bound to no power, may be found.
        /// </summary>
        public string SeekCultivatorSpouse(string memberId)
        {
            var member = clan.FindById(memberId);
            var refusal = SeekRefusal(member);
            if (refusal != null) return refusal;
            resources.ConsumeSpiritStones(Lineage.SeekStones);
            if (!ctx.Rng.Chance(LineageRules.SeekChance(member, Lineage))) return "on n'a trouvé personne de convenable";

            var names = ctx.Content.Names;
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(member, MarriageMatchmaker.PickFamilyName(names, ctx.Rng), names,
                ctx.Content.Balance.OrificeOdds, ctx.Rng);
            spouse.Realm = CultivationRealm.QiRefinement;
            spouse.RealmStage = 1;
            spouse.HasSpiritualOrifice = true; // the one sought was examined
            spouse.OrificeKnown = true;
            spouse.MaxLifespan = PowerLadder.MaxLifespan(spouse.Realm, spouse.RealmStage);
            Wed(member, spouse);
            stability.ApplyModifier(member, WillingStability);
            ctx.Log.Info($"[Marriage] A wandering cultivator weds {member.FullName}.");
            return null;
        }

        /// <summary>
        /// Called during the Events phase (before Inheritance, so this year's newlyweds can have children).
        /// Returns how many couples were formed.
        /// </summary>
        public int ProcessAnnualMarriages()
        {
            var content = ctx.Content;
            var plans = MarriageMatchmaker.PlanAnnualMarriages(clan.LivingMembers.ToList(), clan.FindById,
                content.Balance.AnnualMarriageChance, content.Names, content.Balance.OrificeOdds, ctx.Rng);

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
            if (a.CaptorFaction != null || b.CaptorFaction != null) return false; // a captive marries nobody (L6a)
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
            if (member == null || !member.IsAlive || member.CaptorFaction != null || member.Age < MarriageMatchmaker.MinMarriageAge
                || !string.IsNullOrEmpty(member.SpouseID))
                return false;

            var faction = factions.GetFactionByID(factionId);
            if (faction == null) return false;

            var names = ctx.Content.Names;
            string family = faction.FamilyName ?? MarriageMatchmaker.PickFamilyName(names, ctx.Rng); // a sect sends one of its disciples
            var spouse = MarriageMatchmaker.CreateOutsiderSpouse(member, family, names, ctx.Content.Balance.OrificeOdds, ctx.Rng);
            spouse.Realm = CultivationRealm.QiRefinement;
            spouse.RealmStage = 1;
            spouse.HasSpiritualOrifice = true; // a faction only trains those it has examined
            spouse.OrificeKnown = true;
            spouse.MaxLifespan = PowerLadder.MaxLifespan(spouse.Realm, spouse.RealmStage);
            spouse.FromFaction = faction.Name;
            if (ctx.Rng.Chance(World.IntrigueRules.SpyChance(faction, ctx.Content.Balance.Intrigues)))
                spouse.SpyFor = faction.Name; // its eyes inside the clan (hidden, 2026-09-27)

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
