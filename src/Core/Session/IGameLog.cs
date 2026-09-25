using System.Collections.Generic;

namespace MirrorChronicles.Session
{
    /// <summary>Where the simulation writes its narrative and diagnostics; the Godot layer prints it.</summary>
    public interface IGameLog
    {
        void Info(string message);
        void Warning(string message);
    }

    /// <summary>Discards everything.</summary>
    public sealed class NullGameLog : IGameLog
    {
        public void Info(string message) { }
        public void Warning(string message) { }
    }

    /// <summary>Keeps every line in memory, for tests and headless runs.</summary>
    public sealed class RecordingGameLog : IGameLog
    {
        private readonly List<string> lines = new List<string>();

        public IReadOnlyList<string> Lines => lines;

        public void Info(string message) => lines.Add(message);
        public void Warning(string message) => lines.Add("WARNING " + message);
    }
}
