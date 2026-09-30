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

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            arts = GetNode<VBoxContainer>("%Arts");
            market = GetNode<VBoxContainer>("%Market");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            // LIB_TAB=<0-1> opens a tab (screenshots of a smoke run)
            if (int.TryParse(OS.GetEnvironment("LIB_TAB"), out int tab)) GetNode<TabContainer>("%Tabs").CurrentTab = tab;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            ShowArts();
            ShowMarket();
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
