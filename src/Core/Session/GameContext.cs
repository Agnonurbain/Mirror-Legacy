using System;
using MirrorChronicles.Events;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// What every system of a session shares: its event bus, its log, its random source (seedable,
    /// so a game can be replayed) and its clock.
    /// </summary>
    public sealed class GameContext
    {
        public GameEventBus Events { get; }
        public IGameLog Log { get; }
        public Random Rng { get; }
        public GameClock Clock { get; }

        public GameContext(GameEventBus events, IGameLog log, Random rng, GameClock clock)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }
    }
}
