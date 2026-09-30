using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The clan's family tree screen (G6): the tree, and beside it the marriages of the line (2026-09-29) — each unwed
    /// cultivator, whom the clan may wed them to, and the search abroad for a cultivator spouse. It only binds
    /// <see cref="GenealogyView"/> and <see cref="OperationsView.Unwed"/>, and forwards the choices to the session.
    /// </summary>
    public partial class Genealogy : Control
    {
        public const string ScenePath = "res://scenes/Genealogy.tscn";

        private GameRoot root;
        private VBoxContainer lineage;
        private Label status;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            lineage = GetNode<VBoxContainer>("%Lineage");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            GetNode<GenealogyCanvas>("%Tree").Show(GenealogyView.Tree(root.Session));
            ShowLineage();
        }

        private void ShowLineage()
        {
            foreach (var child in lineage.GetChildren())
            {
                lineage.RemoveChild(child);
                child.QueueFree();
            }
            lineage.AddChild(new Label { Text = "Mariages de la lignée" });
            var unwed = OperationsView.Unwed(root.Session);
            if (unwed.Count == 0) lineage.AddChild(Text("Aucun cultivateur du clan n'attend d'être uni."));
            foreach (var line in unwed)
            {
                lineage.AddChild(Text($"{line.Name} — {line.Rank}"));
                var row = new HBoxContainer();
                if (line.Partners.Count > 0)
                {
                    var partners = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                    foreach (var partner in line.Partners) partners.AddItem($"{partner.Name} ({partner.Rank})");
                    partners.Select(0); // the closest match, until another is chosen
                    row.AddChild(partners);
                    var wed = new Button { Text = "Unir" };
                    wed.Pressed += () =>
                    {
                        var partner = line.Partners[partners.Selected];
                        string refusal = root.Session.Marriages.Arrange(line.Id, partner.Id);
                        status.Text = refusal == null ? $"Le clan unit {line.Name} et {partner.Name}." : $"Impossible : {refusal}.";
                        Refresh();
                    };
                    row.AddChild(wed);
                }
                var seek = new Button
                {
                    Text = $"Chercher au dehors ({line.SeekCost} pierres)", Disabled = line.SeekRefusal != null, TooltipText = line.SeekRefusal ?? ""
                };
                seek.Pressed += () =>
                {
                    string refusal = root.Session.Marriages.SeekCultivatorSpouse(line.Id);
                    status.Text = refusal == null ? $"Un cultivateur errant épouse {line.Name}." : $"Pas cette fois : {refusal}.";
                    Refresh();
                };
                row.AddChild(seek);
                lineage.AddChild(row);
            }
        }

        private static Label Text(string text) => new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };

        private void RunSmoke()
        {
            GD.Print($"[Smoke] Genealogy: {GenealogyView.Tree(root.Session).Count} members recorded, {OperationsView.Unwed(root.Session).Count} unwed cultivators.");
            if (root.SmokeEndsOnDiplomacy) GetTree().ChangeSceneToFile(Diplomacy.ScenePath); // diplomacy checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
