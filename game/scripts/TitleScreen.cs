using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The title screen (G6): three ironman save slots — continue a game, begin a new one (an optional world seed), or
    /// erase one (confirmed twice: a lineage is lost). It only binds <see cref="TitleView"/> and forwards to the
    /// <see cref="GameRoot"/>. A smoke run goes straight to the domain.
    /// </summary>
    public partial class TitleScreen : Control
    {
        public const string ScenePath = "res://scenes/TitleScreen.tscn";

        private GameRoot root;
        private VBoxContainer slots;
        private LineEdit seed;
        private Label status;
        private int eraseArmed; // the slot whose erasure awaits its confirmation (0: none)

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            slots = GetNode<VBoxContainer>("%Slots");
            seed = GetNode<LineEdit>("%Seed");
            status = GetNode<Label>("%Status");
            if (root.IsSmokeRun && root.SmokeStaysOnTitle)
            {
                Refresh();
                if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
                else GetTree().Quit();
                return;
            }
            if (root.IsSmokeRun)
            {
                GD.Print($"[Smoke] Title: {TitleView.Slots(root.ReadSlot).Count} slots.");
                Callable.From(() => GetTree().ChangeSceneToFile(ClanDomain.ScenePath)).CallDeferred();
                return;
            }
            Refresh();
        }

        private void Refresh()
        {
            foreach (var child in slots.GetChildren())
            {
                slots.RemoveChild(child);
                child.QueueFree();
            }
            foreach (var slot in TitleView.Slots(root.ReadSlot))
            {
                var row = new HBoxContainer();
                row.AddChild(new Label { Text = slot.Label, CustomMinimumSize = new Vector2(620, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart });
                var play = new Button { Text = "Continuer", Disabled = !slot.CanContinue };
                play.Pressed += () =>
                {
                    if (root.Continue(slot.Index)) GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
                    else status.Text = "Cette sauvegarde ne peut être lue.";
                };
                row.AddChild(play);
                var begin = new Button { Text = slot.IsEmpty ? "Nouvelle partie" : "Recommencer ici" };
                begin.Pressed += () => Begin(slot);
                row.AddChild(begin);
                if (!slot.IsEmpty)
                {
                    var erase = new Button { Text = eraseArmed == slot.Index ? "Confirmer l'effacement" : "Effacer" };
                    erase.Pressed += () => Erase(slot.Index);
                    row.AddChild(erase);
                }
                slots.AddChild(row);
            }
        }

        private void Begin(SlotLine slot)
        {
            if (!slot.IsEmpty && eraseArmed != -slot.Index) // a game already there: confirm first
            {
                eraseArmed = -slot.Index;
                status.Text = $"L'emplacement {slot.Index} contient une partie : elle sera perdue. Appuyez encore pour recommencer.";
                return;
            }
            int? worldSeed = int.TryParse(seed.Text, out int s) ? s : null;
            root.NewGame(slot.Index, worldSeed);
            GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
        }

        private void Erase(int index)
        {
            if (eraseArmed != index)
            {
                eraseArmed = index;
                status.Text = $"Effacer l'emplacement {index} ? La lignée sera perdue. Confirmez.";
            }
            else
            {
                eraseArmed = 0;
                root.Erase(index);
                status.Text = $"L'emplacement {index} est effacé.";
            }
            Refresh();
        }
    }
}
