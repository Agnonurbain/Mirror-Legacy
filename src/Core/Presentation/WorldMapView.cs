using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A power as the map shows it: name, kind, relation with the clan, strongest realm.</summary>
    public sealed record MapFaction(string Name, string Kind, int Relation, string HighestRealm);

    /// <summary>A named cultivator of a power, as the map shows it.</summary>
    public sealed record MapFigure(string Name, string Realm);

    /// <summary>A beast the clan scouted, as the map shows it: its species, strength and owner (« solitaire » for none).</summary>
    public sealed record MapBeast(string Species, string Strength, string Owner);

    /// <summary>
    /// A place of the map (x west→east, y north→south, 0-1), the powers living there, the density of its Qi and the
    /// atmosphere over it (harsh when it weighs on every cultivator, L5b).
    /// </summary>
    public sealed record MapPlace(string Id, string Name, RegionKind Kind, double X, double Y, bool IsState, bool IsHome,
        IReadOnlyList<MapFaction> Factions, double QiDensity = 1.0, string Atmosphere = null, bool AtmosphereHarsh = false);

    /// <summary>A Qi a place offers, and how abundant it is there (L5b).</summary>
    public sealed record MapQi(string Name, string Abundance);

    /// <summary>The Qi of a place: its density, its Qi, the atmosphere over it and what it does (L5b; LORE.md §2.5, §5.8).</summary>
    public sealed record PlaceQiView(string Density, IReadOnlyList<MapQi> Qi, string Atmosphere, string AtmosphereEffect, string AtmosphereNotes);

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
                    session.Factions.Factions.Where(f => f.RegionId == r.Id).Select(ToMap).ToList(),
                    session.Place.Density(r.Id), session.Place.AtmosphereOf(r.Id)?.Name, session.Place.AtmosphereOf(r.Id)?.GeneralSpeed < 0))
                .ToList();
        }

        /// <summary>The Qi of a place (the most abundant first) and its atmosphere; null for an unknown place.</summary>
        public static PlaceQiView QiOf(GameSession session, string regionId)
        {
            var place = session.Place;
            var content = session.Context.Content;
            if (content.Regions.All(r => r.Id != regionId)) return null;
            double density = place.Density(regionId);
            var qi = place.QiOf(regionId)
                .Select(q => (q.Name, Abundance: place.Abundance(regionId, q)))
                .OrderByDescending(q => q.Abundance).ThenBy(q => q.Name, System.StringComparer.Ordinal)
                .Select(q => new MapQi(q.Name, AbundanceLabel(q.Abundance)))
                .ToList();
            var atmosphere = place.AtmosphereOf(regionId);
            return new PlaceQiView(DensityLabel(density), qi, atmosphere?.Name, AtmosphereEffect(atmosphere, content), atmosphere?.Notes);
        }

        public static string DensityLabel(double density) =>
            density >= 1.15 ? "Qi dense" : density >= 0.85 ? "Qi ordinaire" : "Qi maigre";

        public static string AbundanceLabel(double abundance) =>
            abundance >= 1.2 ? "abondant" : abundance >= 0.85 ? "présent" : "rare";

        /// <summary>What an atmosphere does, in words: its weight on all, and whom it favours.</summary>
        public static string AtmosphereEffect(AtmosphereDefinition atmosphere, GameContent content)
        {
            if (atmosphere == null) return null;
            var parts = new List<string>();
            if (atmosphere.GeneralSpeed != 0) parts.Add($"cultivation de tous {Percent(atmosphere.GeneralSpeed)}");
            var favoured = atmosphere.FavouredFruitions.Select(id => content.Fruitions.FirstOrDefault(f => f.Id == id)?.Name ?? id)
                .Concat(atmosphere.FavouredElements.Select(ElementLabel))
                .Concat(atmosphere.FavouredPaths.Select(PathLabel))
                .ToList();
            if (favoured.Count > 0)
                parts.Add($"favorise {string.Join(", ", favoured)} (cultivation {Percent(atmosphere.FavouredSpeed)}, percée +{atmosphere.FavouredBreakthrough})");
            return parts.Count == 0 ? "sans effet connu" : string.Join(" ; ", parts);
        }

        private static string Percent(double value) =>
            (value < 0 ? "−" : "+") + (System.Math.Abs(value) * 100).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + " %";

        public static string PathLabel(CultivationPath path) => path switch
        {
            CultivationPath.Immortal => "Dao Immortel",
            CultivationPath.Devil => "Dao du Diable",
            CultivationPath.Buddhist => "bouddhisme",
            CultivationPath.Demonic => "Dao Démoniaque",
            CultivationPath.Shamanic => "chamanisme",
            CultivationPath.Divine => "Dao Divin",
            _ => path.ToString()
        };

        public static string ElementLabel(Element element) => element switch
        {
            Element.Fire => "Feu",
            Element.Water => "Eau",
            Element.Wood => "Bois",
            Element.Metal => "Métal",
            Element.Earth => "Terre",
            Element.Lightning => "Foudre",
            Element.Darkness => "Ténèbres",
            Element.Light => "Lumière",
            _ => "aucun élément"
        };

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

        /// <summary>The named cultivators of a power already born this year (the strongest first).</summary>
        public static IReadOnlyList<MapFigure> FiguresOf(GameSession session, string factionName) =>
            session.Context.Content.Figures
                .Where(f => f.FactionName == factionName && (f.BornYear ?? int.MinValue) <= session.Clock.Year)
                .OrderByDescending(f => f.Realm)
                .Select(f => new MapFigure(f.Name, RankCatalog.RealmName(f.Realm)))
                .ToList();

        /// <summary>The beasts of a place the clan has scouted (L2c.2): nothing of what it does not know.</summary>
        public static IReadOnlyList<MapBeast> KnownBeastsOf(GameSession session, string regionId) =>
            session.Bestiary.In(regionId)
                .Where(b => session.Knowledge.Knows(World.FactKind.Beast, b.Id))
                .Select(b => new MapBeast(
                    session.Context.Content.BeastSpecies.FirstOrDefault(sp => sp.Id == b.SpeciesId)?.Name ?? b.SpeciesId,
                    $"{RankCatalog.RealmName(b.Realm)}, stade {b.Stage}",
                    b.OwnerFaction ?? "solitaire"))
                .ToList();

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
