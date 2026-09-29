using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers' answer to what they suspect (L2c.4a; LORE.md D7 « everything is a plot »), each year: a suspicious
    /// power investigates and may find proof; past the threshold it strikes the clan (relation, stones). With proof, it
    /// is its right, and the others think worse of the clan; without proof, it strikes only when clearly stronger — and
    /// every other power, allies included, silently distrusts it. Either way the account is settled, and a proof is spent
    /// by its blow; a rumour of it makes the others wary, never ready to strike on hearsay. Suspicion and proof fade.
    /// </summary>
    public sealed class PlotSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly SecretSystem secrets;
        private readonly TreatySystem treaties;

        public PlotSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            SecretSystem secrets, TreatySystem treaties = null)
        {
            this.treaties = treaties;
            this.secrets = secrets;
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            ctx.Events.OnYearStarted += _ => Fade();
        }

        /// <summary>Memory fades: each year a power's suspicion of the clan and its proof lessen a little.</summary>
        private void Fade()
        {
            foreach (var power in factions.Factions)
            {
                suspicion.AddToClan(power.Name, -Settings.SuspicionFadePerYear);
                suspicion.AddEvidence(power.Name, -Settings.EvidenceFadePerYear);
            }
        }

        private PlotSettings Settings => ctx.Content.Balance.Plots;

        private readonly HashSet<string> struck = new HashSet<string>();

        /// <summary>The powers that struck the clan this year: one blow a year (their blackmail waits).</summary>
        public IReadOnlyCollection<string> StruckThisYear => struck;

        public void ProcessYear()
        {
            struck.Clear();
            foreach (var power in factions.Factions.ToList())
            {
                int suspected = suspicion.OfClan(power.Name);
                if (suspected >= Settings.InvestigateThreshold
                    && ctx.Rng.Chance(PlotRules.InvestigationChance(power, suspected, ctx.Content)))
                {
                    suspicion.AddEvidence(power.Name, Settings.EvidencePerFinding);
                    ctx.Log.Info($"[Plots] {power.Name} finds something against the clan.");
                }
                bool confronting = secrets.Confrontation?.Faction == power.Name; // its move is the confrontation's, not an ordinary blow
                if (suspected >= Settings.ActThreshold && !confronting) Decide(power);
            }
        }

        /// <summary>A rumour makes a power wary — to investigate, never to strike on hearsay alone.</summary>
        private int Rumour(string power) =>
            Math.Max(0, Math.Min(Settings.ProofReputation, Settings.ActThreshold - 1 - suspicion.OfClan(power)));

        private void Decide(FactionData power)
        {
            bool proven = suspicion.Evidence(power.Name) >= Settings.ProofThreshold;
            var strongest = clan.LivingMembers.Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (!proven && !PlotRules.DaresWithoutProof(power, strongest, ctx.Content)) return; // it waits for proof
            if (!proven && treaties?.Spares(power.Name) == true) return;                     // its treaty holds it back, short of proof

            factions.ChangeRelation(power.ID, Settings.ReprisalRelation);
            resources.ConsumeSpiritStones((int)(resources.SpiritStones * Settings.ReprisalStonesShare));
            suspicion.AddToClan(power.Name, -suspicion.OfClan(power.Name)); // the account is settled
            if (proven) suspicion.AddEvidence(power.Name, -suspicion.Evidence(power.Name)); // the proof is spent by the blow
            struck.Add(power.Name);
            ctx.Events.TriggerClanStruck(power.Name);

            foreach (var other in factions.Factions.Where(f => f != power))
            {
                if (proven) suspicion.AddToClan(other.Name, Rumour(other.Name));             // the clan's name suffers
                else suspicion.AddDistrust(other.Name, power.Name, Settings.WitnessDistrust); // they saw it strike without proof
            }
            ctx.Log.Warning(proven
                ? $"[Plots] {power.Name} strikes the clan, proof in hand."
                : $"[Plots] {power.Name} strikes the clan without proof; the others watch in silence.");
        }
    }
}
