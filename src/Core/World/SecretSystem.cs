using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The mirror's secret and those who carry it (L2c.4b; LORE.md §11.5: the mirror must stay hidden, D7). Each year a
    /// member in the secret may talk — more when their mind is unsteady, far less when sworn to secrecy, whose oath then
    /// breaks. A leak reaches the power asking most (the most suspicious), or any: proof against the clan, and clues about
    /// a hidden treasure behind it.
    /// </summary>
    public sealed class SecretSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly OathSystem oaths;

        public SecretSystem(GameContext ctx, ClanManager clan, FactionManager factions, SuspicionLedger suspicion, OathSystem oaths)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.suspicion = suspicion;
            this.oaths = oaths;
        }

        private PlotSettings Settings => ctx.Content.Balance.Plots;

        public void ProcessYear()
        {
            if (factions.Factions.Count == 0) return;
            foreach (var keeper in clan.LivingMembers.Where(m => m.KnowsMirrorSecret).ToList())
            {
                var partner = oaths.SecrecyPartner(keeper);
                if (!ctx.Rng.Chance(PlotRules.LeakChance(keeper, partner != null, ctx.Content))) continue;

                var listener = factions.Factions.OrderByDescending(f => suspicion.OfClan(f.Name)).First();
                if (suspicion.OfClan(listener.Name) == 0) listener = ctx.Rng.Pick(factions.Factions.ToList());
                suspicion.AddMirrorClues(listener.Name, Settings.LeakMirrorClue);
                suspicion.AddEvidence(listener.Name, Settings.LeakEvidence);
                ctx.Log.Warning($"[Secrets] {keeper.FullName} lets something slip before {listener.Name}.");
                if (partner != null) oaths.Transgress(keeper, partner, OathAct.RevealSecret); // the oath breaks
            }
        }
    }
}
