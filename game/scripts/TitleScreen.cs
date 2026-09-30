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

        private enum Pending { None, Erase, BeginOver }

        private GameRoot root;
        private VBoxContainer slots;
        private LineEdit seed;
        private Label status;
        private Pending pending;
        private int pendingSlot; // the slot whose erasure, or new game over a lineage, awaits its confirmation

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            slots = GetNode<VBoxContainer>("%Slots");
            seed = GetNode<LineEdit>("%Seed");
            status = GetNode<Label>("%Status");
            seed.TextChanged += _ => Disarm();
            if (root.IsSmokeRun && root.SmokeStaysOnTitle)
            {
                Refresh();
                if (float.TryParse(OS.GetEnvironment("CURTAIN"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float closure)) root.HoldCurtain(closure);
                if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
                else GetTree().Quit();
                return;
            }
            if (root.IsSmokeRun)
            {
                GD.Print($"[Smoke] Title: {TitleView.Slots(root.ReadSlot).Count} slots.");
                Callable.From(() => root.GoTo(ClanDomain.ScenePath)).CallDeferred();
                return;
            }
            Refresh();
        }

        private bool Armed(Pending what, int slot) => pending == what && pendingSlot == slot;

        private void Arm(Pending what, int slot, string warning)
        {
            (pending, pendingSlot) = (what, slot);
            status.Text = warning;
            Refresh();
        }

        private void Disarm()
        {
            if (pending == Pending.None) return;
            (pending, pendingSlot) = (Pending.None, 0);
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
                play.Pressed += () => Continue(slot.Index);
                row.AddChild(play);
                var begin = new Button
                {
                    Text = slot.IsEmpty ? "Nouvelle partie" : Armed(Pending.BeginOver, slot.Index) ? "Confirmer : recommencer ici" : "Recommencer ici"
                };
                begin.Pressed += () => Begin(slot);
                row.AddChild(begin);
                if (!slot.IsEmpty)
                {
                    var erase = new Button { Text = Armed(Pending.Erase, slot.Index) ? "Confirmer l'effacement" : "Effacer" };
                    erase.Pressed += () => Erase(slot.Index);
                    row.AddChild(erase);
                }
                slots.AddChild(row);
            }
        }

        private void Continue(int index)
        {
            Disarm();
            string refusal = root.Continue(index);
            if (refusal == null) root.GoTo(ClanDomain.ScenePath);
            else status.Text = $"Impossible : {refusal}.";
        }

        private void Begin(SlotLine slot)
        {
            if (!slot.IsEmpty && !Armed(Pending.BeginOver, slot.Index)) // a lineage is there (or an unreadable file): confirm first
            {
                Arm(Pending.BeginOver, slot.Index, $"L'emplacement {slot.Index} n'est pas vide : ce qu'il contient sera perdu. Confirmez.");
                return;
            }
            int? worldSeed = null;
            if (!string.IsNullOrWhiteSpace(seed.Text))
            {
                if (!int.TryParse(seed.Text.Trim(), out int parsed))
                {
                    status.Text = "La graine doit être un nombre entier (ou rien, pour le hasard).";
                    return;
                }
                worldSeed = parsed;
            }
            Disarm();
            if (!root.NewGame(slot.Index, worldSeed)) status.Text = "La partie a commencé, mais n'a pu être sauvegardée.";
            root.GoTo(ClanDomain.ScenePath);
        }

        private void Erase(int index)
        {
            if (!Armed(Pending.Erase, index))
            {
                Arm(Pending.Erase, index, $"Effacer l'emplacement {index} ? La lignée sera perdue. Confirmez.");
                return;
            }
            string refusal = root.Erase(index);
            (pending, pendingSlot) = (Pending.None, 0);
            status.Text = refusal == null ? $"L'emplacement {index} est effacé." : $"Impossible : {refusal}.";
            Refresh();
        }
    }
}
