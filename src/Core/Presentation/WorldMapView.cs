using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A power as the map shows it: name, kind, relation with the clan, strongest realm.</summary>
    public sealed record MapFaction(string Name, string Kind, int Relation, string HighestRealm);

    /// <summary>A place of the map (x west→east, y north→south, 0-1) and the powers living there.</summary>
    public sealed record MapPlace(string Id, string Name, RegionKind Kind, double X, double Y, bool IsState, bool IsHome,
        IReadOnlyList<MapFaction> Factions);

    /// <summary>A border between two places, named in ordinal order.</summary>
    public sealed record MapBorder(string A, string B);

    /// <summary>What the world map screen shows (LORE.md §7), from the content's map and the game's powers.</summary>
    public static class WorldMapView
    {
        public static IReadOnlyList<MapPlace> Places(GameSession session)
        {
            var content = session.Context.Content;
            return content.Regions
                .Select(r => new MapPlace(r.Id, r.Name, r.Kind, r.X, r.Y, r.ParentId == null, r.Id == content.Clan.HomeRegion,
                    session.Factions.Factions.Where(f => f.RegionId == r.Id).Select(ToMap).ToList()))
                .ToList();
        }

        /// <summary>Every border once.</summary>
        public static IReadOnlyList<MapBorder> Borders(GameSession session) =>
            session.Context.Content.Regions
                .SelectMany(r => r.Neighbours.Where(n => string.CompareOrdinal(r.Id, n) < 0).Select(n => new MapBorder(r.Id, n)))
                .ToList();

        /// <summary>Powers with no place on the map (an older save's invented factions).</summary>
        public static IReadOnlyList<MapFaction> Unplaced(GameSession session)
        {
            var regions = session.Context.Content.Regions;
            return session.Factions.Factions.Where(f => regions.All(r => r.Id != f.RegionId)).Select(ToMap).ToList();
        }

        public static string KindLabel(FactionKind kind) => kind switch
        {
            FactionKind.Family => "Famille",
            FactionKind.Sect => "Secte",
            FactionKind.Gate => "Porte",
            FactionKind.ImmortalIsland => "Île immortelle",
            FactionKind.Temple => "Temple",
            FactionKind.Order => "Ordre",
            FactionKind.State => "État",
            _ => kind.ToString()
        };

        private static MapFaction ToMap(FactionData f) =>
            new MapFaction(f.Name, KindLabel(f.Kind), f.RelationWithPlayer, RankCatalog.RealmName(f.HighestRealm));
    }
}
