using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The captives on both sides (L6a; LORE.md D7). A member held by a power can do nothing and is interrogated each
    /// year: one who knows the mirror's secret may talk, an oath of secrecy holds the tongue better (and breaks when it
    /// does not); past its patience the captor may execute them. The clan pays the ransom, rescues them, exchanges an
    /// agent for them, or has the mirror blur what they know. An agent the clan holds is sold back, released,
    /// interrogated once (fragments of its arts, and what its power holds against the clan), denounced once to the other
    /// powers — proof of the scheme, and they distrust its power in silence — or executed. Each action answers with its
    /// refusal, or null when done.
    /// </summary>
    public sealed class CaptiveSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly OathSystem oaths;
        private readonly MirrorSystem mirror;
        private readonly HuntOperations hunts;
        private readonly List<Prisoner> prisoners = new List<Prisoner>();

        public CaptiveSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            OathSystem oaths, MirrorSystem mirror, HuntOperations hunts)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.oaths = oaths;
            this.mirror = mirror;
            this.hunts = hunts;
        }

        private SchemeSettings Settings => ctx.Content.Balance.Schemes;

        /// <summary>The clan's stones as a greedy power sees them: what a ransom could fetch.</summary>
        public int ClanStones => resources.SpiritStones;

        /// <summary>The members held by a power.</summary>
        public IReadOnlyList<CharacterData> Held => clan.LivingMembers.Where(m => m.CaptorFaction != null).ToList();

        /// <summary>The powers' agents the clan holds.</summary>
        public IReadOnlyList<Prisoner> Prisoners => prisoners;

        public void Take(CharacterData member, string faction)
        {
            member.CaptorFaction = faction;
            member.CapturedYear = ctx.Clock.Year;
            member.CurrentTask = TaskType.None;
            clan.HandOver(member); // a patriarch taken: a free member leads the clan
            ctx.Log.Warning($"[Captives] {faction} takes {member.FullName}.");
            ctx.Events.TriggerMemberCaptured(member, faction);
        }

        public void Imprison(Prisoner prisoner)
        {
            prisoners.Add(prisoner);
            ctx.Log.Info($"[Captives] An agent of {prisoner.Faction} is in the clan's hands.");
            ctx.Events.TriggerAgentCaught(prisoner.Faction);
        }

        public void RestorePrisoners(IEnumerable<Prisoner> saved)
        {
            prisoners.Clear();
            if (saved != null) prisoners.AddRange(saved);
        }

        public void ProcessYear()
        {
            prisoners.RemoveAll(p => factions.GetFactionByName(p.Faction) == null); // its power is gone: nobody to bargain with
            foreach (var captive in Held)
            {
                var captor = factions.GetFactionByName(captive.CaptorFaction);
                if (captor == null)
                {
                    Free(captive); // its captor is gone: it walks free
                    continue;
                }
                Interrogate(captive, captor);
                if (ctx.Clock.Year - (captive.CapturedYear ?? ctx.Clock.Year) >= Settings.PatienceYears
                    && ctx.Rng.Chance(Settings.ExecutionChance))
                {
                    clan.Kill(captive, DeathCause.Executed);
                    ctx.Log.Warning($"[Captives] {captor.Name}, tired of waiting, executes {captive.FullName}.");
                }
            }
        }

        private void Interrogate(CharacterData captive, FactionData captor)
        {
            if (!captive.KnowsMirrorSecret || captive.CapturedYear == ctx.Clock.Year) return; // the clan has a year to answer
            var partner = oaths.SecrecyPartner(captive);
            if (!ctx.Rng.Chance(SchemeRules.InterrogationChance(captive, partner != null, ctx.Content))) return;
            suspicion.AddMirrorClues(captor.Name, ctx.Content.Balance.Plots.LeakMirrorClue);
            suspicion.AddEvidence(captor.Name, ctx.Content.Balance.Plots.LeakEvidence);
            ctx.Log.Warning($"[Captives] Under interrogation, {captive.FullName} lets something slip before {captor.Name}.");
            if (partner != null) oaths.Transgress(captive, partner, OathAct.RevealSecret);
        }

        // ---- The clan's answers for its captives ----

        public string PayRansom(string memberId)
        {
            var captive = HeldMember(memberId);
            if (captive == null) return "ce membre n'est pas captif";
            int ransom = SchemeRules.Ransom(captive.Realm, Settings);
            if (!resources.ConsumeSpiritStones(ransom)) return $"il faut {ransom} pierres spirituelles";
            ChangeRelation(captive.CaptorFaction, Settings.PaidRansomRelation);
            Free(captive);
            return null;
        }

        /// <summary>The mirror blurs what the captive knows of it: nothing left to tell.</summary>
        public string Silence(string memberId)
        {
            var captive = HeldMember(memberId);
            if (captive == null) return "ce membre n'est pas captif";
            if (!captive.KnowsMirrorSecret) return "il ne sait rien du miroir";
            if (!mirror.ConsumePower(Settings.SilenceMirrorCost)) return $"il faut {Settings.SilenceMirrorCost} de puissance du miroir";
            captive.KnowsMirrorSecret = false;
            ctx.Log.Info($"[Captives] The mirror blurs what {captive.FullName} knew.");
            return null;
        }

        /// <summary>A rescue by free members: one operation a year for each.</summary>
        public string Rescue(string memberId, IReadOnlyCollection<string> teamIds)
        {
            var captive = HeldMember(memberId);
            if (captive == null) return "ce membre n'est pas captif";
            if (teamIds == null || teamIds.Count == 0) return "il faut une équipe";
            var team = teamIds.Distinct().Select(clan.FindById).ToList(); // each rescuer counts once
            var unfit = team.FirstOrDefault(m => !hunts.IsFree(m));
            if (unfit != null || team.Contains(null)) return $"{unfit?.FullName ?? "un membre"} ne peut pas partir";
            var captor = factions.GetFactionByName(captive.CaptorFaction);
            if (captor == null) return "puissance inconnue";
            if (TravelRules.Refusal(team, ctx.Content.Clan.HomeRegion, captor.RegionId, ctx.Content.Regions, ctx.Content.Balance.Travel) is { } far)
                return far;
            if (!Characters.RealmGap.Reaches(team, captor.HighestRealm, ctx.Content.Balance.RealmGap))
                return $"personne de l'équipe n'atteint {captor.Name}, dont le plus fort est d'un royaume trop haut ({RankCatalog.RealmName(captor.HighestRealm)})";

            foreach (var rescuer in team) rescuer.LastOperationYear = ctx.Clock.Year;
            if (!ctx.Rng.Chance(SchemeRules.RescueChance(team, captor, Settings, ctx.Content.Balance.RealmGap)))
            {
                suspicion.AddToClan(captor.Name, Settings.FailedRescueSuspicion); // how did they know where to strike?
                return "le sauvetage échoue";
            }
            factions.ChangeRelation(captor.ID, Settings.RescueRelation);
            Free(captive);
            ctx.Log.Info($"[Captives] {captive.FullName} is rescued from {captor.Name}.");
            return null;
        }

        /// <summary>An agent of the captor, traded for the captive.</summary>
        public string Exchange(string memberId, string prisonerId)
        {
            var captive = HeldMember(memberId);
            if (captive == null) return "ce membre n'est pas captif";
            var agent = PrisonerOf(prisonerId);
            if (agent == null) return "prisonnier inconnu";
            if (agent.Faction != captive.CaptorFaction) return "cet agent n'est pas de la puissance qui le détient";
            prisoners.Remove(agent);
            Free(captive);
            return null;
        }

        // ---- The agents the clan holds ----

        public string SellBack(string prisonerId)
        {
            var agent = PrisonerOf(prisonerId);
            if (agent == null) return "prisonnier inconnu";
            var power = factions.GetFactionByName(agent.Faction);
            if (power == null) return "puissance inconnue";
            int ransom = SchemeRules.Ransom(agent.Realm, Settings);
            if (power.Wealth < ransom) return $"{power.Name} ne peut pas payer {ransom} pierres";
            power.Wealth -= ransom;
            resources.AddSpiritStones(ransom);
            factions.ChangeRelation(power.ID, Settings.SellBackRelation);
            prisoners.Remove(agent);
            return null;
        }

        public string Release(string prisonerId)
        {
            var agent = PrisonerOf(prisonerId);
            if (agent == null) return "prisonnier inconnu";
            ChangeRelation(agent.Faction, Settings.ReleaseRelation);
            prisoners.Remove(agent);
            return null;
        }

        /// <summary>What the agent tells, once: fragments of its power's arts, and what its power holds against the clan; null when nothing more.</summary>
        public string Interrogate(string prisonerId)
        {
            var agent = PrisonerOf(prisonerId);
            if (agent == null || agent.Interrogated) return null;
            Replace(agent, agent with { Interrogated = true });
            resources.AddTechniqueFragments(Settings.InterrogationFragments);

            int proof = suspicion.Evidence(agent.Faction);
            string holds = proof >= ctx.Content.Balance.Plots.ProofThreshold ? "détient des preuves contre le clan"
                : proof > 0 ? "rassemble des indices, sans preuve encore"
                : "n'a aucune preuve contre le clan";
            string mirrorLine = suspicion.MirrorClues(agent.Faction) > 0 ? " Elle soupçonne un trésor caché." : "";
            return $"{agent.Faction} {holds}.{mirrorLine}";
        }

        /// <summary>The agent shown to the other powers, once: proof of the scheme; they distrust its power in silence.</summary>
        public string Denounce(string prisonerId)
        {
            var agent = PrisonerOf(prisonerId);
            if (agent == null) return "prisonnier inconnu";
            if (agent.Denounced) return "cet agent a déjà été montré";
            foreach (var other in factions.Factions.Where(f => f.Name != agent.Faction))
                suspicion.AddDistrust(other.Name, agent.Faction, Settings.DenounceDistrust);
            ChangeRelation(agent.Faction, Settings.DenounceRelation);
            Replace(agent, agent with { Denounced = true });
            ctx.Log.Info($"[Captives] The clan shows the others what {agent.Faction} plotted.");
            return null;
        }

        public string Execute(string prisonerId)
        {
            var agent = PrisonerOf(prisonerId);
            if (agent == null) return "prisonnier inconnu";
            foreach (var power in factions.Factions)
                factions.ChangeRelation(power.ID, power.Name == agent.Faction ? Settings.ExecuteRelation : Settings.ExecuteWitnessRelation);
            prisoners.Remove(agent);
            ctx.Log.Warning($"[Captives] The clan executes an agent of {agent.Faction}.");
            ctx.Events.TriggerDeed("executed-agent", agent.Faction);
            return null;
        }

        private CharacterData HeldMember(string id)
        {
            var member = clan.FindById(id);
            return member != null && member.IsAlive && member.CaptorFaction != null ? member : null;
        }

        private Prisoner PrisonerOf(string id) => prisoners.FirstOrDefault(p => p.Id == id);

        private void Replace(Prisoner old, Prisoner updated) => prisoners[prisoners.IndexOf(old)] = updated;

        private void ChangeRelation(string faction, int amount)
        {
            var power = factions.GetFactionByName(faction);
            if (power != null) factions.ChangeRelation(power.ID, amount);
        }

        private void Free(CharacterData member)
        {
            ctx.Log.Info($"[Captives] {member.FullName} is free of {member.CaptorFaction}.");
            member.CaptorFaction = null;
            member.CapturedYear = null;
            ctx.Events.TriggerMemberFreed(member);
        }
    }
}
