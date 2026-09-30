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
            inkInSmoke = args.Contains("--ink");
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
            AddChild(new PaperBackdrop()); // rice paper behind every screen (Shuimo)
            BronzeTexture.Apply(ThemeDB.GetProjectTheme()); // worn bronze on the buttons
            ink = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
            ink.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            ink.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://theme/curtain.gdshader") }; // the curtain (inspired by valaxy-theme-shuimo)
            var above = new CanvasLayer { Layer = 100 };
            above.AddChild(ink);
            AddChild(above);

            if (IsSmokeRun) Start(GameSession.NewGame(Setup(SmokeSeed)), slot: 0); // never saved
            else AdoptLegacySave();
        }

        /// <summary>The slot being played (1-based), or 0 when nothing is saved (a smoke run).</summary>
        public int CurrentSlot { get; private set; }

        public static string SlotPath(int slot)
        {
            if (slot < 1 || slot > TitleView.SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return $"user://slot-{slot}.json";
        }

        /// <summary>The text saved in a slot; null when the slot is empty, "" when its file cannot be read.</summary>
        public string ReadSlot(int slot)
        {
            string path = SlotPath(slot);
            if (!Godot.FileAccess.FileExists(path)) return null;
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            return file == null ? "" : file.GetAsText();
        }

        /// <summary>Continues the game of a slot; the reason when it cannot (never a crash).</summary>
        public string Continue(int slot)
        {
            string json = ReadSlot(slot);
            if (string.IsNullOrEmpty(json)) return "cette sauvegarde ne peut être lue";
            var session = GameSession.TryLoad(json, Setup(null), out string error);
            if (session == null)
            {
                GD.PushWarning($"[GameRoot] Slot {slot} cannot be loaded: {error}");
                return "cette sauvegarde ne peut être chargée";
            }
            Start(session, slot);
            return null;
        }

        /// <summary>A new game in a slot — its former game is lost (ironman) — saved at once; false when it could not be saved.</summary>
        public bool NewGame(int slot, int? seed = null)
        {
            Start(GameSession.NewGame(Setup(seed)), slot);
            return Save();
        }

        /// <summary>Erases a slot; the reason when the file could not be removed. The game on screen, if it was this one, ends.</summary>
        public string Erase(int slot)
        {
            string path = SlotPath(slot);
            if (Godot.FileAccess.FileExists(path))
            {
                var error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
                if (error != Error.Ok) return $"l'effacement a échoué ({error})";
            }
            if (CurrentSlot == slot) Stop(); // no stale game played unsaved
            return null;
        }

        /// <summary>
        /// The single save of the first versions moves into slot 1 when that slot is empty; the old file is removed only
        /// once the copy is written and read back the same.
        /// </summary>
        private void AdoptLegacySave()
        {
            if (!Godot.FileAccess.FileExists(LegacySavePath) || Godot.FileAccess.FileExists(SlotPath(1))) return;
            string json = Godot.FileAccess.GetFileAsString(LegacySavePath);
            if (string.IsNullOrWhiteSpace(json)) return; // nothing sure to move: leave it where it is
            if (!WriteSafely(SlotPath(1), json))
            {
                GD.PushWarning("[GameRoot] The old save could not be moved into slot 1; it is kept.");
                return;
            }
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(LegacySavePath));
        }

        /// <summary>Saves the game into its slot, never leaving a half-written file; false when it could not.</summary>
        public bool Save()
        {
            if (IsSmokeRun || Session == null || CurrentSlot == 0) return false;
            string json = SaveSerializer.Serialize(Session.ToSaveData()); // before any file is touched
            if (WriteSafely(SlotPath(CurrentSlot), json)) return true;
            GD.PushError($"[GameRoot] The game could not be saved into slot {CurrentSlot}.");
            return false;
        }

        /// <summary>Writes a temporary file, reads it back, then puts it in place: the old file survives any failure.</summary>
        private static bool WriteSafely(string path, string text)
        {
            string temp = path + ".tmp";
            using (var file = Godot.FileAccess.Open(temp, Godot.FileAccess.ModeFlags.Write))
            {
                if (file == null) return false;
                file.StoreString(text);
            }
            if (Godot.FileAccess.GetFileAsString(temp) != text) return false;
            return DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(temp), ProjectSettings.GlobalizePath(path)) == Error.Ok;
        }

        private const double InkSeconds = 0.45;
        private ColorRect ink;
        private bool travelling;
        private string pendingPath;
        private string currentPath;
        private bool sceneChanged;
        private const double LoadPause = 0.06;
        private bool inkInSmoke; // --ink: a smoke run crosses under the ink too (to check it)

        /// <summary>
        /// Goes to another screen behind a curtain (Shuimo): two gold-flecked panels close on red seals, the scene changes
        /// behind them, they part. A smoke run changes at once (<c>--ink</c> to check the curtain).
        /// </summary>
        public void GoTo(string scenePath)
        {
            if ((IsSmokeRun && !inkInSmoke) || ink == null)
            {
                GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scenePath);
                return;
            }
            if (travelling)
            {
                // closing: the old screen's last presses (a key held, a second click) are dropped; once the new screen
                // is loaded, its own request (a redirect from _Ready) is kept for when the curtain has parted — the last wins
                if (sceneChanged && scenePath != currentPath) pendingPath = scenePath;
                return;
            }
            travelling = true;
            sceneChanged = false;
            currentPath = scenePath;
            GetViewport().GuiReleaseFocus(); // no key or pad press reaches the old screen while the curtain closes
            var material = (ShaderMaterial)ink.Material;
            ink.Visible = true;
            ink.MouseFilter = Control.MouseFilterEnum.Stop; // no click lands mid-crossing
            var tween = CreateTween();
            tween.TweenMethod(Callable.From<float>(v => material.SetShaderParameter("progress", v)), 0f, 1f, InkSeconds);
            tween.TweenCallback(Callable.From(() =>
            {
                var error = GetTree().ChangeSceneToFile(scenePath);
                if (error != Error.Ok) GD.PushError($"[GameRoot] Cannot open {scenePath} ({error}).");
                sceneChanged = true;
            }));
            tween.TweenInterval(LoadPause); // the new screen settles before the curtain parts
            tween.TweenMethod(Callable.From<float>(v => material.SetShaderParameter("progress", v)), 1f, 0f, InkSeconds);
            tween.TweenCallback(Callable.From(() =>
            {
                ink.Visible = false;
                ink.MouseFilter = Control.MouseFilterEnum.Ignore;
                travelling = false;
                if (pendingPath is { } next)
                {
                    pendingPath = null;
                    GoTo(next);
                }
            }));
        }

        /// <summary>Holds the curtain at a given closure (screenshots: <c>CURTAIN=0.7</c>).</summary>
        public void HoldCurtain(float progress)
        {
            if (ink == null) return;
            ink.Visible = true;
            ((ShaderMaterial)ink.Material).SetShaderParameter("progress", progress);
        }

        /// <summary>A screen reached without a game (a direct launch, an erased slot) goes back to the title; true when it did.</summary>
        public bool RedirectWithoutSession(Node screen)
        {
            if (Session != null) return false;
            GoTo(TitleScreen.ScenePath);
            return true;
        }

        private GameSetup Setup(int? seed)
        {
            var setup = new GameSetup { Log = new GodotGameLog(), Content = content };
            return seed.HasValue ? setup with { Seed = seed.Value } : setup;
        }

        private void Start(GameSession session, int slot)
        {
            Stop();
            Session = session;
            CurrentSlot = slot;
            Chronicle = new Chronicle(session);
            session.Events.OnYearStarted += SaveAtYearStart;
            SessionChanged?.Invoke();
        }

        /// <summary>The game on screen ends (another begins, or its slot was erased): it no longer saves anywhere.</summary>
        private void Stop()
        {
            if (Session != null) Session.Events.OnYearStarted -= SaveAtYearStart;
            Session = null;
            CurrentSlot = 0;
        }

        private void SaveAtYearStart(int year) => Save();

        private static string ReadDataFile(string name)
        {
            string path = DataDirectory + name;
            return Godot.FileAccess.FileExists(path) ? Godot.FileAccess.GetFileAsString(path) : null;
        }
    }
}
