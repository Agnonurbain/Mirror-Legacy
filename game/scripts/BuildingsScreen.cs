using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The clan's buildings (G6): each with its level, what it gives now and at the next level, and the cost of raising
    /// it — or why not. It only binds <see cref="BuildingsView"/> and forwards the upgrades to the session.
    /// </summary>
    public partial class BuildingsScreen : Control
    {
        public const string ScenePath = "res://scenes/BuildingsScreen.tscn";

        private GameRoot root;
        private Label stones;
        private VBoxContainer list;
        private Label status;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            stones = GetNode<Label>("%Stones");
            list = GetNode<VBoxContainer>("%List");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            stones.Text = $"{root.Session.Resources.SpiritStones} pierres spirituelles";
            foreach (var child in list.GetChildren())
            {
                list.RemoveChild(child);
                child.QueueFree();
            }
            foreach (var building in BuildingsView.Buildings(root.Session))
            {
                var row = new HBoxContainer();
                var text = new VBoxContainer { CustomMinimumSize = new Vector2(640, 0) };
                text.AddChild(new Label { Text = $"{building.Name} — niveau {building.Level}/{building.MaxLevel}" });
                text.AddChild(Detail(building.Effect == null ? "Pas encore bâti." : $"Aujourd'hui : {building.Effect}."));
                if (building.NextEffect != null) text.AddChild(Detail($"Au niveau {building.Level + 1} : {building.NextEffect}."));
                row.AddChild(text);
                if (building.Cost is int cost)
                {
                    var raise = new Button
                    {
                        Text = $"Élever ({cost} pierres)", Disabled = building.Refusal != null, TooltipText = building.Refusal ?? "",
                        SizeFlagsVertical = SizeFlags.ShrinkCenter
                    };
                    raise.Pressed += () =>
                    {
                        status.Text = root.Session.Buildings.Upgrade(building.Type)
                            ? $"{building.Name} s'élève au niveau {building.Level + 1}." : "La construction n'a pu se faire.";
                        Refresh();
                    };
                    row.AddChild(raise);
                }
                if (building.Refusal != null)
                    row.AddChild(new Label
                    {
                        Text = $"({building.Refusal})", AutowrapMode = TextServer.AutowrapMode.WordSmart,
                        SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter
                    });
                list.AddChild(row);
            }
        }

        private static Label Detail(string text) =>
            new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.75f) };

        private void RunSmoke()
        {
            GD.Print($"[Smoke] Buildings: {BuildingsView.Buildings(root.Session).Count} buildings, {root.Session.Resources.SpiritStones} stones.");
            if (root.SmokeEndsOnBattle) root.GoTo(BattleScreen.ScenePath); // the battle checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
