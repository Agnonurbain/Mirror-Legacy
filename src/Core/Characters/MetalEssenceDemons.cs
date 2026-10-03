using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Metal Essence Demons born of the clan (LORE.md §6.9; L4e, user decisions 2026-10-03). Each awaits the clan's
    /// choice: leave it to the Underworld — the custom of the Douxuan Profundity, safe; take its essence back — a Golden
    /// Core force must seal it, and the Underworld, provoked, bears a grudge: it keeps the registers of the living, so no
    /// ancestor of the clan is reborn, and its emissaries claim any new demon of the clan at once; a bribe of essence
    /// soothes it; or let it be — it ravages the region for years by its rank, then the Underworld claims it, unless a
    /// Golden Core of the clan subdues it first. Unanswered for a year, the custom prevails.
    /// </summary>
    public sealed class MetalEssenceDemons
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly Diplomacy.FactionManager factions;
        private readonly List<MetalEssenceDemon> pending = new List<MetalEssenceDemon>();
        private readonly List<MetalEssenceDemon> ravaging = new List<MetalEssenceDemon>();

        public MetalEssenceDemons(GameContext ctx, ClanManager clan, ResourceManager resources, Diplomacy.FactionManager factions)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            ctx.Events.OnMetalEssenceDemon += Born;
            ctx.Events.OnElderDied += (power, elder, demon) => { if (demon) BornInTheWorld(power, elder); };
        }

        private string Home => ctx.Content.Clan.HomeRegion;

        private string RegionOf(MetalEssenceDemon d) => d.RegionId ?? Home;

        /// <summary>
        /// A demon born of a power's elder (the user's rule, 2026-10-03: what befalls the clan befalls the world): it ravages
        /// its power's region, unless the Underworld's emissaries claim it at once.
        /// </summary>
        private void BornInTheWorld(Data.FactionData power, Data.FactionElder elder)
        {
            if (power?.RegionId == null || !ctx.Rng.Chance(Settings.WorldRavageChance)) return;
            var tier = elder.FruitionId != null ? DemonTier.Realization : elder.Realm >= CultivationRealm.PurpleMansion ? DemonTier.Ascent : DemonTier.Lesser;
            var demon = new MetalEssenceDemon(ctx.Rng.NextId(), elder.Name, tier, ctx.Clock.Year, ctx.Clock.Year + Settings.Tiers[tier].RavageYears)
                { RegionId = power.RegionId };
            ravaging.Add(demon);
            ctx.Log.Warning($"[Demons] {elder.Name} of {power.Name} becomes a demon ({tier}): it ravages {power.RegionId}.");
            ctx.Events.TriggerWorldDemon(demon);
        }

        private DemonSettings Settings => ctx.Content.Balance.Demons;

        public IReadOnlyList<MetalEssenceDemon> Pending => pending;
        public IReadOnlyList<MetalEssenceDemon> Ravaging => ravaging;
        public int Essences { get; private set; }
        public int GrudgeUntil { get; private set; }
        public int Grudge => System.Math.Max(0, GrudgeUntil - ctx.Clock.Year); // the years the Underworld's grudge has yet to run

        public void Restore(IEnumerable<MetalEssenceDemon> savedPending, IEnumerable<MetalEssenceDemon> savedRavaging, int essences, int grudgeUntil)
        {
            pending.Clear();
            if (savedPending != null) pending.AddRange(savedPending);
            ravaging.Clear();
            if (savedRavaging != null) ravaging.AddRange(savedRavaging);
            Essences = essences;
            GrudgeUntil = grudgeUntil;
        }

        private static DemonTier TierOf(CharacterData member) =>
            member.GoldenCore == GoldenCoreState.Realization ? DemonTier.Realization
            : member.Realm >= CultivationRealm.PurpleMansion ? DemonTier.Ascent : DemonTier.Lesser;

        private void Born(CharacterData member)
        {
            var demon = new MetalEssenceDemon(ctx.Rng.NextId(), member.FullName, TierOf(member), ctx.Clock.Year, 0);
            ctx.Events.TriggerDemonBorn(demon);
            if (Grudge > 0)
            {
                ctx.Log.Info($"[Demons] The Underworld's emissaries claim {member.FullName}'s essence at once.");
                return;
            }
            pending.Add(demon);
            ctx.Log.Warning($"[Demons] A Metal Essence Demon is born of {member.FullName} ({demon.Tier}): the clan must choose.");
        }

        private MetalEssenceDemon Take(string id)
        {
            var demon = pending.FirstOrDefault(d => d.Id == id);
            if (demon != null) pending.Remove(demon);
            return demon;
        }

        private bool HasAGoldenCoreForce =>
            clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Retreat == Retreat.None && m.Realm >= CultivationRealm.GoldenCore);

        /// <summary>The custom: the Underworld collects the essence. Null when done, else why not (French).</summary>
        public string LeaveToTheUnderworld(string id)
        {
            var demon = Take(id);
            if (demon == null) return "aucun démon n'attend ce choix";
            ctx.Log.Info($"[Demons] {demon.Name}'s essence is left to the Underworld.");
            return null;
        }

        /// <summary>A Golden Core of the clan seals the essence and keeps it, against the custom. Null when done, else why not (French).</summary>
        public string TakeTheEssence(string id)
        {
            if (pending.All(d => d.Id != id)) return "aucun démon n'attend ce choix";
            if (!HasAGoldenCoreForce) return "seule une force de Noyau d'Or peut sceller une essence";
            var demon = Take(id);
            Essences++;
            GrudgeUntil = ctx.Clock.Year + Settings.GrudgeYears;
            ctx.Log.Warning($"[Demons] The clan seals {demon.Name}'s essence: the Underworld is provoked.");
            ctx.Events.TriggerUnderworldProvoked();
            return null;
        }

        /// <summary>The demon is let be: it ravages the region by its rank. Null when done, else why not (French).</summary>
        public string LetItBe(string id)
        {
            var demon = Take(id);
            if (demon == null) return "aucun démon n'attend ce choix";
            ravaging.Add(demon with { UntilYear = ctx.Clock.Year + Settings.Tiers[demon.Tier].RavageYears, RegionId = Home });
            ctx.Log.Warning($"[Demons] {demon.Name}'s demon is let be: it ravages the region.");
            return null;
        }

        /// <summary>A Golden Core of the clan subdues a ravaging demon. Null when done, else why not (French).</summary>
        public string Subdue(string id)
        {
            var demon = ravaging.FirstOrDefault(d => d.Id == id);
            if (demon == null) return "aucun démon ne ravage la région";
            if (RegionOf(demon) != Home) return "ce démon ravage une autre région que celle du clan";
            if (!HasAGoldenCoreForce) return "seule une force de Noyau d'Or peut soumettre un démon";
            ravaging.Remove(demon);
            ctx.Log.Info($"[Demons] The clan subdues {demon.Name}'s demon.");
            ctx.Events.TriggerDemonSubdued(demon);
            return null;
        }

        /// <summary>An essence offered to the Underworld's officials soothes its grudge. Null when done, else why not (French).</summary>
        public string Bribe()
        {
            if (Essences == 0) return "le clan ne garde aucune essence";
            if (Grudge == 0) return "le Monde Souterrain n'a pas de rancune envers le clan";
            Essences--;
            GrudgeUntil = ctx.Clock.Year;
            ctx.Log.Info("[Demons] An essence soothes the Underworld's grudge.");
            return null;
        }

        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            pending.RemoveAll(d => d.Year < year); // unanswered: the custom prevails
            foreach (var demon in ravaging.ToList())
            {
                if (demon.UntilYear < year)
                {
                    ravaging.Remove(demon); // the Underworld claims it in the end
                    continue;
                }
                if (SubduedByTheRegion(demon)) continue;
                Ravage(demon);
            }
        }

        /// <summary>A True Monarch of a power of the region subdues the demon, by the year's odds.</summary>
        private bool SubduedByTheRegion(MetalEssenceDemon demon)
        {
            var monarch = factions.Factions.Where(f => f.RegionId == RegionOf(demon) && f.Elders.Any(e => e.Realm >= CultivationRealm.GoldenCore)).FirstOrDefault();
            if (monarch == null || !ctx.Rng.Chance(Settings.WorldSubdueChance)) return false;
            ravaging.Remove(demon);
            ctx.Log.Info($"[Demons] {monarch.Name} subdues {demon.Name}'s demon.");
            ctx.Events.TriggerDemonSubdued(demon);
            return true;
        }

        private void Ravage(MetalEssenceDemon demon)
        {
            var tier = Settings.Tiers[demon.Tier];
            string region = RegionOf(demon);
            foreach (var power in factions.Factions.Where(f => f.RegionId == region).ToList())
            {
                power.PowerLevel = System.Math.Max(0, power.PowerLevel - tier.PowerLoss);
                var elders = power.Elders.Where(e => e.Realm < CultivationRealm.GoldenCore).ToList();
                if (elders.Count == 0 || !ctx.Rng.Chance(tier.ElderKillChance)) continue;
                var victim = elders[ctx.Rng.Next(elders.Count)];
                power.Elders.Remove(victim);
                World.ElderSystem.Sync(power);
                ctx.Log.Warning($"[Demons] {demon.Name}'s demon kills {victim.Name} of {power.Name}.");
                ctx.Events.TriggerElderDied(power, victim, false);
            }
            if (region != Home) return; // the clan, elsewhere, is untouched
            resources.ConsumeSpiritStones(System.Math.Min(resources.SpiritStones, tier.StonesLost));
            if (!ctx.Rng.Chance(tier.KillChance)) return;
            var prey = clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm < CultivationRealm.GoldenCore).ToList();
            if (prey.Count > 0) clan.Kill(prey[ctx.Rng.Next(prey.Count)], DeathCause.DemonRavaged);
        }
    }
}
