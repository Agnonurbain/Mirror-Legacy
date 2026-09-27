using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A treaty as the diplomacy screen shows it (the years left: null for an open treaty).</summary>
    public sealed record TreatyLine(string Id, string Kind, bool Secret, bool Sealed, int? YearsLeft, bool ClanIsSuzerain, int Grip);

    /// <summary>A power as the diplomacy screen shows it: never what it hides (D7).</summary>
    public sealed record PowerLine(string Name, string Kind, string HighestRealm, int Relation, IReadOnlyList<TreatyLine> Treaties);

    /// <summary>The diplomacy screen (G6; D7): the powers, the treaties that bind them to the clan, why a proposal fails.</summary>
    public static class DiplomacyView
    {
        /// <summary>Every power, the friendliest first.</summary>
        public static IReadOnlyList<PowerLine> Powers(GameSession session) =>
            session.Factions.Factions
                .OrderByDescending(f => f.RelationWithPlayer).ThenBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => new PowerLine(f.Name, WorldMapView.KindLabel(f.Kind), RankCatalog.RealmName(f.HighestRealm), f.RelationWithPlayer,
                    session.Treaties.With(f.Name).Select(t => new TreatyLine(t.Id, KindLabel(t.Kind), t.Secret, t.Sealed,
                        t.EndYear.HasValue ? t.EndYear.Value - session.Clock.Year : null, t.ClanIsSuzerain, t.Grip)).ToList()))
                .ToList();

        /// <summary>Why a power would refuse this treaty now; null when it would accept.</summary>
        public static string ProposalRefusal(GameSession session, string faction, TreatyKind kind, bool clanAsSuzerain, bool sealedByOath) =>
            session.Treaties.Refusal(session.Factions.GetFactionByName(faction), kind, sealedByOath, clanAsSuzerain);

        public static string KindLabel(TreatyKind kind) => kind switch
        {
            TreatyKind.NonAggression => "non-agression",
            TreatyKind.Trade => "commerce",
            TreatyKind.Defence => "défense mutuelle",
            TreatyKind.Vassalage => "vassalité",
            _ => kind.ToString()
        };
    }
}
