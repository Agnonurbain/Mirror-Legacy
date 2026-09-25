using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The yearly turn: Management → Events → Breakthrough → Inheritance, then a new year.
    /// </summary>
    public sealed class GameClock
    {
        public int Year { get; private set; } = 1;
        public GamePhase Phase { get; private set; } = GamePhase.Management;

        /// <summary>Moves to the next phase; returns true when a new year has begun.</summary>
        public bool Advance()
        {
            if (Phase == GamePhase.Inheritance)
            {
                Year++;
                Phase = GamePhase.Management;
                return true;
            }

            Phase++;
            return false;
        }

        public void Restore(int year, GamePhase phase)
        {
            Year = year;
            Phase = phase;
        }
    }
}
