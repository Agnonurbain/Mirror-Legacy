using System;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Autoload that owns the running game: it loads the content from res://data, resumes the saved game
    /// (user://) or starts a new one, and saves at the start of every year. A smoke run (<c>-- --smoke</c>)
    /// always starts a fresh seeded game and never touches the player's save.
    /// </summary>
    public partial class GameRoot : Node
    {
        public const string SavePath = "user://ironman_save.json";
        private const string DataDirectory = "res://data/";
        private const int SmokeSeed = 7;

        public GameSession Session { get; private set; }
        public Chronicle Chronicle { get; private set; }
        public bool IsSmokeRun { get; private set; }

        /// <summary>With <c>-- --smoke --screenshot=file.png</c>, the smoke run saves the window instead of quitting at once.</summary>
        public string ScreenshotPath { get; private set; }

        private const string ScreenshotArgument = "--screenshot=";

        /// <summary>Raised when a new game starts or a save is loaded: views bind to the new session.</summary>
        public event Action SessionChanged;

        private GameContent content;

        public override void _Ready()
        {
            var args = OS.GetCmdlineUserArgs();
            IsSmokeRun = args.Contains("--smoke");
            ScreenshotPath = args.Where(a => a.StartsWith(ScreenshotArgument)).Select(a => a.Substring(ScreenshotArgument.Length)).FirstOrDefault();
            content = GameContentLoader.Load(ReadDataFile); // unplayable content stops the game at startup, naming the file

            if (IsSmokeRun) Start(GameSession.NewGame(Setup(SmokeSeed)));
            else Start(LoadSave() ?? GameSession.NewGame(Setup(null)));
        }

        public void NewGame() => Start(GameSession.NewGame(Setup(null)));

        public void Save()
        {
            if (IsSmokeRun || Session == null) return;

            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushError($"[GameRoot] Cannot write the save ({Godot.FileAccess.GetOpenError()}).");
                return;
            }
            file.StoreString(SaveSerializer.Serialize(Session.ToSaveData()));
        }

        private GameSetup Setup(int? seed)
        {
            var setup = new GameSetup { Log = new GodotGameLog(), Content = content };
            return seed.HasValue ? setup with { Seed = seed.Value } : setup;
        }

        private GameSession LoadSave()
        {
            if (!Godot.FileAccess.FileExists(SavePath)) return null;
            try
            {
                var data = SaveSerializer.Deserialize(Godot.FileAccess.GetFileAsString(SavePath));
                return GameSession.FromSaveData(data, Setup(null));
            }
            catch (System.IO.InvalidDataException e)
            {
                GD.PushWarning($"[GameRoot] The save cannot be read, a new game begins: {e.Message}");
                return null;
            }
        }

        private void Start(GameSession session)
        {
            Session = session;
            Chronicle = new Chronicle(session);
            session.Events.OnYearStarted += year => Save();
            SessionChanged?.Invoke();
        }

        private static string ReadDataFile(string name)
        {
            string path = DataDirectory + name;
            return Godot.FileAccess.FileExists(path) ? Godot.FileAccess.GetFileAsString(path) : null;
        }
    }
}
