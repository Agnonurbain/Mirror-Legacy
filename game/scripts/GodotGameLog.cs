using Godot;
using MirrorChronicles.Session;

namespace MirrorChronicles.Game
{
    /// <summary>Sends the simulation's diagnostic log to Godot's output.</summary>
    public sealed class GodotGameLog : IGameLog
    {
        public void Info(string message) => GD.Print(message);

        // Gameplay warnings (not enough stones, a refused task) are not engine errors: print, don't push.
        public void Warning(string message) => GD.Print("WARNING " + message);
    }
}
