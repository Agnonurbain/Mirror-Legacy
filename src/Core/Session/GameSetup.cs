using System;
using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// How to start or load a session: the seed, the clan's name, where the log goes, and the data tables
    /// (null tables use the built-in placeholders until phase G2 loads them from JSON).
    /// </summary>
    public sealed class GameSetup
    {
        public int Seed { get; init; } = Environment.TickCount;
        public string ClanName { get; init; } = GameSession.DefaultClanName;
        public IGameLog Log { get; init; } = new NullGameLog();
        public IReadOnlyList<RandomEventData> RandomEvents { get; init; }
        public IReadOnlyList<StoryEventData> StoryEvents { get; init; }
    }
}
