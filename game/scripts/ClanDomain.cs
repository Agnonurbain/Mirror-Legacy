using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The clan domain screen. It only binds the engine-free view models (<see cref="ClanDomainView"/>,
    /// <see cref="Chronicle"/>) and forwards the player's choices to the session.
    /// </summary>
    public partial class ClanDomain : Control
    {
        private const int ChronicleLines = 40;
        private const int SmokeYears = 5;
        private const int PhasesPerYear = 4;
        private const float MemberLabelWidth = 220;
        private const float TaskSelectorWidth = 140;
        private const float MethodSelectorWidth = 330;

        private GameRoot root;
        private Chronicle boundChronicle;
        private Label clanName, year, phase, stones, mirror, generation, qi, storyTitle, storyText, chronicle, status;
        private Button nextPhase;
        private VBoxContainer roster, storyChoices;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            clanName = GetNode<Label>("%ClanName");
            year = GetNode<Label>("%Year");
            phase = GetNode<Label>("%Phase");
            stones = GetNode<Label>("%Stones");
            mirror = GetNode<Label>("%Mirror");
            generation = GetNode<Label>("%Generation");
            qi = GetNode<Label>("%Qi");
            storyTitle = GetNode<Label>("%StoryTitle");
            storyText = GetNode<Label>("%StoryText");
            chronicle = GetNode<Label>("%Chronicle");
            status = GetNode<Label>("%Status");
            nextPhase = GetNode<Button>("%NextPhase");
            roster = GetNode<VBoxContainer>("%Roster");
            storyChoices = GetNode<VBoxContainer>("%StoryChoices");

            nextPhase.Pressed += AdvancePhase;
            root.SessionChanged += Bind;
            Bind();

            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        public override void _ExitTree()
        {
            root.SessionChanged -= Bind;
            Unbind(); // the chronicle lives on the autoload and outlives this screen
        }

        /// <summary>Follows the current session's chronicle, leaving the previous one.</summary>
        private void Bind()
        {
            Unbind();
            boundChronicle = root.Chronicle;
            boundChronicle.OnEntryAdded += OnChronicleEntry;
            Refresh();
        }

        private void Unbind()
        {
            if (boundChronicle != null) boundChronicle.OnEntryAdded -= OnChronicleEntry;
            boundChronicle = null;
        }

        private void OnChronicleEntry(string entry) => ShowChronicle();

        private void AdvancePhase()
        {
            root.Session.AdvancePhase();
            Refresh();
        }

        private void AssignTask(string memberId, TaskType task)
        {
            var member = root.Session.Clan.FindById(memberId);
            if (member != null) root.Session.Tasks.AssignTask(member, task);
        }

        private void ChooseStoryOutcome(int choice)
        {
            root.Session.Story.ResolveChoice(choice);
            Refresh();
        }

        private void Refresh()
        {
            var session = root.Session;
            var header = ClanDomainView.Header(session);
            clanName.Text = $"Clan {session.Clan.ClanName}";
            year.Text = $"An {header.Year}";
            phase.Text = header.Phase;
            stones.Text = $"{header.SpiritStones} pierres spirituelles";
            mirror.Text = $"Miroir {header.MirrorPower}/100";
            generation.Text = $"Génération {header.Generation}";
            var stock = ClanDomainView.QiStock(session);
            qi.Text = stock.Count == 0 ? "Aucun Qi en réserve"
                : string.Join(" · ", stock.Select(line => $"{line.Name} ×{line.Portions}"));

            ShowRoster(ClanDomainView.Roster(session));
            ShowStory(session.Story.PendingEvent);
            ShowChronicle();

            bool over = session.Victory.IsOver;
            status.Text = session.Victory.GameWon ? "La lignée est devenue éternelle."
                : session.Victory.GameLost ? "La lignée s'est éteinte." : "";
            nextPhase.Disabled = over || session.Story.PendingEvent != null; // a story event waits for a choice
        }

        private void ShowRoster(IReadOnlyList<MemberRow> rows)
        {
            Clear(roster);
            foreach (var row in rows)
                roster.AddChild(BuildRow(row));
        }

        private Control BuildRow(MemberRow row)
        {
            var line = new HBoxContainer();
            line.AddChild(new Label
            {
                Text = MemberText(row),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(MemberLabelWidth, 0)
            });

            var tasks = Selector(TaskSelectorWidth);
            foreach (var task in row.AllowedTasks)
                tasks.AddItem(ClanDomainView.TaskLabel(task));
            int current = IndexOf(row.AllowedTasks, row.Task);
            if (current >= 0) tasks.Select(current);

            string memberId = row.Id;
            var allowed = row.AllowedTasks;
            tasks.ItemSelected += index => AssignTask(memberId, allowed[(int)index]);
            line.AddChild(tasks);
            line.AddChild(BuildMethodChoice(row));
            return line;
        }

        /// <summary>Who the member is: rank and temper, then (when they have them) foundation, abilities and retreat.</summary>
        private static string MemberText(MemberRow row)
        {
            var details = new[] { row.Foundation, row.Abilities, row.Retreat, row.HeartDemon }.Where(d => d != null);
            string text = $"{(row.IsPatriarch ? "★ " : "")}{row.Name}, {row.Age} ans — {row.Rank} — stabilité {row.Stability} — {row.Temperament}";
            return details.Any() ? $"{text}\n{string.Join(" · ", details)}" : text;
        }

        /// <summary>The method a member practises: a choice among those they may take up, or just its name.</summary>
        private Control BuildMethodChoice(MemberRow row)
        {
            if (row.Methods.Count == 0)
                return new Label { Text = row.Method, CustomMinimumSize = new Vector2(MethodSelectorWidth, 0), ClipText = true };

            var methods = Selector(MethodSelectorWidth);
            methods.TooltipText = row.Method;
            foreach (var choice in row.Methods)
                methods.AddItem(choice.Label);
            int current = -1;
            for (int i = 0; i < row.Methods.Count && current < 0; i++)
                if (row.Methods[i].Id == row.MethodId) current = i;
            if (current >= 0) methods.Select(current);
            else
            {
                methods.AddItem(row.Method); // the common breathing: nothing chosen yet
                methods.Select(methods.ItemCount - 1);
            }

            string memberId = row.Id;
            var choices = row.Methods;
            methods.ItemSelected += index =>
            {
                if (index < choices.Count) AssignMethod(memberId, choices[(int)index].Id);
            };
            return methods;
        }

        /// <summary>A drop-down of fixed width whose long entries end in an ellipsis instead of widening the row.</summary>
        private static OptionButton Selector(float width) => new OptionButton
        {
            CustomMinimumSize = new Vector2(width, 0),
            FitToLongestItem = false,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };

        private void AssignMethod(string memberId, string methodId)
        {
            var member = root.Session.Clan.FindById(memberId);
            if (member != null && root.Session.Techniques.AssignMethod(member, methodId)) Refresh();
        }

        private void ShowStory(StoryEventData evt)
        {
            storyTitle.Text = evt?.Name ?? "";
            storyText.Text = evt?.NarrativeText ?? "";
            Clear(storyChoices);
            if (evt == null) return;

            for (int i = 0; i < evt.Choices.Count; i++)
            {
                int choice = i;
                var button = new Button { Text = evt.Choices[i].Label };
                button.Pressed += () => ChooseStoryOutcome(choice);
                storyChoices.AddChild(button);
            }
        }

        private void ShowChronicle()
        {
            chronicle.Text = string.Join("\n", root.Chronicle.Entries.Reverse().Take(ChronicleLines)); // newest first
        }

        /// <summary>
        /// Headless check (<c>-- --smoke</c>): plays a few years through the same handlers as the player,
        /// cultivating whoever can and sending adult mortals to the mine, then quits.
        /// </summary>
        private void RunSmoke()
        {
            var session = root.Session;
            for (int y = 0; y < SmokeYears && !session.Victory.IsOver; y++)
            {
                foreach (var row in ClanDomainView.Roster(session))
                {
                    var task = row.AllowedTasks.Contains(TaskType.Cultivation) ? TaskType.Cultivation
                        : row.AllowedTasks.Contains(TaskType.Mine) ? TaskType.Mine : TaskType.None;
                    AssignTask(row.Id, task);
                }

                for (int p = 0; p < PhasesPerYear; p++)
                {
                    while (session.Story.PendingEvent != null) ChooseStoryOutcome(0);
                    AdvancePhase();
                }
            }

            GD.Print($"[Smoke] Year {session.Clock.Year}: {session.Clan.LivingMembers.Count} members, "
                + $"{roster.GetChildCount()} roster rows, {root.Chronicle.Entries.Count} chronicle entries.");

            if (root.ScreenshotPath != null) CaptureAndQuit(root.ScreenshotPath);
            else GetTree().Quit();
        }

        /// <summary>Lets the layout settle for a few frames, saves the window as PNG, then quits.</summary>
        private async void CaptureAndQuit(string path)
        {
            for (int i = 0; i < 3; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var error = GetViewport().GetTexture().GetImage().SavePng(path);
            if (error != Error.Ok) GD.PushError($"[Smoke] Cannot save the screenshot to {path}: {error}");
            GetTree().Quit();
        }

        private static int IndexOf(IReadOnlyList<TaskType> tasks, TaskType task)
        {
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i] == task) return i;
            return -1;
        }

        private static void Clear(Node container)
        {
            foreach (var child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
