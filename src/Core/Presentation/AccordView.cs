using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    /// <summary>Something the clan could give in an accord: its label, the term, and what it is worth to the power.</summary>
    public sealed record AccordCandidate(string Label, AccordTerm Term, int Worth);

    /// <summary>The chosen terms weighed: their worth, the method's price, and why the power would refuse (null: it accepts).</summary>
    public sealed record AccordSummary(int Worth, int Price, string Refusal);

    /// <summary>
    /// The accords screen (LORE.md §11.10, A): for a method a power holds, everything the clan could give — an art the power
    /// lacks, a secret of a third party, a captured beast, its Qi, a debt, a disciple, stones for an ordinary method — and the
    /// worth of the chosen terms against the method's price.
    /// </summary>
    public static class AccordView
    {
        public static IReadOnlyList<AccordCandidate> Candidates(KnowledgeAccords accords, ClanManager clan, TechniqueLibrary techniques,
            ResourceManager resources, SecretBook secrets, string power, string techniqueId)
        {
            var method = accords.Method(techniqueId);
            if (method == null) return new List<AccordCandidate>();
            var terms = new List<(string Label, AccordTerm Term)>();

            if (!(method.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(method)))
                terms.Add(($"{accords.PriceOf(method)} pierres spirituelles", new AccordTerm(AccordCurrency.Stones, null, accords.PriceOf(method))));
            foreach (var art in techniques.Known.OrderByDescending(t => t.Grade))
                terms.Add(($"l'art « {art.Name} » (grade {art.Grade})", new AccordTerm(AccordCurrency.Technique, art.ID, 1)));
            foreach (var secret in secrets.KnownBy(SecretBook.ClanHolder))
                terms.Add(($"un secret de {secret.Holder} (rang {secret.Rank})", new AccordTerm(AccordCurrency.Secret, secret.Id, 1)));
            foreach (var beast in resources.Beasts)
                terms.Add(($"une bête spirituelle ({RankCatalog.RealmName(beast.Realm)})", new AccordTerm(AccordCurrency.Beast, beast.Id, 1)));
            foreach (var (qi, portions) in resources.SpiritualQi.Where(q => q.Value > 0))
                terms.Add(($"{portions} portion(s) de {techniques.FindQi(qi)?.Name ?? qi}", new AccordTerm(AccordCurrency.Qi, qi, portions)));
            terms.Add(("une dette : la puissance la réclamera un jour", new AccordTerm(AccordCurrency.Debt, null, 1)));
            foreach (var member in clan.LivingMembers.Where(m => m.ID != clan.PatriarchID && m.CaptorFaction == null && SpiritualOrificeRules.CanCultivate(m)))
                terms.Add(($"{member.FullName} part comme disciple ({RankCatalog.RealmName(member.Realm)})", new AccordTerm(AccordCurrency.Disciple, member.ID, 1)));

            return terms.Where(t => accords.TermRefusal(power, t.Term) == null)
                .Select(t => new AccordCandidate(t.Label, t.Term, accords.WorthOf(power, techniqueId, t.Term)))
                .Where(c => c.Worth > 0).ToList();
        }

        /// <summary>The cheapest bundle of present gifts (no debt, no disciple) worth at least <paramref name="price"/>; null when none is.</summary>
        public static IReadOnlyList<AccordCandidate> Bundle(IReadOnlyList<AccordCandidate> candidates, int price)
        {
            var bundle = new List<AccordCandidate>();
            foreach (var c in candidates.Where(c => c.Term.Currency != AccordCurrency.Debt && c.Term.Currency != AccordCurrency.Disciple).OrderBy(c => c.Worth))
            {
                if (bundle.Sum(b => b.Worth) >= price) break;
                bundle.Add(c);
            }
            return bundle.Sum(b => b.Worth) >= price ? bundle : null;
        }

        public static AccordSummary Summary(KnowledgeAccords accords, string power, string techniqueId, IReadOnlyList<AccordTerm> terms)
        {
            int worth = (terms ?? new List<AccordTerm>()).Sum(t => accords.WorthOf(power, techniqueId, t));
            var method = accords.Method(techniqueId);
            return new AccordSummary(worth, method == null ? 0 : accords.PriceOf(method), accords.Refusal(power, techniqueId, terms));
        }

    }
}
