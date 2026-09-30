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
        public const string ScenePath = "res://scenes/ClanDomain.tscn";

        private const int ChronicleLines = 40;
        private const int SmokeYears = 5;
        private const int PhasesPerYear = 4;
        private const float MemberLabelWidth = 220;
        private const float TaskSelectorWidth = 140;
        private const float MethodSelectorWidth = 330;

        private GameRoot root;
        private Chronicle boundChronicle;
        private Label clanName, year, phase, stones, mirror, generation, qi, ritual, storyTitle, storyText, chronicle, status;
        private Button nextPhase;
        private VBoxContainer roster, storyChoices;
        private Button foundSect;
        private AcceptDialog ending; // the dynastic ending told on screen, if any

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            clanName = GetNode<Label>("%ClanName");
            year = GetNode<Label>("%Year");
            phase = GetNode<Label>("%Phase");
            stones = GetNode<Label>("%Stones");
            mirror = GetNode<Label>("%Mirror");
            generation = GetNode<Label>("%Generation");
            qi = GetNode<Label>("%Qi");
            ritual = GetNode<Label>("%Ritual");
            storyTitle = GetNode<Label>("%StoryTitle");
            storyText = GetNode<Label>("%StoryText");
            chronicle = GetNode<Label>("%Chronicle");
            status = GetNode<Label>("%Status");
            nextPhase = GetNode<Button>("%NextPhase");
            roster = GetNode<VBoxContainer>("%Roster");
            storyChoices = GetNode<VBoxContainer>("%StoryChoices");

            nextPhase.Pressed += AdvancePhase;
            GetNode<Button>("%OpenMap").Pressed += () => root.GoTo(WorldMap.ScenePath);
            GetNode<Button>("%OpenOperations").Pressed += () => root.GoTo(Operations.ScenePath);
            GetNode<Button>("%OpenMirror").Pressed += () => root.GoTo(MirrorScreen.ScenePath);
            GetNode<Button>("%OpenBuildings").Pressed += () => root.GoTo(BuildingsScreen.ScenePath);
            GetNode<Button>("%OpenBattle").Pressed += () => root.GoTo(BattleScreen.ScenePath);
            GetNode<Button>("%OpenTitle").Pressed += () =>
            {
                root.Save(); // the year's work kept
                root.GoTo(TitleScreen.ScenePath);
            };
            GetNode<Button>("%OpenLibrary").Pressed += () => root.GoTo(Library.ScenePath);
            GetNode<Button>("%OpenDiplomacy").Pressed += () => root.GoTo(Diplomacy.ScenePath);
            GetNode<Button>("%OpenGenealogy").Pressed += () => root.GoTo(Genealogy.ScenePath);
            var nav = GetNode<Button>("%OpenGenealogy").GetParent();
            var annals = new Button { Text = "Annales" };
            annals.Pressed += ShowAnnals;
            nav.AddChild(annals);
            foundSect = new Button { Text = "Fonder la secte" };
            foundSect.Pressed += FoundSect;
            nav.AddChild(foundSect);
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
            var upkeep = ClanDomainView.Upkeep(session);
            stones.Text = $"{header.SpiritStones} pierres spirituelles (entretien {upkeep.Yearly}/an)";
            stones.TooltipText = upkeep.Warning ?? "";
            if (upkeep.Warning != null) stones.Text += " — misère";
            mirror.Text = $"Miroir {header.MirrorPower}/100";
            generation.Text = $"Génération {header.Generation}";
            GetNode<Button>("%OpenBattle").Visible = session.Challenges.Pending != null || session.Challenges.Current != null;
            var stock = ClanDomainView.QiStock(session);
            qi.Text = stock.Count == 0 ? "Aucun Qi en réserve"
                : string.Join(" · ", stock.Select(line => $"{line.Name} ×{line.Portions}"));
            ritual.Text = ClanDomainView.RitualLine(session);

            ShowRoster(ClanDomainView.Roster(session));
            ShowStory(session.Story.PendingEvent);
            ShowChronicle();

            string sectRefusal = session.Sect.FoundingRefusal();
            foundSect.Visible = !session.Sect.Founded;
            foundSect.Disabled = sectRefusal != null;
            foundSect.TooltipText = sectRefusal ?? "Les cultivateurs aux pics, les mortels à la ville (La Double Maison).";

            bool over = session.Victory.IsOver;
            status.Text = EndingView.Defeat(session);
            TellNextEnding();
            OfferFromAPatron();
            nextPhase.Disabled = over || session.Story.PendingEvent != null; // a story event waits for a choice
        }

        private void FoundSect()
        {
            status.Text = root.Session.Sect.Found() ?? "Le clan fonde sa secte : les pics en haut, la ville en bas.";
            Refresh();
        }

        /// <summary>A dynastic ending waits to be told: its story, then the game goes on.</summary>
        private void TellNextEnding()
        {
            if (ending != null || root.PendingEndings.Count == 0) return;
            var screen = root.PendingEndings.Dequeue();
            ending = new AcceptDialog
            {
                Title = screen.Title,
                DialogText = $"{screen.Byline}\n\n{screen.Story}",
                OkButtonText = screen.Continue,
                DialogAutowrap = true,
                MinSize = new Vector2I(640, 0)
            };
            ending.Confirmed += EndingClosed;
            ending.Canceled += EndingClosed;
            AddChild(ending);
            ending.PopupCentered();
        }

        private ConfirmationDialog patronOffer; // a patron's offer awaiting the player's answer

        /// <summary>A patron offers an ascent method (LORE.md §11.10): the player sees the offer, never the design.</summary>
        private void OfferFromAPatron()
        {
            var offer = root.Session.Sponsorships.Pending;
            if (offer == null || patronOffer != null || ending != null) return;
            string method = root.Session.Context.Content.Techniques.FirstOrDefault(t => t.ID == offer.TechniqueId)?.Name ?? offer.TechniqueId;
            patronOffer = new ConfirmationDialog
            {
                Title = $"Une offre de {offer.Power}",
                DialogText = $"{offer.Power} offre au clan le {method}, une méthode qui mène au Manoir Pourpre. En échange, le clan lui devra une faveur. Nul ne donne un tel trésor sans raison.",
                OkButtonText = "Accepter",
                CancelButtonText = "Refuser",
                DialogAutowrap = true,
                MinSize = new Vector2I(640, 0)
            };
            patronOffer.Confirmed += () => { status.Text = root.Session.Sponsorships.Accept() ?? $"Le clan accepte le don de {offer.Power}."; PatronOfferClosed(); };
            patronOffer.Canceled += () => { root.Session.Sponsorships.Refuse(); status.Text = $"Le clan décline l'offre de {offer.Power}."; PatronOfferClosed(); };
            AddChild(patronOffer);
            patronOffer.PopupCentered();
        }

        private void PatronOfferClosed()
        {
            patronOffer?.QueueFree();
            patronOffer = null;
            Refresh();
        }

        private void EndingClosed()
        {
            ending?.QueueFree();
            ending = null;
            TellNextEnding();
        }

        private void ShowAnnals()
        {
            var lines = AnnalsView.Lines(root.Session);
            var dialog = new AcceptDialog
            {
                Title = "Annales du clan",
                DialogText = lines.Count == 0 ? "Aucun jalon encore : l'histoire du clan reste à écrire." : string.Join("\n", lines),
                DialogAutowrap = true,
                MinSize = new Vector2I(720, 0)
            };
            dialog.Confirmed += dialog.QueueFree;
            dialog.Canceled += dialog.QueueFree;
            AddChild(dialog);
            dialog.PopupCentered();
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
            if (PortraitView.For(root.Session, row.Id) is { } portrait) line.AddChild(new PortraitIcon(portrait));
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

        /// <summary>Who the member is: rank and temper, then (when they have them) standing, foundation, abilities and retreat.</summary>
        private static string MemberText(MemberRow row)
        {
            var details = new[] { row.Position, row.Foundation, row.Abilities, row.Retreat, row.HeartDemon }.Where(d => d != null);
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

            if (root.SmokeEndsOnMap) root.GoTo(WorldMap.ScenePath); // the map checks itself, then quits
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
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
