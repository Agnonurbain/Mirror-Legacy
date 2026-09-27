using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>The clan's family tree screen (G6): it only binds <see cref="GenealogyView"/> to the canvas.</summary>
    public partial class Genealogy : Control
    {
        public const string ScenePath = "res://scenes/Genealogy.tscn";

        private GameRoot root;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            GetNode<Button>("%Back").Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            GetNode<GenealogyCanvas>("%Tree").Show(GenealogyView.Tree(root.Session));
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void RunSmoke()
        {
            GD.Print($"[Smoke] Genealogy: {GenealogyView.Tree(root.Session).Count} members recorded.");
            if (root.SmokeEndsOnDiplomacy) GetTree().ChangeSceneToFile(Diplomacy.ScenePath); // diplomacy checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
