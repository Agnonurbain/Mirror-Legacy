using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// How to start or load a session: the seed, where the log goes, and the game's content
    /// (required: load it with <see cref="GameContentLoader"/>).
    /// </summary>
    public sealed record GameSetup
    {
        public int Seed { get; init; } = Environment.TickCount;
        public IGameLog Log { get; init; } = new NullGameLog();
        public GameContent Content { get; init; }

        /// <summary>
        /// A player is there to fight (G6): an ambush not foiled waits for the clan on the battle screen. Off for headless
        /// runs — tests, balance runs, the pilot — where chance settles it as before.
        /// </summary>
        public bool InteractiveBattles { get; init; }
    }
}
