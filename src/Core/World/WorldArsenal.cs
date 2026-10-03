using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers' treasures and artifacts (the user's rule, 2026-10-03: what befalls the clan befalls the world). Each year
    /// their True Monarchs may condense a Dharma Treasure (lost with them); a holder may mortgage it into a Rank Designation,
    /// which guards its power after its master — fully while an elder holds its lineage, by half otherwise — and, without
    /// one, strikes the power's elders until it is unsealed; a sect, a gate or a kingdom forges artifacts of its craft, a
    /// family less often, as many as its elders. All of it weighs in the power's war strength.
    /// </summary>
    public sealed class WorldArsenal
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly ArtifactArmoury forge;

        public WorldArsenal(GameContext ctx, FactionManager factions, ArtifactArmoury forge)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.forge = forge;
        }

        private WorldArsenalSettings Settings => ctx.Content.Balance.WorldArsenal;

        public void ProcessYear()
        {
            foreach (var power in factions.Factions.ToList())
            {
                Treasures(power);
                Designations(power);
                Forge(power);
                power.DomainStrength = Strength(power);
            }
        }

        private void Treasures(FactionData power)
        {
            foreach (var elder in power.Elders.Where(e => e.Realm >= CultivationRealm.GoldenCore))
            {
                if (!elder.HasDharmaTreasure && ctx.Rng.Chance(Settings.ElderCondenseChance)) elder.HasDharmaTreasure = true;
                else if (elder.HasDharmaTreasure && elder.FruitionId != null && ctx.Rng.Chance(Settings.ElderDesignationChance))
                {
                    elder.HasDharmaTreasure = false;
                    power.Designations.Add(new RankDesignation(ctx.Rng.NextId(), elder.FruitionId, elder.Id, elder.Name, ctx.Clock.Year));
                    ctx.Log.Info($"[Arsenal] {elder.Name} of {power.Name} mortgages its treasure on {elder.FruitionId}.");
                }
            }
        }

        private static bool Mastered(FactionData power, RankDesignation d) => power.Elders.Any(e => e.FruitionId == d.Lineage);

        private void Designations(FactionData power)
        {
            foreach (var d in power.Designations.Where(d => !Mastered(power, d)).ToList())
            {
                if (ctx.Rng.Chance(Settings.UnsealChance))
                {
                    power.Designations.Remove(d); // it returns to its Fruition
                    continue;
                }
                var prey = power.Elders.Where(e => e.Realm < CultivationRealm.GoldenCore).ToList();
                if (prey.Count == 0 || !ctx.Rng.Chance(Settings.MasterlessStrikeChance)) continue;
                var struck = prey[ctx.Rng.Next(prey.Count)];
                power.Elders.Remove(struck);
                ElderSystem.Sync(power);
                ctx.Log.Warning($"[Arsenal] {d.MasterName}'s masterless Designation strikes {struck.Name} of {power.Name}.");
                ctx.Events.TriggerElderDied(power, struck, false);
            }
        }

        private void Forge(FactionData power)
        {
            if (power.HighestRealm < CultivationRealm.QiRefinement || power.Artifacts.Count >= power.Elders.Count) return;
            bool great = power.Kind == FactionKind.Sect || power.Kind == FactionKind.Gate || power.Kind == FactionKind.State;
            if (!ctx.Rng.Chance(great ? Settings.ForgeChance : Settings.FamilyForgeChance)) return;
            var rank = power.HighestRealm > CultivationRealm.PurpleMansion ? CultivationRealm.PurpleMansion : power.HighestRealm;
            var forms = ctx.Content.ArtifactForms;
            power.Artifacts.Add(forge.Shape(forms[ctx.Rng.Next(forms.Count)].Id, rank, null));
        }

        /// <summary>
        /// What its treasures, its Designations and its artifacts add to a power's war strength: the greatest whole, the
        /// others as the clan's other fighters weigh (wars.clanStrengthPerMember) — the same measure for the clan and the world.
        /// </summary>
        public double Strength(FactionData power)
        {
            var dharma = ctx.Content.Balance.Dharma;
            var gifts = power.Elders.Where(e => e.HasDharmaTreasure).Select(_ => dharma.TreasureStrength)
                .Concat(power.Designations.Select(d => (Mastered(power, d) ? 1.0 : 0.5) * dharma.DesignationStrength))
                .Concat(power.Artifacts.Where(a => a.LentBy != ArtifactTrade.ClanLender).Select(a => (double)a.Strength));
            return Weigh(gifts, ctx.Content.Balance.Wars.ClanStrengthPerMember);
        }

        /// <summary>The greatest whole, the others by this share.</summary>
        public static double Weigh(System.Collections.Generic.IEnumerable<double> gifts, double share)
        {
            var sorted = gifts.OrderByDescending(g => g).ToList();
            return sorted.Count == 0 ? 0 : sorted[0] + sorted.Skip(1).Sum() * share;
        }
    }
}
