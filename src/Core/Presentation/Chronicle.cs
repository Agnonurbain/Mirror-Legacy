using System;
using System.Collections.Generic;
using MirrorChronicles.Characters;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>
    /// The clan's chronicle as the player reads it: births, deaths, breakthroughs and events, one French
    /// line each, dated by year. Keeps the most recent entries only. (The full Annals arrive with L6.)
    /// </summary>
    public sealed class Chronicle
    {
        public const int DefaultCapacity = 200;

        private readonly GameSession session;
        private readonly int capacity;
        private readonly List<string> entries = new List<string>();

        public IReadOnlyList<string> Entries => entries;
        public event Action<string> OnEntryAdded;

        public Chronicle(GameSession session, int capacity = DefaultCapacity)
        {
            this.session = session;
            this.capacity = Math.Max(1, capacity);

            var bus = session.Events;
            bus.OnCharacterBorn += c => Add(c.Age == 0 ? $"naissance de {c.FullName}." : $"{c.FullName} rejoint le clan.");
            bus.OnCharacterDied += (c, cause) => Add($"{c.FullName} meurt {ClanDomainView.DeathLabel(cause)}.");
            bus.OnAncestorAscended += c => Add($"{c.FullName} s'élève au-delà du monde mortel.");
            bus.OnBreakthroughSuccess += (c, realm) => Add($"{c.FullName} atteint {RankCatalog.DisplayName(c)}.");
            bus.OnBreakthroughFailed += c => Add($"{c.FullName} échoue à sa percée.");
            bus.OnRandomEventOccurred += e => Add($"{e.Name} — {e.Description}");
            bus.OnStoryEventRaised += e => Add($"{e.Name}.");
            bus.OnGameOver += won => Add(won ? "la lignée devient éternelle." : "la lignée s'éteint.");
        }

        private void Add(string text)
        {
            string entry = $"An {session.Clock.Year} : {text}";
            entries.Add(entry);
            if (entries.Count > capacity) entries.RemoveAt(0);
            OnEntryAdded?.Invoke(entry);
        }
    }
}
