using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The mirror's screen (G6, L2b): its power and seeds, its interventions with their cost and why not now, the
    /// Talisman Seed, the Judgment (confirmed twice: a life is taken) and the deduction of fragments. It only binds
    /// <see cref="MirrorView"/> and forwards the choices to the session.
    /// </summary>
    public partial class MirrorScreen : Control
    {
        public const string ScenePath = "res://scenes/MirrorScreen.tscn";

        private GameRoot root;
        private Label power;
        private VBoxContainer interventions;
        private VBoxContainer seeds;
        private VBoxContainer judgment;
        private VBoxContainer deduction;
        private Label status;
        private readonly HashSet<string> chosenFragments = new HashSet<string>();
        private string judgmentArmed; // the member whose judgment awaits its confirmation

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            power = GetNode<Label>("%Power");
            interventions = GetNode<VBoxContainer>("%Interventions");
            seeds = GetNode<VBoxContainer>("%Seeds");
            judgment = GetNode<VBoxContainer>("%Judgment");
            deduction = GetNode<VBoxContainer>("%Deduction");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            // MIR_TAB=<0-3> opens a tab (screenshots of a smoke run)
            var tabs = GetNode<TabContainer>("%Tabs");
            if (int.TryParse(OS.GetEnvironment("MIR_TAB"), out int tab) && tab >= 0 && tab < tabs.GetTabCount()) tabs.CurrentTab = tab;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            var header = MirrorView.Header(root.Session);
            power.Text = $"Puissance {header.Power}/{header.MaxPower} · Graines de Sceau {header.Seeds}/{header.SeedCapacity}";
            var all = MirrorView.Interventions(root.Session);
            ShowInterventions(all);
            ShowSeeds();
            ShowJudgment(all.Single(i => i.Id == MirrorView.Judgment).Refusal);
            ShowDeduction();
        }

        private void ShowInterventions(IEnumerable<InterventionLine> all)
        {
            Clear(interventions);
            foreach (var line in all)
            {
                string text = $"{line.Name} — {line.Cost} de puissance — {line.Effect}{(line.Refusal == null ? "" : $" ({line.Refusal})")}";
                if (line.Id != MirrorView.Shield)
                {
                    Add(interventions, text); // the others are used from their own tab, or in battle
                    continue;
                }
                var row = new HBoxContainer();
                row.AddChild(Text(text));
                var use = new Button { Text = "Invoquer", Disabled = line.Refusal != null, TooltipText = line.Refusal ?? "" };
                use.Pressed += () =>
                {
                    status.Text = root.Session.Mirror.UseAncestralShield()
                        ? "Le Bouclier ancestral veille sur la prochaine percée." : "Le miroir n'a pu invoquer le bouclier.";
                    Refresh();
                };
                row.AddChild(use);
                interventions.AddChild(row);
            }
        }

        private void ShowSeeds()
        {
            Clear(seeds);
            var candidates = MirrorView.SeedCandidates(root.Session);
            if (candidates.Count == 0) Add(seeds, "Aucun mortel du clan n'attend de Graine de Sceau.");
            foreach (var candidate in candidates)
            {
                var row = new HBoxContainer();
                row.AddChild(Text($"{candidate.Name}, {candidate.Age} ans{(candidate.Refusal == null ? "" : $" ({candidate.Refusal})")}"));
                var plant = new Button { Text = "Planter la graine", Disabled = candidate.Refusal != null, TooltipText = candidate.Refusal ?? "" };
                plant.Pressed += () =>
                {
                    bool planted = root.Session.Mirror.GrantTalismanSeed(root.Session.Clan.FindById(candidate.Id));
                    status.Text = planted ? $"Une Graine de Sceau prend racine en {candidate.Name}." : "La graine n'a pu prendre racine.";
                    Refresh();
                };
                row.AddChild(plant);
                seeds.AddChild(row);
            }
        }

        private void ShowJudgment(string refusal)
        {
            Clear(judgment);
            if (refusal != null) Add(judgment, $"Le jugement est hors de portée : {refusal}.");
            foreach (var target in MirrorView.JudgmentTargets(root.Session))
            {
                var row = new HBoxContainer();
                row.AddChild(Text($"{target.Name} — {target.Realm}{(target.Origin == null ? "" : $" — {target.Origin}")}"));
                var strike = new Button { Text = judgmentArmed == target.Id ? "Confirmer le jugement" : "Juger", Disabled = refusal != null };
                strike.Pressed += () => Judge(target);
                row.AddChild(strike);
                judgment.AddChild(row);
            }
        }

        private void Judge(JudgmentTarget target)
        {
            if (judgmentArmed != target.Id)
            {
                judgmentArmed = target.Id;
                status.Text = $"Juger {target.Name} ? Sa vie sera prise. Confirmez.";
            }
            else
            {
                judgmentArmed = null;
                bool struck = root.Session.Mirror.UseMirrorJudgment(root.Session.Clan.FindById(target.Id));
                status.Text = struck ? $"Le jugement tombe sur {target.Name}." : "Le jugement n'a pu tomber.";
            }
            Refresh();
        }

        private void ShowDeduction()
        {
            Clear(deduction);
            var fragments = MirrorView.Fragments(root.Session);
            chosenFragments.IntersectWith(fragments.Select(f => f.Id)); // those spent are gone
            if (fragments.Count == 0)
            {
                Add(deduction, "Le miroir ne détient aucun fragment.");
                return;
            }
            foreach (var fragment in fragments)
            {
                var pick = new CheckBox
                {
                    Text = $"{fragment.Name} — {fragment.Element}, qualité {fragment.Quality}",
                    ButtonPressed = chosenFragments.Contains(fragment.Id)
                };
                pick.Toggled += on =>
                {
                    if (on) chosenFragments.Add(fragment.Id); else chosenFragments.Remove(fragment.Id);
                    Callable.From(Refresh).CallDeferred(); // not while the box is still handling its toggle
                };
                deduction.AddChild(pick);
            }
            var preview = MirrorView.DeductionPreview(root.Session, chosenFragments.ToList());
            var row = new HBoxContainer();
            row.AddChild(Text(preview.Refusal == null ? $"Coût : {preview.Cost} de puissance" : $"({preview.Refusal})"));
            var deduce = new Button { Text = "Déduire", Disabled = preview.Refusal != null };
            deduce.Pressed += () =>
            {
                var inputs = root.Session.Deduction.Fragments.Where(f => chosenFragments.Contains(f.ID)).ToList();
                var technique = root.Session.Deduction.AttemptDeduction(inputs);
                chosenFragments.Clear();
                status.Text = technique != null ? $"Le miroir déduit « {technique.Name} »." : "La déduction a échoué.";
                Refresh();
            };
            row.AddChild(deduce);
            deduction.AddChild(row);
        }

        private static Label Text(string text) =>
            new Label { Text = text, CustomMinimumSize = new Vector2(620, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart };

        private static void Add(Container box, string text) =>
            box.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart });

        private static void Clear(Node container)
        {
            foreach (var child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }

        private void RunSmoke()
        {
            var header = MirrorView.Header(root.Session);
            GD.Print($"[Smoke] Mirror: power {header.Power}, {MirrorView.SeedCandidates(root.Session).Count} seed candidates, {MirrorView.Fragments(root.Session).Count} fragments.");
            if (root.SmokeEndsOnBuildings) root.GoTo(BuildingsScreen.ScenePath); // the buildings check themselves
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
