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
    public sealed record ArtMemberSkill(ImmortalArt Art, string Name, string Gift, int Mastery, string MasteryRank, string Refusal);

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
                            ImmortalArtRules.Rank(mastery, set), s.Arts.PracticeRefusal(m, d.Art));
                    }).ToList()))
                .OrderByDescending(l => l.Practising != null).ThenByDescending(l => l.Skills.Count(k => k.Gift != "sans don"))
                .ThenBy(l => l.Name, System.StringComparer.Ordinal).ToList();
        }
    }
}
