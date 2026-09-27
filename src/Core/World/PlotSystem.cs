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
    /// every other power, allies included, silently distrusts it. Either way the account is settled.
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
        }

        private PlotSettings Settings => ctx.Content.Balance.Plots;

        public void ProcessYear()
        {
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

        private void Decide(FactionData power)
        {
            bool proven = suspicion.Evidence(power.Name) >= Settings.ProofThreshold;
            var strongest = clan.LivingMembers.Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (!proven && !PlotRules.DaresWithoutProof(power, strongest, ctx.Content)) return; // it waits for proof
            if (!proven && treaties?.Spares(power.Name) == true) return;                     // its treaty holds it back, short of proof

            factions.ChangeRelation(power.ID, Settings.ReprisalRelation);
            resources.ConsumeSpiritStones((int)(resources.SpiritStones * Settings.ReprisalStonesShare));
            suspicion.AddToClan(power.Name, -suspicion.OfClan(power.Name)); // the account is settled

            foreach (var other in factions.Factions.Where(f => f != power))
            {
                if (proven) suspicion.AddToClan(other.Name, Settings.ProofReputation);        // the clan's name suffers
                else suspicion.AddDistrust(other.Name, power.Name, Settings.WitnessDistrust); // they saw it strike without proof
            }
            ctx.Log.Warning(proven
                ? $"[Plots] {power.Name} strikes the clan, proof in hand."
                : $"[Plots] {power.Name} strikes the clan without proof; the others watch in silence.");
        }
    }
}
