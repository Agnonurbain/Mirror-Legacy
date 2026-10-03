using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>What the clan may give in an accord of knowledge (LORE.md §11.10, A).</summary>
    public enum AccordCurrency { Stones, Technique, Secret, Beast, Qi, Debt, Disciple, Artifact }

    /// <summary>One thing given: its currency, what it refers to (a technique, a secret, a beast, a Qi, a member), how much.</summary>
    public sealed record AccordTerm(AccordCurrency Currency, string Ref, int Amount);

    /// <summary>A debt the clan owes a power for a method (saved): the power will call it, at the worst moment.</summary>
    public sealed record KnowledgeDebt(string Power, int Year, string TechniqueId);

    /// <summary>
    /// Accords of knowledge (LORE.md §11.10; the user's decisions of 2026-09-30): the most precious thing in this world is a
    /// method, and a power parts with one for what it wants — a technique it lacks, a secret of a third party, a beast, Qi, a
    /// debt, a disciple — never for stones when the method leads to the Purple Mansion. It accepts when the worth covers the
    /// method and it trusts the clan enough (🔎 balance.json « knowledgeTrade »).
    /// </summary>
    public sealed class KnowledgeAccords
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TechniqueLibrary techniques;
        private readonly ResourceManager resources;
        private readonly SecretBook secrets;
        private readonly List<KnowledgeDebt> debts = new List<KnowledgeDebt>();

        public IReadOnlyList<KnowledgeDebt> Debts => debts;

        public KnowledgeAccords(GameContext ctx, ClanManager clan, FactionManager factions, TechniqueLibrary techniques, ResourceManager resources,
            SecretBook secrets)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.techniques = techniques;
            this.resources = resources;
            this.secrets = secrets;
        }

        private KnowledgeTradeSettings Settings => ctx.Content.Balance.KnowledgeTrade;

        private static bool IsAscent(TechniqueData t) => t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t);

        /// <summary>A high technique, of the Purple Mansion and its abilities: never bought with stones (the user's rule, 2026-10-03).</summary>
        public static bool IsPrecious(TechniqueData t) => t != null && TechniqueRules.HasPurpleMansionSecret(t);

        /// <summary>The clan's armoury, whose artifacts may be given in an accord (L4f; set by the session).</summary>
        public ArtifactArmoury Armoury { get; set; }

        /// <summary>A method of the catalog, or null.</summary>
        public TechniqueData Method(string techniqueId) => ctx.Content.Techniques.FirstOrDefault(t => t.ID == techniqueId);

        /// <summary>What a method is worth to its holder: its grade's price.</summary>
        public int PriceOf(TechniqueData method) => Settings.StonesPerGrade[method.Grade - 1];

        /// <summary>Why the power will not part with this method for these terms (French, for the screens), or null.</summary>
        public string Refusal(string powerName, string techniqueId, IReadOnlyList<AccordTerm> terms)
        {
            var power = factions.GetFactionByName(powerName);
            var method = ctx.Content.Techniques.FirstOrDefault(t => t.ID == techniqueId);
            if (power == null || method == null || !power.Techniques.Contains(techniqueId) || techniques.Knows(techniqueId))
                return "elle ne détient pas cet art, ou le clan le connaît déjà";
            int minRelation = IsAscent(method) ? Settings.AscentAccordMinRelation : Settings.MinRelation;
            if (power.RelationWithPlayer < minRelation) return $"la relation est trop froide ({minRelation} requise)";
            if ((terms ?? new List<AccordTerm>()).Select(t => t.Ref).Where(r => r != null).GroupBy(r => r).Any(g => g.Count() > 1))
                return "un même don ne compte qu'une fois";
            foreach (var term in terms ?? new List<AccordTerm>())
                if (TermRefusal(power, term) is { } why) return why;
            int worth = (terms ?? new List<AccordTerm>()).Sum(t => Worth(power, method, t));
            return worth < PriceOf(method) ? $"ce que le clan offre ne suffit pas ({worth} sur {PriceOf(method)})" : null;
        }

        /// <summary>Why the clan cannot give this to the power (French), or null.</summary>
        public string TermRefusal(string powerName, AccordTerm term) =>
            factions.GetFactionByName(powerName) is { } power ? TermRefusal(power, term) : "puissance inconnue";

        /// <summary>What a term is worth to the power for this method (0 when unknown).</summary>
        public int WorthOf(string powerName, string techniqueId, AccordTerm term)
        {
            var power = factions.GetFactionByName(powerName);
            var method = ctx.Content.Techniques.FirstOrDefault(t => t.ID == techniqueId);
            return power == null || method == null ? 0 : Worth(power, method, term);
        }

        private string TermRefusal(FactionData power, AccordTerm term) => TermRefusal(power?.Name, power, term);

        /// <summary>Why the clan cannot give this to the recipient — a power, or a lone figure (no power) — (French), or null.</summary>
        private string TermRefusal(string recipient, FactionData power, AccordTerm term)
        {
            switch (term.Currency)
            {
                case AccordCurrency.Stones:
                    return resources.SpiritStones < term.Amount ? $"il faut {term.Amount} pierres" : null;
                case AccordCurrency.Technique:
                    return !techniques.Knows(term.Ref) || power?.Techniques.Contains(term.Ref) == true ? "le clan ne peut offrir cet art" : null;
                case AccordCurrency.Secret:
                    var secret = secrets.All.FirstOrDefault(s => s.Id == term.Ref);
                    return secret == null || !secrets.Knows(SecretBook.ClanHolder, secret.Id) || secret.Holder == recipient || secrets.Knows(recipient, secret.Id)
                        ? "ce secret n'a rien à lui apprendre" : null;
                case AccordCurrency.Artifact:
                    return Armoury?.Armoury.Any(a => a.Id == term.Ref && a.LentBy == null) == true ? null : "cet artefact n'est pas au clan, dans son armurerie";
                case AccordCurrency.Debt:
                    return power == null ? "une figure isolée ne prend pas la parole du clan" : null;
                case AccordCurrency.Beast:
                    return resources.Beasts.All(b => b.Id != term.Ref) ? "le clan ne tient pas cette bête" : null;
                case AccordCurrency.Qi:
                    return resources.QiPortions(term.Ref) < term.Amount ? "le clan n'a pas ces portions de Qi" : null;
                case AccordCurrency.Disciple:
                    var member = clan.FindById(term.Ref);
                    return power == null || member == null || !member.IsAlive || member.CaptorFaction != null || member.ID == clan.PatriarchID
                        || !SpiritualOrificeRules.CanCultivate(member) ? "ce membre ne peut partir comme disciple" : null;
                default:
                    return null; // a debt: the clan's word
            }
        }

        /// <summary>What a term is worth to the power (🔎): stones buy no high technique.</summary>
        private int Worth(FactionData power, TechniqueData method, AccordTerm term) => TermWorth(term, PriceOf(method), IsPrecious(method));

        /// <summary>What a term is worth for a thing of this price — nothing in stones when the thing is precious.</summary>
        private int TermWorth(AccordTerm term, int price, bool precious)
        {
            var s = Settings;
            return term.Currency switch
            {
                AccordCurrency.Stones => precious ? 0 : term.Amount,
                AccordCurrency.Artifact => Armoury?.Armoury.FirstOrDefault(a => a.Id == term.Ref) is { } artifact
                    ? ArtifactRules.Worth(artifact, ctx.Content.Balance.Artifacts, s.ArtifactOreValue) : 0,
                AccordCurrency.Technique => ctx.Content.Techniques.FirstOrDefault(t => t.ID == term.Ref) is { } given ? s.StonesPerGrade[given.Grade - 1] : 0,
                AccordCurrency.Secret => secrets.All.FirstOrDefault(x => x.Id == term.Ref) is { } secret ? s.SecretWorthPerRank * secret.Rank : 0,
                AccordCurrency.Beast => resources.Beasts.FirstOrDefault(b => b.Id == term.Ref) is { } beast ? s.BeastWorthPerRealm * (int)beast.Realm : 0,
                AccordCurrency.Qi => s.QiWorthPerPortion * term.Amount,
                AccordCurrency.Debt => (int)(price * s.DebtWorthShare),
                AccordCurrency.Disciple => clan.FindById(term.Ref) is { } member ? s.DiscipleWorthPerRealm * (int)member.Realm : 0,
                _ => 0
            };
        }

        /// <summary>The accord is sealed: each term is given, and the clan learns the method. Null when done, else why not (French).</summary>
        public string Conclude(string powerName, string techniqueId, IReadOnlyList<AccordTerm> terms)
        {
            string refusal = Refusal(powerName, techniqueId, terms);
            if (refusal != null) return refusal;
            var power = factions.GetFactionByName(powerName);
            foreach (var term in terms) Give(power, techniqueId, term);
            techniques.Learn(techniqueId);
            ctx.Log.Info($"[Accords] {powerName} parts with « {techniqueId} » for {string.Join(", ", terms.Select(t => t.Currency))}.");
            return null;
        }

        /// <summary>Gives these terms to the power (they were checked by the caller).</summary>
        public void GiveTerms(string powerName, string techniqueId, IEnumerable<AccordTerm> terms)
        {
            var power = factions.GetFactionByName(powerName);
            if (power == null) return;
            foreach (var term in terms) Give(power, techniqueId, term);
        }

        private void Give(FactionData power, string techniqueId, AccordTerm term) => Give(power?.Name, power, techniqueId, term);

        private void Give(string recipient, FactionData power, string subject, AccordTerm term)
        {
            switch (term.Currency)
            {
                case AccordCurrency.Stones: resources.ConsumeSpiritStones(term.Amount); if (power != null) power.Wealth += term.Amount; break;
                case AccordCurrency.Technique: power?.Techniques.Add(term.Ref); break;
                case AccordCurrency.Secret: secrets.Grant(recipient, term.Ref); break;
                case AccordCurrency.Beast: resources.ConsumeBeast(resources.Beasts.First(b => b.Id == term.Ref)); break;
                case AccordCurrency.Qi: resources.ConsumeQi(term.Ref, term.Amount); break;
                case AccordCurrency.Debt: debts.Add(new KnowledgeDebt(recipient, ctx.Clock.Year, subject)); break;
                case AccordCurrency.Disciple: clan.Depart(clan.FindById(term.Ref)); break; // gone to serve the power
                case AccordCurrency.Artifact:
                    var given = Armoury?.TakeAway(term.Ref);
                    if (given != null) power?.Artifacts.Add(given);
                    break;
            }
        }

        // ---- A barter for any precious thing (the user's rule, 2026-10-03) ----

        /// <summary>What a term is worth toward a thing of this price (precious: stones weigh nothing).</summary>
        public int BarterWorth(AccordTerm term, int price, bool precious) => TermWorth(term, price, precious);

        /// <summary>Why the recipient — a power, or a lone figure by its name — refuses these terms for a thing of this price (French), or null.</summary>
        public string BarterRefusal(string recipient, int price, bool precious, IReadOnlyList<AccordTerm> terms)
        {
            var power = factions.GetFactionByName(recipient);
            var given = terms ?? new List<AccordTerm>();
            if (given.Select(t => t.Ref).Where(r => r != null).GroupBy(r => r).Any(g => g.Count() > 1)) return "un même don ne compte qu'une fois";
            foreach (var term in given)
                if (TermRefusal(recipient, power, term) is { } why) return why;
            int worth = given.Sum(t => TermWorth(t, price, precious));
            return worth < price ? $"ce que le clan offre ne suffit pas ({worth} sur {price}{(precious ? ", les pierres n'y comptent pas" : "")})" : null;
        }

        /// <summary>Gives these terms to the recipient (checked by the caller with <see cref="BarterRefusal"/>).</summary>
        public void Barter(string recipient, IEnumerable<AccordTerm> terms, string subject)
        {
            var power = factions.GetFactionByName(recipient);
            foreach (var term in terms) Give(recipient, power, subject, term);
            ctx.Log.Info($"[Accords] The clan gives {recipient} {string.Join(", ", terms.Select(t => t.Currency))} for {subject}.");
        }

        /// <summary>A debt owed for a method (a patron's price, §11.10).</summary>
        public void AddDebt(string power, string techniqueId) => debts.Add(new KnowledgeDebt(power, ctx.Clock.Year, techniqueId));

        /// <summary>A patron forgives what the clan owes it.</summary>
        public void ForgiveDebts(string power) => debts.RemoveAll(d => d.Power == power);

        public void RestoreDebts(IEnumerable<KnowledgeDebt> saved)
        {
            debts.Clear();
            debts.AddRange(saved ?? Enumerable.Empty<KnowledgeDebt>());
        }
    }
}
