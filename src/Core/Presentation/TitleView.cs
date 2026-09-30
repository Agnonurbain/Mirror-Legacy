using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A save slot: what it holds (or that it is empty or unreadable), and whether its game can be continued.</summary>
    public sealed record SlotLine(int Index, string Label, bool CanContinue, bool IsEmpty);

    /// <summary>The title screen (G6): the save slots, each an ironman game saved at the start of every year.</summary>
    public static class TitleView
    {
        public const int SlotCount = 3;

        /// <param name="readSlot">The text saved in a slot (1-based), or null when the slot is empty; a file that reads
        /// empty is unreadable, never an empty slot (it must not be overwritten unasked).</param>
        public static IReadOnlyList<SlotLine> Slots(Func<int, string> readSlot) =>
            Enumerable.Range(1, SlotCount).Select(i =>
            {
                string json = readSlot(i);
                if (json == null) return new SlotLine(i, $"Emplacement {i} — vide", false, true);
                var summary = SaveSummary.Read(json);
                if (summary == null) return new SlotLine(i, $"Emplacement {i} — sauvegarde illisible", false, false);
                string patriarch = summary.Patriarch == null ? "sans patriarche" : $"patriarche {summary.Patriarch}";
                return new SlotLine(i, $"Emplacement {i} — Clan {summary.ClanName}, an {summary.Year}, génération {summary.Generation}, "
                    + $"{summary.Living} vivant(s), {patriarch}", true, false);
            }).ToList();
    }
}
