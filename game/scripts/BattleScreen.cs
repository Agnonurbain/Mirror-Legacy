using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// A rival's challenge (G6): first the summons — who sends it, the rivals, the members who may answer — then the battle
    /// on the grid: choose an action, click a target, end the turn, let the fighters fight on their own, or spend the
    /// mirror's power on a pulse of Qi. The battle must be concluded before leaving. It only binds <see cref="BattleView"/>
    /// and forwards the choices to the session.
    /// </summary>
    public partial class BattleScreen : Control
    {
        public const string ScenePath = "res://scenes/BattleScreen.tscn";

        private GameRoot root;
        private Button back;
        private Control prep;
        private Label summons;
        private VBoxContainer candidates;
        private Control field;
        private BattleCanvas canvas;
        private VBoxContainer units;
        private VBoxContainer actions;
        private Button endTurn;
        private Button auto;
        private Button pulse;
        private Button conclude;
        private Label status;
        private readonly HashSet<string> chosen = new HashSet<string>();
        private string action = BattleView.Move;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            back = GetNode<Button>("%Back");
            prep = GetNode<Control>("%Prep");
            summons = GetNode<Label>("%Summons");
            candidates = GetNode<VBoxContainer>("%Candidates");
            field = GetNode<Control>("%Field");
            canvas = GetNode<BattleCanvas>("%Canvas");
            units = GetNode<VBoxContainer>("%Units");
            actions = GetNode<VBoxContainer>("%Actions");
            endTurn = GetNode<Button>("%EndTurn");
            auto = GetNode<Button>("%Auto");
            pulse = GetNode<Button>("%Pulse");
            conclude = GetNode<Button>("%Conclude");
            status = GetNode<Label>("%Status");

            back.Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            GetNode<Button>("%Accept").Pressed += Accept;
            GetNode<Button>("%Decline").Pressed += () => Answer(root.Session.Challenges.Decline(), "Le clan se dérobe au défi.");
            endTurn.Pressed += () => { Battle?.EndTurn(); Refresh(); };
            auto.Pressed += () => { Battle?.AutoPlay(); Refresh(); };
            pulse.Pressed += Pulse;
            conclude.Pressed += Conclude;
            canvas.CellClicked += Strike;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private MirrorChronicles.Combat.Battle Battle => root.Session.Challenges.Current;

        private System.Func<string, MirrorChronicles.Data.TechniqueData> Find => root.Session.Techniques.Find;

        private void Refresh()
        {
            bool fighting = Battle != null;
            prep.Visible = !fighting;
            field.Visible = fighting;
            back.Disabled = fighting; // a battle is fought to its end
            if (fighting) ShowBattle(); else ShowSummons();
        }

        private void ShowSummons()
        {
            Clear(candidates);
            var pending = BattleView.Pending(root.Session);
            GetNode<Button>("%Accept").Disabled = pending == null;
            GetNode<Button>("%Decline").Disabled = pending == null;
            if (pending == null)
            {
                summons.Text = "Aucun défi n'attend le clan.";
                return;
            }
            summons.Text = $"{pending.Faction} défie le clan{(pending.ToTheDeath ? " À MORT — qui tombe meurt, et fuir coûte cher" : " dans les règles")} : "
                + $"{pending.Rivals} rival(aux) de rang {pending.Rank}. "
                + $"Choisissez jusqu'à {pending.MaxFighters} combattant(s).";
            chosen.IntersectWith(pending.Candidates.Where(c => c.Refusal == null).Select(c => c.Id));
            foreach (var candidate in pending.Candidates)
            {
                var pick = new CheckBox
                {
                    Text = $"{candidate.Name} — {candidate.Rank}{(candidate.Refusal == null ? "" : $" ({candidate.Refusal})")}",
                    Disabled = candidate.Refusal != null,
                    ButtonPressed = chosen.Contains(candidate.Id)
                };
                pick.Toggled += on => { if (on) chosen.Add(candidate.Id); else chosen.Remove(candidate.Id); };
                candidates.AddChild(pick);
            }
        }

        private void Accept()
        {
            var refusal = root.Session.Challenges.Accept(chosen.ToList());
            status.Text = refusal == null ? "Le défi est relevé : la bataille commence." : $"Impossible : {refusal}.";
            if (refusal == null) chosen.Clear();
            action = BattleView.Move;
            Refresh();
        }

        private void Conclude()
        {
            string outcome = Battle == null ? null : BattleView.Outcome(Battle);
            Answer(root.Session.Challenges.Conclude(), $"Le défi est réglé : {outcome?.ToLowerInvariant()}.");
        }

        private void Answer(string refusal, string done)
        {
            status.Text = refusal == null ? done : $"Impossible : {refusal}.";
            Refresh();
        }

        private void ShowBattle()
        {
            var battle = Battle;
            var open = BattleView.Actions(battle, Find);
            if (open.All(a => a.Id != action)) action = BattleView.Move;
            var current = open.FirstOrDefault(a => a.Id == action);
            canvas.Show(BattleView.Cells(battle), BattleView.Units(battle), current?.Targets ?? new List<(int, int)>());

            Clear(units);
            foreach (var unit in BattleView.Units(battle))
                units.AddChild(new Label
                {
                    Text = $"{(unit.IsCurrent ? "▶ " : "")}{(unit.IsAlly ? "Clan" : "Rival")} — {unit.Name} ({unit.Rank}) : "
                        + $"vitalité {unit.Vitality}/{unit.MaxVitality}, Qi {unit.Qi}/{unit.MaxQi}{(unit.Status == null ? "" : $" — {unit.Status}")}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });

            Clear(actions);
            foreach (var line in open)
            {
                var pick = new Button
                {
                    Text = $"{(line.Id == action ? "● " : "")}{line.Label}{(line.QiCost > 0 ? $" ({line.QiCost} Qi)" : "")}",
                    Disabled = line.Targets.Count == 0
                };
                pick.Pressed += () => { action = line.Id; Refresh(); };
                actions.AddChild(pick);
            }

            bool myTurn = open.Count > 0;
            endTurn.Disabled = !myTurn;
            auto.Disabled = !myTurn;
            var pulseRefusal = MirrorView.Interventions(root.Session).Single(i => i.Id == MirrorView.Pulse).Cost > root.Session.Mirror.MirrorPower
                ? "puissance insuffisante" : null;
            pulse.Disabled = !myTurn || pulseRefusal != null;
            pulse.TooltipText = pulseRefusal ?? "";
            conclude.Visible = battle.IsOver;
            if (battle.IsOver) status.Text = $"{BattleView.Outcome(battle)}. Concluez le défi.";
        }

        private void Strike(int x, int y)
        {
            var battle = Battle;
            if (battle == null) return;
            if (!BattleView.Perform(battle, action, x, y, Find)) status.Text = "Cette case n'est pas une cible.";
            Refresh();
        }

        private void Pulse()
        {
            var battle = Battle;
            if (battle == null) return;
            status.Text = root.Session.Mirror.UseQiPulse(battle.CurrentUnit)
                ? "Le miroir envoie une pulsation de Qi." : "Le miroir n'a pu envoyer de pulsation.";
            Refresh();
        }

        private static void Clear(Node container)
        {
            foreach (var child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }

        /// <summary>The smoke run answers a challenge (issued if none awaits), lets the fighters fight, and concludes.</summary>
        private void RunSmoke()
        {
            var session = root.Session;
            if (session.Challenges.Pending == null && session.Challenges.Current == null)
                session.Challenges.Issue(session.Factions.Factions.FirstOrDefault());
            var pending = BattleView.Pending(session);
            if (pending != null)
                session.Challenges.Accept(pending.Candidates.Where(c => c.Refusal == null).Take(pending.MaxFighters).Select(c => c.Id).ToList());
            Refresh();
            if (root.ScreenshotPath != null)
            {
                Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
                return;
            }
            Battle?.AutoPlay();
            string outcome = Battle == null ? "none" : BattleView.Outcome(Battle);
            session.Challenges.Conclude();
            GD.Print($"[Smoke] Battle: challenge answered, outcome {outcome}.");
            GetTree().Quit();
        }
    }
}
