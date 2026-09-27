using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>An art the clan knows: what it is, the Qi a method needs and the portions in store, who practises it.</summary>
    public sealed record TechniqueLine(string Id, string Name, string Kind, int Grade, string Category, string Element, string FirstRealm,
        string Qi, int QiInStore, IReadOnlyList<string> Practitioners);

    /// <summary>An art a power would sell: its price, and why not now (null when it would).</summary>
    public sealed record MarketLine(string Power, string TechniqueId, string Name, string Kind, int Grade, int Price, string Refusal);

    /// <summary>The clan's library and the market of knowledge (G6; LORE.md §2.3-§2.4).</summary>
    public static class LibraryView
    {
        /// <summary>The known arts: methods first, then by kind, the highest grade first.</summary>
        public static IReadOnlyList<TechniqueLine> Library(GameSession session)
        {
            var living = session.Clan.LivingMembers.ToList();
            return session.Techniques.Known
                .OrderBy(t => t.Kind).ThenByDescending(t => t.Grade).ThenBy(t => t.Name, StringComparer.Ordinal)
                .Select(t =>
                {
                    var qi = session.Techniques.FindQi(t.RequiredQiId);
                    int inStore = qi != null && session.Resources.SpiritualQi.TryGetValue(qi.Id, out var n) ? n : 0;
                    var practitioners = living.Where(m => m.CultivationMethodId == t.ID).Select(m => m.FullName).ToList();
                    return new TechniqueLine(t.ID, t.Name, KindLabel(t.Kind), t.Grade, CategoryLabel(t.Category),
                        WorldMapView.ElementLabel(t.DominantElement), RankCatalog.RealmName(t.RequiredRealm), qi?.Name, inStore, practitioners);
                })
                .ToList();
        }

        /// <summary>What every power would sell, the cheapest first, with why it will not sell now.</summary>
        public static IReadOnlyList<MarketLine> Market(GameSession session) =>
            session.Factions.Factions
                .SelectMany(f => session.Exchange.Offers(f.Name).Select(t => new MarketLine(f.Name, t.ID, t.Name, KindLabel(t.Kind), t.Grade,
                    session.Exchange.PriceOf(t, f.Name), session.Exchange.PurchaseRefusal(f.Name, t.ID))))
                .OrderBy(m => m.Price).ThenBy(m => m.Power, StringComparer.Ordinal).ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList();

        public static string KindLabel(TechniqueKind kind) => kind switch
        {
            TechniqueKind.Cultivation => "Méthode de cultivation",
            TechniqueKind.Spell => "Sort",
            TechniqueKind.Movement => "Art de mouvement",
            TechniqueKind.Weapon => "Art martial",
            TechniqueKind.ImmortalArt => "Art immortel",
            _ => kind.ToString()
        };

        public static string CategoryLabel(TechniqueCategory category) => category switch
        {
            TechniqueCategory.Common => "commune",
            TechniqueCategory.Ancestral => "ancestrale",
            TechniqueCategory.Secret => "secrète",
            _ => category.ToString()
        };
    }
}
