using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>An Immortal Art as the clan holds it: its name, whether the clan holds its legacy, its best master.</summary>
    public sealed record ArtLegacyLine(ImmortalArt Art, string Name, bool Held, string Master);

    /// <summary>A member before the arts: its gift for each (as the mirror perceives it), its mastery, what it practises, and the arts it may take up.</summary>
    public sealed record ArtMemberLine(string Id, string Name, string Rank, string Practising, IReadOnlyList<ArtMemberSkill> Skills);

    /// <summary>One art for one member: the gift, the mastery's rank, and why it cannot practise it (null when it can).</summary>
    public sealed record ArtMemberSkill(ImmortalArt Art, string Name, string Gift, int Mastery, string MasteryRank, string Refusal)
    {
        public string Yield { get; init; } // what a year of it sells for (the talismans, audit §2.4), else null
    }

    /// <summary>An Essence Gathering Pill the clan's best alchemist may refine, of an element its Qi Cultivators need, and why not (null: it can).</summary>
    public sealed record PillOffer(Element Element, string AlchemistId, string Label, string Refusal);

    /// <summary>The examination of the clan's pills by its best alchemist, and why not (null: it can).</summary>
    public sealed record ExamineOffer(string AlchemistId, string Label, string Refusal);

    /// <summary>The Immortal Arts' screen (audit §2, 2026-10-04): the legacies, and each cultivator's gifts and mastery.</summary>
    public static class ArtView
    {
        public static IReadOnlyList<ArtLegacyLine> Legacies(GameSession s) =>
            s.Context.Content.Balance.Arts.Arts.Select(d => new ArtLegacyLine(d.Art, d.Name, s.Arts.HoldsLegacy(d.Art), s.Arts.MasterOf(d.Art)?.FullName)).ToList();

        /// <summary>The clan's cultivators of the Qi Cultivation and above, the gifted and the practising first.</summary>
        public static IReadOnlyList<ArtMemberLine> Members(GameSession s)
        {
            var set = s.Context.Content.Balance.Arts;
            return s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.QiRefinement)
                .Select(m => new ArtMemberLine(m.ID, m.FullName, RankCatalog.DisplayName(m),
                    m.CurrentTask == TaskType.ArtPractice && m.PracticedArt is { } art ? set.Arts.FirstOrDefault(a => a.Art == art)?.Name : null,
                    set.Arts.Select(d =>
                    {
                        int mastery = ArtSystem.MasteryOf(m, d.Art);
                        return new ArtMemberSkill(d.Art, d.Name, ImmortalArtRules.GiftLabel(ImmortalArtRules.Gift(m, d.Art, set)), mastery,
                            ImmortalArtRules.Rank(mastery, set), s.Arts.PracticeRefusal(m, d.Art))
                        {
                            Yield = d.Art == ImmortalArt.Talismans ? $"≈ {ImmortalArtRules.TalismanStones(m, set)} pierres l'an" : null
                        };
                    }).ToList()))
                .OrderByDescending(l => l.Practising != null).ThenByDescending(l => l.Skills.Count(k => k.Gift != "sans don"))
                .ThenBy(l => l.Name, System.StringComparer.Ordinal).ToList();
        }

        /// <summary>The clan's Essence Gathering Pills, by element (French).</summary>
        public static string PillStock(GameSession s) =>
            "Pilules de Rassemblement d'Essence : " + (s.Alchemy.EssencePills.Count == 0 ? "aucune"
                : string.Join(", ", s.Alchemy.EssencePills.OrderBy(p => p.Key).Select(p => $"{WorldMapView.ElementLabel(p.Key)} {p.Value}")));

        /// <summary>One offer per element the clan's Qi Cultivators need for the Foundation wall, by its best alchemist (audit §2.6).</summary>
        public static IReadOnlyList<PillOffer> PillOffers(GameSession s)
        {
            var needs = s.Clan.LivingMembers.Where(m => m.Realm == CultivationRealm.QiRefinement)
                .Select(m => AlchemySystem.ElementFor(m, s.Context.Content)).Where(e => e.HasValue).Select(e => e.Value).Distinct().OrderBy(e => e).ToList();
            var alchemist = s.Clan.LivingMembers.OrderBy(m => s.Alchemy.RefineRefusal(m) == null ? 0 : 1)
                .ThenByDescending(m => ArtSystem.MasteryOf(m, ImmortalArt.Alchemy)).FirstOrDefault();
            var pill = s.Context.Content.Balance.Arts.EssencePill;
            return needs.Select(e => new PillOffer(e, alchemist?.ID,
                $"Raffiner une Pilule de Rassemblement d'Essence ({WorldMapView.ElementLabel(e)})" + (alchemist == null ? "" : $" par {alchemist.FullName}")
                + $" — {pill.Herbs} herbes, {pill.Stones} pierres",
                alchemist == null ? "aucun alchimiste" : s.Alchemy.RefineRefusal(alchemist))).ToList();
        }

        /// <summary>The store's examination for poison (audit §2.7), by the clan's best alchemist.</summary>
        public static ExamineOffer ExamineOffer(GameSession s)
        {
            var alchemist = s.Clan.LivingMembers.OrderBy(m => s.Alchemy.ExamineRefusal(m) == null ? 0 : 1)
                .ThenByDescending(m => ArtSystem.MasteryOf(m, ImmortalArt.Alchemy)).FirstOrDefault();
            return new ExamineOffer(alchemist?.ID, "Examiner les pilules (un poison s'y cache-t-il ?)" + (alchemist == null ? "" : $" par {alchemist.FullName}"),
                alchemist == null ? "aucun alchimiste" : s.Alchemy.ExamineRefusal(alchemist));
        }
    }
}
