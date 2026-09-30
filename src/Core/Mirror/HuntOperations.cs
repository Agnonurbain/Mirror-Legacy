using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The hunt as a planned operation (L2c.3; LORE.md D7 « everything is a plot »). A plan is checked, paid (cover
    /// story in stones, the mirror's help in power), its team and diversion kept busy for the year, then carried out
    /// in three rolls: the approach (seen: the team flees with nothing), the capture (failed: the beast escapes and the
    /// strikers are hurt, killed when it was far stronger), the retreat. The traces feed the owner's hidden suspicion;
    /// a false trail that takes turns it into the owner's distrust of the framed power, one seen through makes it worse.
    /// </summary>
    public sealed class HuntOperations
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MirrorSystem mirror;
        private readonly FactionManager factions;
        private readonly BeastRegistry bestiary;
        private readonly KnowledgeBase knowledge;
        private readonly TalismanSystem talismans;
        private readonly SuspicionLedger suspicion;
        private readonly MentalStabilitySystem stability;

        public HuntOperations(GameContext ctx, ClanManager clan, ResourceManager resources, MirrorSystem mirror, FactionManager factions,
            BeastRegistry bestiary, KnowledgeBase knowledge, TalismanSystem talismans, SuspicionLedger suspicion, MentalStabilitySystem stability)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.mirror = mirror;
            this.factions = factions;
            this.bestiary = bestiary;
            this.knowledge = knowledge;
            this.talismans = talismans;
            this.suspicion = suspicion;
            this.stability = stability;
            ctx.Events.OnYearStarted += year => ReturnFromOperations();
        }

        /// <summary>Fit and not already sent on a hunt or a diversion this year (one operation a year).</summary>
        public bool IsFree(CharacterData member) => Fit(member) && member.LastOperationYear != ctx.Clock.Year;

        /// <summary>A new year: those away on an operation are back, free for their tasks.</summary>
        private void ReturnFromOperations()
        {
            foreach (var m in clan.LivingMembers.Where(m => m.CurrentTask == TaskType.HuntBeast || m.CurrentTask == TaskType.Diversion))
                m.CurrentTask = TaskType.None;
        }

        private HuntSettings Settings => ctx.Content.Balance.Hunt;

        /// <summary>Why the plan cannot be carried out, told to the player in French, or null when it can.</summary>
        public string Validate(HuntPlan plan)
        {
            if (plan == null) return "il n'y a pas de plan";
            if (!talismans.HuntWindowOpen) return "la fenêtre de chasse est fermée";
            var beast = Target(plan);
            if (beast == null) return "le clan n'a pas repéré cette bête";
            if (plan.Team == null || !plan.Team.ContainsValue(HuntRole.Striker)) return "l'équipe a besoin d'un frappeur";
            var unfit = plan.Team.Keys.FirstOrDefault(id => !IsFree(clan.FindById(id)));
            if (unfit != null) return $"{clan.FindById(unfit)?.FullName ?? unfit} ne peut pas être de la chasse";

            if (plan.DiversionMemberId != null)
            {
                if (plan.Team.ContainsKey(plan.DiversionMemberId)) return "la diversion ne peut pas être de l'équipe";
                if (!IsFree(clan.FindById(plan.DiversionMemberId))) return "le membre de la diversion ne peut pas partir";
                var place = ctx.Content.Regions.FirstOrDefault(r => r.Id == plan.DiversionRegionId);
                if (place?.ParentId == null || place.Id == beast.RegionId) return "la diversion doit se montrer ailleurs, sur un lieu de la carte";
            }
            if (plan.FramedFaction != null && (factions.GetFactionByName(plan.FramedFaction) == null || plan.FramedFaction == beast.OwnerFaction))
                return "la fausse piste doit viser une autre puissance";
            int aidCost = Settings.AidMirrorCost[(int)plan.Aid];
            if (aidCost > 0 && mirror.IsAsleep) return "le miroir dort : il intègre un éclat";
            if (mirror.MirrorPower < aidCost) return "le miroir manque de puissance pour aider";
            if (resources.SpiritStones < Settings.CoverStones[(int)plan.Cover]) return "la couverture coûte plus de pierres que le clan n'en a";
            return null;
        }

        /// <summary>Carries the plan out (see the class summary); an invalid plan does nothing and says why.</summary>
        public HuntOutcome Execute(HuntPlan plan)
        {
            string refusal = Validate(plan);
            if (refusal != null)
            {
                ctx.Log.Warning($"[Hunt] The plan is not carried out: {refusal}.");
                return new HuntOutcome(false, false, 0, null, refusal, new List<string>());
            }

            var beast = Target(plan);
            resources.ConsumeSpiritStones(Settings.CoverStones[(int)plan.Cover]);
            mirror.ConsumePower(Settings.AidMirrorCost[(int)plan.Aid]);
            foreach (var id in plan.Team.Keys)
            {
                var member = clan.FindById(id);
                member.CurrentTask = TaskType.HuntBeast; // away for the year
                member.LastOperationYear = ctx.Clock.Year;
                member.KnowsMirrorSecret = true;         // and in the secret (L2c.4b)
            }
            if (plan.DiversionMemberId != null)
            {
                var decoy = clan.FindById(plan.DiversionMemberId);
                decoy.CurrentTask = TaskType.Diversion; // seen elsewhere
                decoy.LastOperationYear = ctx.Clock.Year;
                decoy.KnowsMirrorSecret = true;
            }

            int approach = HuntRules.ApproachChance(plan, beast, factions, ctx.Content);
            if (ctx.Rng.Next(1, 101) > approach)
            {
                Suspect(beast, Settings.SeenExposure, framed: null); // seen: no false trail holds
                ctx.Log.Info($"[Hunt] The team is seen before it gets close ({approach}%) and flees.");
                ctx.Events.TriggerHunt(beast.Id, false);
                return new HuntOutcome(false, false, Settings.SeenExposure, null, null, new List<string>());
            }

            int capture = HuntRules.CaptureChance(plan, beast, clan, ctx.Content);
            if (ctx.Rng.Next(1, 101) > capture)
            {
                var fallen = Hurt(plan, beast);
                string blamed = Suspect(beast, Settings.FailedCaptureExposure, plan.FramedFaction);
                ctx.Log.Info($"[Hunt] The beast breaks free ({capture}%).");
                ctx.Events.TriggerHunt(beast.Id, false);
                return new HuntOutcome(true, false, Settings.FailedCaptureExposure, blamed, null, fallen);
            }

            bestiary.Take(beast);
            resources.AddBeast(new CapturedBeast(beast.Id, beast.Realm, beast.Stage, beast.OwnerFaction));
            if (beast.OwnerFaction != null) ctx.Events.TriggerDeed("blood-of-anothers-beast", beast.OwnerFaction); // a secret of the clan
            int exposure = HuntRules.CleanExposure(plan, ctx.Content);
            string cleanBlame = Suspect(beast, exposure, plan.FramedFaction);
            ctx.Log.Info($"[Hunt] The team takes the beast ({beast.Realm}, stage {beast.Stage}).");
            ctx.Events.TriggerHunt(beast.Id, true);
            return new HuntOutcome(true, true, exposure, cleanBlame, null, new List<string>());
        }

        private WorldBeast Target(HuntPlan plan) =>
            bestiary.Beasts.FirstOrDefault(b => b.Id == plan.TargetBeastId && knowledge.Knows(FactKind.Beast, b.Id));

        private static bool Fit(CharacterData m) =>
            m != null && m.IsAlive && m.Realm >= CultivationRealm.QiRefinement && m.Retreat == Retreat.None && m.CaptorFaction == null;

        /// <summary>A failed capture shakes the strikers; the far stronger beast may kill them.</summary>
        private List<string> Hurt(HuntPlan plan, WorldBeast beast)
        {
            var fallen = new List<string>();
            int beastPower = HuntRules.Power(beast.Realm, beast.Stage);
            foreach (var striker in plan.Team.Where(p => p.Value == HuntRole.Striker).Select(p => clan.FindById(p.Key)).ToList())
            {
                stability.ApplyModifier(striker, -Settings.FailedCaptureStabilityLoss);
                if (beastPower - HuntRules.Power(striker.Realm, striker.RealmStage) > Settings.DeathMargin && ctx.Rng.Chance(Settings.DeathChance))
                {
                    clan.Kill(striker, DeathCause.Combat);
                    fallen.Add(striker.ID);
                }
            }
            return fallen;
        }

        /// <summary>
        /// The owner's hidden reaction to the traces: a false trail may turn it on another power (plausible when that
        /// power lives by the beast or is aggressive); seen through, it makes the clan's case worse. Returns who is blamed.
        /// </summary>
        private string Suspect(WorldBeast beast, int exposure, string framed)
        {
            string owner = beast.OwnerFaction;
            if (owner == null || exposure <= 0) return null; // a solitary beast: no power watches it (the mirror's secret: L2c.4)
            if (framed == null)
            {
                suspicion.AddToClan(owner, exposure);
                return null;
            }

            var target = factions.GetFactionByName(framed);
            var beastPlace = ctx.Content.Regions.FirstOrDefault(r => r.Id == beast.RegionId);
            bool near = target.RegionId == beastPlace?.Id || beastPlace?.Neighbours.Contains(target.RegionId) == true;
            double chance = Settings.FrameBaseChance + (near ? Settings.FrameNeighbourBonus : 0)
                + (target.Personality == FactionPersonality.Aggressive ? Settings.FrameAggressiveBonus : 0);
            if (ctx.Rng.Chance(chance))
            {
                suspicion.AddDistrust(owner, framed, exposure);
                return framed;
            }
            suspicion.AddToClan(owner, exposure + Settings.FailedFrameBacklash);
            return null;
        }
    }
}
