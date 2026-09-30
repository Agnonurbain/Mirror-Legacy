using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>A patron's offer awaiting the clan's answer (saved): who, which method, its hidden design, since when.</summary>
    public sealed record SponsorOffer(string Power, string TechniqueId, string DesignId, int Year);

    /// <summary>An accepted patronage (saved): its design, whether it has fallen due, whether the mirror cleansed the manual.</summary>
    public sealed record Sponsorship(string Id, string Power, string TechniqueId, string DesignId, int Year, bool Due)
    {
        public bool Cleansed { get; init; }
    }

    /// <summary>
    /// Patrons (LORE.md §11.10, C; the user's decisions of 2026-09-30): the likeliest way to an ascent method is a power that
    /// offers it — always a plot. While the clan knows no way up and has a Foundation waiting, a power holding such a method may
    /// offer it, with a hidden design drawn from designs.json. Accepting teaches the method, leaves a debt, and hides the design
    /// among the patron's secrets (the clan's probes may pierce it). The design falls due at its milestone (its consequences
    /// come with each design); the mirror can cleanse a marked or flawed manual (§11.5).
    /// </summary>
    public sealed class Sponsorships
    {
        private const string DesignSecret = "patron-design";

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TechniqueLibrary techniques;
        private readonly SecretBook secrets;
        private readonly KnowledgeAccords accords;
        private readonly MirrorSystem mirror;
        private readonly List<Sponsorship> active = new List<Sponsorship>();

        public SponsorOffer Pending { get; private set; }
        public IReadOnlyList<Sponsorship> Active => active;

        public Sponsorships(GameContext ctx, ClanManager clan, FactionManager factions, TechniqueLibrary techniques, SecretBook secrets,
            KnowledgeAccords accords, MirrorSystem mirror)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.techniques = techniques;
            this.secrets = secrets;
            this.accords = accords;
            this.mirror = mirror;

            ctx.Events.OnBreakthroughSuccess += (member, realm) =>
            {
                if (realm == CultivationRealm.PurpleMansion) Milestone(member, DesignDue.PurpleMansion);
                if (realm == CultivationRealm.GoldenCore) Milestone(member, DesignDue.GoldenCore);
            };
            ctx.Events.OnCharacterBorn += child =>
            {
                foreach (var parent in new[] { clan.FindById(child.FatherID), clan.FindById(child.MotherID) }.Where(p => p != null))
                    Milestone(parent, DesignDue.HeirBorn);
            };
            ctx.Events.OnYearStarted += year =>
            {
                foreach (var s in active.Where(s => !s.Due && Design(s).Due == DesignDue.Years && year - s.Year >= Design(s).Years).ToList()) FallDue(s);
            };
        }

        private KnowledgeTradeSettings Settings => ctx.Content.Balance.KnowledgeTrade;

        private PatronDesign Design(Sponsorship s) => ctx.Content.PatronDesigns.First(d => d.Id == s.DesignId);

        private static bool IsAscent(TechniqueData t) => t != null && t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t);

        /// <summary>The clan knows no way up yet, and a Foundation waits for one.</summary>
        private bool NeedsAWayUp() =>
            !techniques.Known.Any(IsAscent)
            && clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Realm == CultivationRealm.Foundation);

        /// <summary>A year: a power that holds an ascent method may offer it (🔎 balance.json « knowledgeTrade.patronOfferChance »).</summary>
        public void ProcessYear()
        {
            if (Pending != null || !NeedsAWayUp()) return;
            var patrons = factions.Factions
                .Select(f => (Power: f, Method: f.Techniques.Select(id => ctx.Content.Techniques.FirstOrDefault(t => t.ID == id)).Where(IsAscent)
                    .OrderByDescending(t => t.Grade).FirstOrDefault()))
                .Where(p => p.Method != null).ToList();
            if (patrons.Count == 0 || !ctx.Rng.Chance(Settings.PatronOfferChance)) return;
            var (power, method) = patrons[ctx.Rng.Next(patrons.Count)];
            Pending = new SponsorOffer(power.Name, method.ID, DrawDesign().Id, ctx.Clock.Year);
            ctx.Log.Info($"[Patrons] {power.Name} offers the clan « {method.Name} ».");
            ctx.Events.TriggerPatronOffer(Pending);
        }

        private PatronDesign DrawDesign()
        {
            var designs = ctx.Content.PatronDesigns;
            int roll = ctx.Rng.Next(designs.Sum(d => d.Weight));
            foreach (var d in designs)
            {
                if (roll < d.Weight) return d;
                roll -= d.Weight;
            }
            return designs.Last();
        }

        /// <summary>The clan accepts: the method, a debt, and the design hidden among the patron's secrets. Null when done.</summary>
        public string Accept()
        {
            if (Pending == null) return "aucune offre n'attend";
            var offer = Pending;
            Pending = null;
            if (factions.GetFactionByName(offer.Power) == null) return $"{offer.Power} n'est plus";
            var sponsorship = new Sponsorship(ctx.Rng.NextId(), offer.Power, offer.TechniqueId, offer.DesignId, ctx.Clock.Year, false);
            active.Add(sponsorship);
            techniques.Learn(offer.TechniqueId);
            accords.AddDebt(offer.Power, offer.TechniqueId);
            secrets.Create(DesignSecret, offer.Power, sponsorship.Id);
            ctx.Log.Info($"[Patrons] The clan accepts « {offer.TechniqueId} » from {offer.Power}.");
            return null;
        }

        /// <summary>The clan declines: the patron takes it ill.</summary>
        public void Refuse()
        {
            if (Pending == null) return;
            if (factions.GetFactionByName(Pending.Power) is { } power) factions.ChangeRelation(power.ID, -Settings.PatronRefusalLoss);
            Pending = null;
        }

        /// <summary>Whether the clan has pierced the patron's design (its secret).</summary>
        public bool IsRevealed(Sponsorship s) =>
            secrets.All.Any(x => x.KindId == DesignSecret && x.Subject == s.Id && secrets.Knows(SecretBook.ClanHolder, x.Id));

        /// <summary>The mirror washes a mark or a flaw out of the manual (§11.5). Null when done, else why not (French).</summary>
        public string Cleanse(string sponsorshipId)
        {
            var s = active.FirstOrDefault(x => x.Id == sponsorshipId);
            if (s == null || s.Cleansed || !Design(s).Cleansable) return "le miroir n'y trouve rien à nettoyer";
            if (!mirror.ConsumePower(Settings.CleanseMirrorCost)) return mirror.PayRefusal(Settings.CleanseMirrorCost) ?? "le miroir ne peut pas";
            active[active.IndexOf(s)] = s with { Cleansed = true };
            ctx.Log.Info($"[Patrons] The mirror cleanses the manual « {s.TechniqueId} » of its patron's hold.");
            return null;
        }

        private void Milestone(CharacterData member, DesignDue due)
        {
            if (member == null) return;
            foreach (var s in active.Where(s => !s.Due && Design(s).Due == due && member.CultivationMethodId == s.TechniqueId).ToList()) FallDue(s);
        }

        private void FallDue(Sponsorship s)
        {
            if (s.Cleansed) return; // the mark is gone: the design has nothing left to hold
            var due = s with { Due = true };
            active[active.IndexOf(s)] = due;
            ctx.Log.Warning($"[Patrons] {s.Power}'s design « {s.DesignId} » falls due.");
            ctx.Events.TriggerPatronDesignDue(due, Design(due));
        }

        public void Restore(SponsorOffer pending, IEnumerable<Sponsorship> saved)
        {
            Pending = pending;
            active.Clear();
            active.AddRange(saved ?? Enumerable.Empty<Sponsorship>());
        }
    }
}
