using System;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Autoload that owns the running game: it loads the content from res://data and keeps three ironman save slots
    /// (user://slot-N.json; the title screen continues, begins or erases them), saving the slot played at the start of
    /// every year. A smoke run (<c>-- --smoke</c>) always starts a fresh seeded game and never touches the player's saves.
    /// </summary>
    public partial class GameRoot : Node
    {
        /// <summary>The single save of the first versions: it becomes slot 1 when that slot is empty.</summary>
        public const string LegacySavePath = "user://ironman_save.json";
        private const string DataDirectory = "res://data/";
        private const int SmokeSeed = 7;

        public GameSession Session { get; private set; }
        public Chronicle Chronicle { get; private set; }
        public bool IsSmokeRun { get; private set; }

        /// <summary>With <c>-- --smoke --screenshot=file.png</c>, the smoke run saves the window instead of quitting at once.</summary>
        public string ScreenshotPath { get; private set; }

        private const string ScreenshotArgument = "--screenshot=";

        /// <summary>With <c>-- --smoke --map</c> (or <c>--ops</c>, reached through the map), the smoke run ends on the world map (the screen it checks and captures).</summary>
        public bool SmokeEndsOnMap { get; private set; }

        /// <summary>With <c>-- --smoke --map --ops</c>, the smoke run goes on from the map to the secret operations screen.</summary>
        public bool SmokeEndsOnOperations { get; private set; }

        /// <summary>With <c>--lib</c> (implies <c>--ops</c>), the smoke run goes on from the operations to the clan's library.</summary>
        public bool SmokeEndsOnLibrary { get; private set; }

        /// <summary>With <c>--tree</c> (implies <c>--lib</c>), the smoke run goes on from the library to the family tree.</summary>
        public bool SmokeEndsOnGenealogy { get; private set; }

        /// <summary>With <c>--dip</c> (implies <c>--tree</c>), the smoke run goes on from the family tree to the diplomacy screen.</summary>
        public bool SmokeEndsOnDiplomacy { get; private set; }

        /// <summary>With <c>--mir</c> (implies <c>--dip</c>), the smoke run goes on from the diplomacy screen to the mirror's screen.</summary>
        public bool SmokeEndsOnMirror { get; private set; }

        /// <summary>With <c>--bld</c> (implies <c>--mir</c>), the smoke run goes on from the mirror's screen to the buildings.</summary>
        public bool SmokeEndsOnBuildings { get; private set; }

        /// <summary>With <c>--title</c>, the smoke run stays on the title screen (to capture it).</summary>
        public bool SmokeStaysOnTitle { get; private set; }

        /// <summary>With <c>--bat</c> (implies <c>--bld</c>), the smoke run goes on from the buildings to a rival's challenge.</summary>
        public bool SmokeEndsOnBattle { get; private set; }

        /// <summary>Raised when a new game starts or a save is loaded: views bind to the new session.</summary>
        public event Action SessionChanged;

        private GameContent content;

        public override void _Ready()
        {
            var args = OS.GetCmdlineUserArgs();
            IsSmokeRun = args.Contains("--smoke");
            SmokeStaysOnTitle = args.Contains("--title");
            SmokeEndsOnBattle = args.Contains("--bat");
            SmokeEndsOnBuildings = args.Contains("--bld") || SmokeEndsOnBattle; // the battle is reached through the buildings
            SmokeEndsOnMirror = args.Contains("--mir") || SmokeEndsOnBuildings; // the buildings are reached through the mirror
            SmokeEndsOnDiplomacy = args.Contains("--dip") || SmokeEndsOnMirror; // the mirror is reached through diplomacy
            SmokeEndsOnGenealogy = args.Contains("--tree") || SmokeEndsOnDiplomacy; // diplomacy is reached through the tree
            SmokeEndsOnLibrary = args.Contains("--lib") || SmokeEndsOnGenealogy; // the tree is reached through the library
            SmokeEndsOnOperations = args.Contains("--ops") || SmokeEndsOnLibrary; // the library is reached through the operations
            SmokeEndsOnMap = args.Contains("--map") || SmokeEndsOnOperations; // the operations are reached through the map
            ScreenshotPath = args.Where(a => a.StartsWith(ScreenshotArgument)).Select(a => a.Substring(ScreenshotArgument.Length)).FirstOrDefault();
            content = GameContentLoader.Load(ReadDataFile); // unplayable content stops the game at startup, naming the file

            if (IsSmokeRun) Start(GameSession.NewGame(Setup(SmokeSeed)), slot: 0); // never saved
            else AdoptLegacySave();
        }

        /// <summary>The slot being played (1-based), or 0 when nothing is saved (a smoke run).</summary>
        public int CurrentSlot { get; private set; }

        public static string SlotPath(int slot) => $"user://slot-{slot}.json";

        /// <summary>The text saved in a slot, or null when the slot is empty.</summary>
        public string ReadSlot(int slot) =>
            Godot.FileAccess.FileExists(SlotPath(slot)) ? Godot.FileAccess.GetFileAsString(SlotPath(slot)) : null;

        /// <summary>Continues the game of a slot; false when it cannot be read.</summary>
        public bool Continue(int slot)
        {
            string json = ReadSlot(slot);
            if (json == null) return false;
            try
            {
                Start(GameSession.FromSaveData(SaveSerializer.Deserialize(json), Setup(null)), slot);
                return true;
            }
            catch (System.IO.InvalidDataException e)
            {
                GD.PushWarning($"[GameRoot] Slot {slot} cannot be read: {e.Message}");
                return false;
            }
        }

        /// <summary>A new game in a slot (its former game is lost: ironman), saved at once.</summary>
        public void NewGame(int slot, int? seed = null)
        {
            Start(GameSession.NewGame(Setup(seed)), slot);
            Save();
        }

        public void Erase(int slot)
        {
            if (Godot.FileAccess.FileExists(SlotPath(slot))) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SlotPath(slot)));
            if (CurrentSlot == slot) CurrentSlot = 0; // the game on screen is no longer saved
        }

        /// <summary>The single save of the first versions moves into slot 1 when that slot is empty.</summary>
        private void AdoptLegacySave()
        {
            if (!Godot.FileAccess.FileExists(LegacySavePath) || Godot.FileAccess.FileExists(SlotPath(1))) return;
            string json = Godot.FileAccess.GetFileAsString(LegacySavePath);
            using (var file = Godot.FileAccess.Open(SlotPath(1), Godot.FileAccess.ModeFlags.Write)) file?.StoreString(json);
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(LegacySavePath));
        }

        public void Save()
        {
            if (IsSmokeRun || Session == null || CurrentSlot == 0) return;

            using var file = Godot.FileAccess.Open(SlotPath(CurrentSlot), Godot.FileAccess.ModeFlags.Write);
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

        private void Start(GameSession session, int slot)
        {
            Session = session;
            CurrentSlot = slot;
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
