using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A lineage as the clan knows it: its name, its state in words, and whether the mirror can lay it bare.</summary>
    public sealed record LineageRow(string Id, string Name, string State, bool CanReveal, string RevealLabel);

    /// <summary>
    /// The lineages of the world as the clan knows them (the library's « Lignées » tab, 2026-10-01): free, held and by whom,
    /// broken, hidden or suspected; a race open on a freed lineage and until when; a holder reborn who may come back.
    /// A hidden or suspected lineage offers the mirror's sight, at its price.
    /// </summary>
    public static class LineagesView
    {
        public static IReadOnlyList<LineageRow> Rows(GameSession s)
        {
            int cost = s.Context.Content.Balance.WorldFruitions.RevealMirrorCost;
            return s.Context.Content.Fruitions.Select(f =>
            {
                var state = s.Fruitions.State(f.Id);
                bool veiled = state.Status == FruitionStatus.Hidden || state.Status == FruitionStatus.Suspected;
                return new LineageRow(f.Id, f.Name, Describe(s, f.Id, state), veiled, veiled ? $"La révéler par le miroir ({cost})" : null);
            }).ToList();
        }

        private static string Describe(GameSession s, string id, FruitionState state)
        {
            string text = state.Status switch
            {
                FruitionStatus.Occupied => $"tenue par {state.Holder}",
                FruitionStatus.Free => "libre",
                FruitionStatus.Broken => "brisée",
                FruitionStatus.Hidden => "cachée : son état réel est inconnu",
                FruitionStatus.Suspected => $"soupçonnée tenue par {state.Holder}",
                _ => "inconnue",
            };
            if (s.WorldFruitions.Races.FirstOrDefault(r => r.FruitionId == id) is { } race) text += $" ; course ouverte jusqu'en l'an {race.UntilYear}";
            if (state.ReturningHolder != null) text += $" ; {state.ReturningHolder}, réincarné, pourrait revenir vers l'an {state.ReturnYear}";
            return text;
        }
    }
}
