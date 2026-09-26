using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The world map screen (LORE.md §7): the map, and the powers of the place the player selects. It only binds
    /// <see cref="WorldMapView"/>; the clan's home is selected first.
    /// </summary>
    public partial class WorldMap : Control
    {
        public const string ScenePath = "res://scenes/WorldMap.tscn";

        private GameRoot root;
        private MapCanvas canvas;
        private Label placeName, placeFactions;
        private string selected;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            canvas = GetNode<MapCanvas>("%Canvas");
            placeName = GetNode<Label>("%PlaceName");
            placeFactions = GetNode<Label>("%PlaceFactions");
            GetNode<Button>("%Back").Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            canvas.PlaceSelected += Select;
            canvas.Resized += Refresh;

            selected = WorldMapView.Places(root.Session).FirstOrDefault(p => p.IsHome)?.Id;
            Refresh();

            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Select(string placeId)
        {
            selected = placeId;
            Refresh();
        }

        private void Refresh()
        {
            var session = root.Session;
            var places = WorldMapView.Places(session);
            canvas.Show(places, WorldMapView.Borders(session), selected);

            var place = places.FirstOrDefault(p => p.Id == selected);
            placeName.Text = place == null ? "" : place.IsHome ? $"{place.Name} — domaine du clan" : place.Name;
            var factions = place?.Factions ?? System.Array.Empty<MapFaction>();
            placeFactions.Text = factions.Count == 0
                ? "Aucune puissance connue ici."
                : string.Join("\n", factions.Select(f => $"{f.Name} ({f.Kind}) — {f.HighestRealm} — relation {f.Relation:+#;-#;0}"));

            var unplaced = WorldMapView.Unplaced(session);
            if (unplaced.Count > 0)
                placeFactions.Text += $"\n\nSans lieu connu : {string.Join(", ", unplaced.Select(f => f.Name))}";
        }

        private void RunSmoke()
        {
            GD.Print($"[Smoke] Map: {WorldMapView.Places(root.Session).Count} places, {WorldMapView.Borders(root.Session).Count} borders.");
            if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
