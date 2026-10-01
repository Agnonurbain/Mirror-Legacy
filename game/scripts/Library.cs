using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The clan's library (G6): the arts the clan knows, their Qi and who practises them, and the market of knowledge —
    /// what each power would sell, at what price, and why not now. It only binds <see cref="LibraryView"/> and forwards
    /// the purchases to the session.
    /// </summary>
    public partial class Library : Control
    {
        public const string ScenePath = "res://scenes/Library.tscn";

        private GameRoot root;
        private VBoxContainer arts;
        private VBoxContainer market;
        private Label status;
        private VBoxContainer accord;                 // the accords tab (LORE.md §11.10, A)
        private TabContainer tabs;
        private (string Power, string TechniqueId, string Name)? negotiating;
        private readonly System.Collections.Generic.HashSet<int> chosen = new System.Collections.Generic.HashSet<int>();

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            arts = GetNode<VBoxContainer>("%Arts");
            market = GetNode<VBoxContainer>("%Market");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            // LIB_TAB=<0-1> opens a tab (screenshots of a smoke run)
            tabs = GetNode<TabContainer>("%Tabs");
            var accordTab = new ScrollContainer { Name = "Accord" };
            accord = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            accordTab.AddChild(accord);
            tabs.AddChild(accordTab);
            if (int.TryParse(OS.GetEnvironment("LIB_TAB"), out int tab)) tabs.CurrentTab = tab;
            if (tab == 2 && LibraryView.Market(root.Session).FirstOrDefault() is { } first) // screenshots: an accord under way
                negotiating = (first.Power, first.TechniqueId, first.Name);
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            ShowArts();
            ShowMarket();
            ShowAccord();
        }

        private System.Collections.Generic.IReadOnlyList<AccordCandidate> Candidates() =>
            AccordView.Candidates(root.Session.Accords, root.Session.Clan, root.Session.Techniques, root.Session.Resources,
                root.Session.SecretBook, negotiating.Value.Power, negotiating.Value.TechniqueId);

        /// <summary>The accord under way: what the clan could give, ticked or not, the worth against the price, and the seal.</summary>
        private void ShowAccord()
        {
            Clear(accord);
            if (negotiating == null)
            {
                Add(accord, "Choisissez « Négocier » sur un art du marché : une méthode se paie en savoir, en secrets, en bêtes, en Qi, en dettes ou en disciples.");
                return;
            }
            var (power, techniqueId, name) = negotiating.Value;
            Add(accord, $"Accord avec {power} pour « {name} ». Ce que le clan pourrait donner :");
            var candidates = Candidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = i;
                var box = new CheckBox { Text = $"{candidates[i].Label} — vaut {candidates[i].Worth}", ButtonPressed = chosen.Contains(i) };
                box.Toggled += on => { if (on) chosen.Add(index); else chosen.Remove(index); ShowAccord(); };
                accord.AddChild(box);
            }
            var terms = chosen.Where(i => i < candidates.Count).Select(i => candidates[i].Term).ToList();
            var summary = AccordView.Summary(root.Session.Accords, power, techniqueId, terms);
            Add(accord, $"Valeur offerte : {summary.Worth} sur {summary.Price}.{(summary.Refusal == null ? "" : $" ({summary.Refusal})")}");
            var seal = new Button { Text = "Conclure l'accord", Disabled = summary.Refusal != null, TooltipText = summary.Refusal ?? "" };
            seal.Pressed += () =>
            {
                string refusal = root.Session.Accords.Conclude(power, techniqueId, terms);
                status.Text = refusal == null ? $"Accord conclu : le clan obtient « {name} » de {power}." : $"Refusé : {refusal}.";
                if (refusal == null) { negotiating = null; chosen.Clear(); }
                Refresh();
            };
            accord.AddChild(seal);
        }

        private void ShowArts()
        {
            Clear(arts);
            string kind = null;
            foreach (var art in LibraryView.Library(root.Session))
            {
                if (art.Kind != kind) Add(arts, $"— {kind = art.Kind} —");
                string qi = art.Qi == null ? "" : $" · {art.Qi} : {art.QiInStore} portion{(art.QiInStore > 1 ? "s" : "")} en réserve";
                string who = art.Practitioners.Count == 0 ? "" : $" · pratiquée par {string.Join(", ", art.Practitioners)}";
                Add(arts, $"{art.Name} — grade {(art.Grade >= MirrorChronicles.Characters.TechniqueRules.MaxGrade ? "7+" : art.Grade.ToString())}, {art.Category}, {art.Element}, dès {art.FirstRealm}{qi}{who}");
            }
        }

        private void ShowMarket()
        {
            Clear(market);
            var offers = LibraryView.Market(root.Session);
            if (offers.Count == 0) Add(market, "Aucune puissance ne détient d'art que le clan ignore.");
            foreach (var offer in offers)
            {
                var line = new HBoxContainer();
                line.AddChild(new Label
                {
                    Text = $"{offer.Name} ({offer.Kind}, grade {offer.Grade}) — {offer.Power} — {offer.Price} pierres",
                    CustomMinimumSize = new Vector2(620, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
                var buy = new Button { Text = "Acheter", Disabled = offer.Refusal != null, TooltipText = offer.Refusal ?? "" };
                buy.Pressed += () =>
                {
                    bool bought = root.Session.Exchange.BuyTechnique(offer.Power, offer.TechniqueId);
                    status.Text = bought ? $"Le clan acquiert « {offer.Name} » auprès de {offer.Power}." : "L'achat n'a pu se faire.";
                    Refresh();
                };
                line.AddChild(buy);
                var negotiate = new Button { Text = "Négocier" };
                negotiate.Pressed += () =>
                {
                    negotiating = (offer.Power, offer.TechniqueId, offer.Name);
                    chosen.Clear();
                    ShowAccord();
                    tabs.CurrentTab = tabs.GetTabCount() - 1;
                };
                line.AddChild(negotiate);
                if (offer.Refusal != null) line.AddChild(new Label { Text = $"({offer.Refusal})" });
                market.AddChild(line);
            }
        }

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
            GD.Print($"[Smoke] Library: {LibraryView.Library(root.Session).Count} arts known, {LibraryView.Market(root.Session).Count} on the market.");
            if (root.SmokeEndsOnGenealogy) root.GoTo(Genealogy.ScenePath); // the tree checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
