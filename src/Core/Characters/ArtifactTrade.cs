using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The artifacts between the clan and the powers (L4f, user decisions 2026-10-03; 📚 an armour commissioned of a sect, a
    /// map lent by a family, an axe traded for a pill). A sect, a gate or a kingdom forges on commission what its realm
    /// allows, dearer than the clan's own forge; the clan sells an artifact for half its worth; a friendly power lends one
    /// for some years, and the clan lends its own to warm a power — one fallen out with it keeps it; the clan steals from a
    /// power, at the risk of being caught. The powers' own thefts from the armoury go through the intrigues.
    /// </summary>
    public sealed class ArtifactTrade
    {
        public const string ClanLender = "clan";

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly ArtifactArmoury armoury;

        public ArtifactTrade(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            ArtifactArmoury armoury)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.armoury = armoury;
        }

        private ArtifactSettings Artifacts => ctx.Content.Balance.Artifacts;

        /// <summary>The accords, to commission a precious artifact in kind (set by the session).</summary>
        public Diplomacy.KnowledgeAccords Accords { get; set; }
        private ArtifactTradeSettings Settings => Artifacts.Trade;

        private static CultivationRealm Craft(FactionData power) =>
            power.HighestRealm > CultivationRealm.PurpleMansion ? CultivationRealm.PurpleMansion : power.HighestRealm;

        /// <summary>What an artifact is worth: its forging, its class.</summary>
        public int Worth(CultivationRealm rank, ArtifactClass cls) =>
            ArtifactRules.Worth(new ArtifactInstance(null, null, null, cls, rank, null, ArtifactEffect.Combat, 0, 0, 0, 1), Artifacts, Settings.OreValue);

        /// <summary>A Purple Mansion's artifact is precious: never bought nor sold for stones (the user's rule, 2026-10-03).</summary>
        public static bool IsPrecious(CultivationRealm rank, ArtifactClass cls) => rank >= CultivationRealm.PurpleMansion || cls == ArtifactClass.SpiritualTreasure;

        public int CommissionPrice(CultivationRealm rank) => (int)(Worth(rank, ArmouryClass(rank)) * Settings.CommissionMarkup);

        private static ArtifactClass ArmouryClass(CultivationRealm rank) => ArtifactArmoury.ClassOf(rank);

        private string AnyLineage()
        {
            var lineages = ctx.Content.Fruitions.Where(f => f.Abilities.Count > 0).ToList();
            return lineages.Count == 0 ? null : lineages[ctx.Rng.Next(lineages.Count)].Id;
        }

        /// <summary>
        /// A sect, a gate or a kingdom forges an artifact for the clan: for stones, or — a Purple Mansion's, precious — for what
        /// it wants in kind. Null when done, else why not (French).
        /// </summary>
        public string Commission(string powerName, string formId, CultivationRealm rank, IReadOnlyList<AccordTerm> terms = null)
        {
            var power = factions.GetFactionByName(powerName);
            if (power == null || (power.Kind != FactionKind.Sect && power.Kind != FactionKind.Gate && power.Kind != FactionKind.State))
                return "seule une secte, une porte ou un royaume forge sur commande";
            if (power.RelationWithPlayer < Settings.CommissionRelation) return $"{power.Name} ne travaille pas pour le clan";
            if (rank > Craft(power) || !Artifacts.Forging.ContainsKey(rank)) return $"{power.Name} ne forge pas d'artefact de ce rang";
            if (ctx.Content.ArtifactForms.All(f => f.Id != formId)) return "forme d'artefact inconnue";
            int price = CommissionPrice(rank);
            if (IsPrecious(rank, ArtifactArmoury.ClassOf(rank)))
            {
                if (Accords == null) return "aucun accord possible";
                if (Accords.BarterRefusal(power.Name, price, precious: true, terms) is { } why) return why;
                Accords.Barter(power.Name, terms, "un artefact du Manoir Pourpre");
            }
            else
            {
                if (resources.SpiritStones < price) return $"la commande coûte {price} pierres";
                resources.ConsumeSpiritStones(price);
                power.Wealth += price;
            }
            var made = armoury.Create(formId, rank, AnyLineage());
            ctx.Events.TriggerArtifactFound(made, $"commandé à {power.Name}");
            return null;
        }

        /// <summary>The clan sells an artifact of its armoury to a power. Null when done, else why not (French).</summary>
        public string Sell(string artifactId, string powerName)
        {
            var power = factions.GetFactionByName(powerName);
            var artifact = armoury.Armoury.FirstOrDefault(a => a.Id == artifactId);
            if (power == null) return "puissance inconnue";
            if (artifact == null || artifact.LentBy != null) return "le clan ne vend que ses propres artefacts de l'armurerie";
            if (IsPrecious(artifact.Rank, artifact.Class)) return "un artefact du Manoir Pourpre ne se vend pas pour des pierres : il s'offre dans un accord";
            int price = (int)(Worth(artifact.Rank, artifact.Class) * Settings.SellShare);
            armoury.TakeAway(artifactId);
            power.Artifacts.Add(artifact);
            resources.AddSpiritStones(price);
            power.Wealth -= price;
            return null;
        }

        /// <summary>A friendly power lends the clan an artifact of its craft for some years. Null when done, else why not (French).</summary>
        public string Borrow(string powerName)
        {
            var power = factions.GetFactionByName(powerName);
            if (power == null || power.RelationWithPlayer < Settings.LoanRelation) return "seule une puissance amie prête un artefact";
            if (power.HighestRealm < CultivationRealm.QiRefinement) return $"{powerName} n'a rien à prêter";
            if (armoury.All.Any(a => a.LentBy == power.Name)) return $"{power.Name} prête déjà un artefact au clan";
            var lent = armoury.Shape(ctx.Content.ArtifactForms[ctx.Rng.Next(ctx.Content.ArtifactForms.Count)].Id, Craft(power), AnyLineage())
                with { LentBy = power.Name, DueYear = ctx.Clock.Year + Settings.LoanYears };
            armoury.Add(lent);
            factions.ChangeRelation(power.ID, -Settings.BorrowRelationCost); // a favour owed
            ctx.Events.TriggerArtifactFound(lent, $"prêté par {power.Name}");
            return null;
        }

        /// <summary>The clan lends one of its artifacts to a power, which warms it. Null when done, else why not (French).</summary>
        public string Lend(string artifactId, string powerName)
        {
            var power = factions.GetFactionByName(powerName);
            var artifact = armoury.Armoury.FirstOrDefault(a => a.Id == artifactId);
            if (power == null) return "puissance inconnue";
            if (artifact == null || artifact.LentBy != null) return "le clan ne prête que ses propres artefacts de l'armurerie";
            armoury.TakeAway(artifactId);
            power.Artifacts.Add(artifact with { LentBy = ClanLender, DueYear = ctx.Clock.Year + Settings.LoanYears });
            factions.ChangeRelation(power.ID, Settings.LendRelationGain);
            return null;
        }

        /// <summary>The clan steals an artifact from a power: its own, or one of its arsenal. Null when done, else why not (French).</summary>
        public string StealFrom(string powerName, IReadOnlyList<string> teamIds)
        {
            var power = factions.GetFactionByName(powerName);
            if (power == null) return "puissance inconnue";
            var team = (teamIds ?? new List<string>()).Distinct().Select(clan.FindById).ToList();
            var ops = ctx.Content.Balance.Shards;
            if (team.Count == 0 || team.Count > ops.ExpeditionMaxTeam || team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null
                || m.Realm < CultivationRealm.QiRefinement || m.LastOperationYear == ctx.Clock.Year))
                return $"un vol se fait à 1 à {ops.ExpeditionMaxTeam} cultivateurs libres";
            foreach (var m in team) m.LastOperationYear = ctx.Clock.Year;
            var powers = team.Select(m => (double)HuntRules.Power(m)).OrderByDescending(p => p).ToList();
            double strength = powers[0] + powers.Skip(1).Sum() * 0.3;
            double chance = System.Math.Clamp(ops.TheftBaseChance + (strength - HuntRules.Power(power.HighestRealm, 5)) * ops.TheftChancePerPower,
                ops.TheftMinChance, ops.TheftMaxChance) * World.PowerFormation.Guard(power, ctx.Content); // its formation (audit §2.3)
            if (ctx.Rng.Chance(chance))
            {
                var held = power.Artifacts.Where(a => a.LentBy != ClanLender).OrderByDescending(a => a.Rank).FirstOrDefault();
                ArtifactInstance taken;
                if (held != null) { power.Artifacts.Remove(held); taken = held with { LentBy = null, DueYear = 0 }; }
                else taken = armoury.Shape(ctx.Content.ArtifactForms[ctx.Rng.Next(ctx.Content.ArtifactForms.Count)].Id, Craft(power), AnyLineage());
                armoury.Add(taken);
                ctx.Events.TriggerArtifactFound(taken, $"volé à {power.Name}");
                return null;
            }
            suspicion.AddEvidence(power.Name, Settings.CaughtEvidence);
            factions.ChangeRelation(power.ID, Settings.CaughtRelation);
            var weakest = team.OrderBy(m => HuntRules.Power(m)).First();
            if (ctx.Rng.Chance(Settings.CaughtDeathChance)) clan.Kill(weakest, DeathCause.Combat);
            ctx.Log.Warning($"[Artifacts] The clan's theft from {power.Name} fails, and is seen.");
            return "le vol échoue, et le clan est vu";
        }

        /// <summary>Loans end: the powers' go back to them, the clan's come home — unless the borrower fell out with the clan.</summary>
        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            foreach (var lent in armoury.All.Where(a => a.LentBy != null && a.LentBy != ClanLender && a.DueYear < year).ToList())
            {
                armoury.TakeAway(lent.Id);
                factions.GetFactionByName(lent.LentBy)?.Artifacts.Add(lent with { LentBy = null, DueYear = 0 });
            }
            foreach (var power in factions.Factions)
                foreach (var loan in power.Artifacts.Where(a => a.LentBy == ClanLender && a.DueYear < year).ToList())
                {
                    power.Artifacts.Remove(loan);
                    if (power.RelationWithPlayer < Settings.KeepRelation)
                    {
                        power.Artifacts.Add(loan with { LentBy = null, DueYear = 0 }); // fallen out with the clan: it keeps it
                        ctx.Log.Warning($"[Artifacts] {power.Name} keeps the clan's {loan.Name}.");
                        continue;
                    }
                    armoury.Add(loan with { LentBy = null, DueYear = 0 });
                }
        }
    }
}
