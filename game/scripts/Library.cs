using System.Linq;
using Godot;
using MirrorChronicles.Data;
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
        private VBoxContainer lineages;               // the lineages of the world, as the clan knows them (2026-10-01)
        private VBoxContainer goldenCore;             // the Golden Core's actions (L4e, G6, 2026-10-03)
        private VBoxContainer armoury;                // the clan's artifacts, the forge, the powers' deals (L4f, 2026-10-03)
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
            // LIB_TAB=<0-5> opens a tab (screenshots of a smoke run)
            tabs = GetNode<TabContainer>("%Tabs");
            var accordTab = new ScrollContainer { Name = "Accord" };
            accord = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            accordTab.AddChild(accord);
            tabs.AddChild(accordTab);
            var lineagesTab = new ScrollContainer { Name = "Lignées" };
            lineages = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            lineagesTab.AddChild(lineages);
            tabs.AddChild(lineagesTab);
            var coreTab = new ScrollContainer { Name = "Noyau d'Or" };
            goldenCore = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            coreTab.AddChild(goldenCore);
            tabs.AddChild(coreTab);
            var armouryTab = new ScrollContainer { Name = "Armurerie" };
            armoury = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            armouryTab.AddChild(armoury);
            tabs.AddChild(armouryTab);
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
            ShowLineages();
            ShowArmoury();
            ShowGoldenCore();
        }

        private static readonly GoldenCoreAction[] Perilous = { GoldenCoreAction.Forge, GoldenCoreAction.Claim, GoldenCoreAction.Transmute };

        /// <summary>What each member may do toward and at the Golden Core, its odds or price, or why not; the perilous asked twice.</summary>
        private void ShowGoldenCore()
        {
            Clear(goldenCore);
            var options = GoldenCoreView.Actions(root.Session);
            if (options.Count == 0) Add(goldenCore, "Aucun membre n'est aux portes du Noyau d'Or : il faut une Grande Perfection du Manoir Pourpre, ses cinq capacités et l'XP du royaume.");
            foreach (var option in options)
                AddButton(goldenCore, option.Label, option.Refusal, () =>
                {
                    if (!Perilous.Contains(option.Kind)) { Act(GoldenCoreView.Perform(root.Session, option), "C'est fait."); return; }
                    var confirm = new ConfirmationDialog
                    {
                        Title = "Le Noyau d'Or",
                        DialogText = $"{option.Label}\n\nUn échec fait naître un Démon d'Essence Métallique : le membre est perdu. Un Manoir Pourpre au sommet est précieux.",
                        OkButtonText = "Tenter",
                        CancelButtonText = "Attendre",
                        DialogAutowrap = true,
                        MinSize = new Vector2I(640, 0)
                    };
                    confirm.Confirmed += () => { Act(GoldenCoreView.Perform(root.Session, option), "Le Ciel répond : c'est accompli."); confirm.QueueFree(); };
                    confirm.Canceled += () => confirm.QueueFree();
                    AddChild(confirm);
                    confirm.PopupCentered();
                });
        }

        private void Act(string outcome, string done)
        {
            status.Text = outcome == null ? done : $"Impossible : {outcome}.";
            Refresh();
        }

        private void AddButton(VBoxContainer box, string text, string refusal, System.Action pressed)
        {
            var button = new Button { Text = text, Disabled = refusal != null, TooltipText = refusal ?? "" };
            button.Pressed += pressed;
            box.AddChild(button);
        }

        /// <summary>The clan's artifacts — borne, in store, lent —, the forge's works, the powers' deals, the bonds to a treasure (L4f).</summary>
        private void ShowArmoury()
        {
            Clear(armoury);
            var s = root.Session;
            Add(armoury, "— Les artefacts du clan —");
            var rows = ArtifactView.Rows(s);
            if (rows.Count == 0) Add(armoury, "Le clan n'a aucun artefact.");
            var buyer = s.Factions.Factions.Where(f => f.Kind == FactionKind.Sect || f.Kind == FactionKind.Gate)
                .OrderByDescending(f => f.RelationWithPlayer).FirstOrDefault();
            foreach (var row in rows)
            {
                var line = new HBoxContainer();
                line.AddChild(new Label { Text = row.Label, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill });
                if (row.EntrustTo != null)
                {
                    var entrust = new Button { Text = row.EntrustLabel };
                    string id = row.Id, to = row.EntrustTo;
                    entrust.Pressed += () => Act(s.Artifacts.Equip(s.Clan.FindById(to), id), "L'artefact est confié.");
                    line.AddChild(entrust);
                }
                if (row.Owned && buyer != null && s.Artifacts.Armoury.Any(a => a.Id == row.Id))
                {
                    var sell = new Button { Text = $"Vendre à {buyer.Name}" };
                    string id = row.Id;
                    sell.Pressed += () => Act(s.ArtifactTrade.Sell(id, buyer.Name), "L'artefact est vendu.");
                    line.AddChild(sell);
                }
                armoury.AddChild(line);
            }
            Add(armoury, "— La forge —");
            var offers = ArtifactView.ForgeOffers(s);
            if (offers.Count == 0) Add(armoury, "Aucun forgeron libre.");
            foreach (var offer in offers)
                AddButton(armoury, offer.Label, offer.Refusal, () => Act(s.Forge.Make(offer.FormId, offer.Rank, offer.SmithId), "La forge livre son œuvre."));
            Add(armoury, "— Les puissances —");
            foreach (var deal in ArtifactView.PowerOffers(s))
                AddButton(armoury, deal.Label, null, () => Act(deal.Kind switch
                {
                    ArtifactDeal.Borrow => s.ArtifactTrade.Borrow(deal.Power),
                    ArtifactDeal.Commission => s.ArtifactTrade.Commission(deal.Power, s.Context.Content.ArtifactForms[0].Id,
                        s.Factions.GetFactionByName(deal.Power).HighestRealm > CultivationRealm.PurpleMansion ? CultivationRealm.PurpleMansion : s.Factions.GetFactionByName(deal.Power).HighestRealm),
                    _ => s.ArtifactTrade.StealFrom(deal.Power, deal.TeamIds),
                }, "C'est fait."));
            var binds = ArtifactView.BindOffers(s);
            if (binds.Count > 0) Add(armoury, "— Le lien à un trésor —");
            foreach (var bind in binds)
                AddButton(armoury, bind.Label, null, () => Act(s.Finds.Bind(bind.MemberId, bind.ArtifactId), "Le lien est scellé."));
        }

        /// <summary>Each lineage as the clan knows it: held, free, broken, a race, a holder reborn; the mirror for the hidden.</summary>
        private void ShowLineages()
        {
            Clear(lineages);
            var contenders = LineagesView.Contenders(root.Session);
            if (contenders.Count > 0) Add(lineages, "— Les concurrents dans les courses ouvertes —");
            foreach (var contender in contenders)
            {
                var sabotage = new Button { Text = contender.Label, Disabled = contender.TeamIds.Count == 0 };
                sabotage.Pressed += () =>
                {
                    string outcome = root.Session.WorldFruitions.Sabotage(contender.Power, contender.TeamIds);
                    status.Text = outcome == null ? $"La préparation de {contender.Elder} est gâchée." : $"Le sabotage : {outcome}.";
                    Refresh();
                };
                lineages.AddChild(sabotage);
            }
            if (contenders.Count > 0) Add(lineages, "— Les lignées —");
            foreach (var row in LineagesView.Rows(root.Session))
            {
                var line = new HBoxContainer();
                line.AddChild(new Label { Text = $"{row.Name} — {row.State}", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill });
                if (row.CanReveal)
                {
                    var reveal = new Button { Text = row.RevealLabel };
                    string id = row.Id;
                    reveal.Pressed += () =>
                    {
                        string refusal = root.Session.WorldFruitions.Reveal(id);
                        status.Text = refusal == null ? $"Le miroir lève le voile sur la lignée {row.Name}." : $"Impossible : {refusal}.";
                        Refresh();
                    };
                    line.AddChild(reveal);
                }
                lineages.AddChild(line);
            }
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
            var paths = root.Session.Paths;
            if (paths.Tomb != null) // LORE.md §11.10, C5: a Purple Mansion's tomb keeps an ascent method
            {
                string kept = root.Session.Context.Content.Techniques.FirstOrDefault(t => t.ID == paths.Tomb)?.Name ?? paths.Tomb;
                var explore = new Button { Text = $"Explorer le tombeau d'un Manoir Pourpre (il garde « {kept} »)" };
                explore.Pressed += () =>
                {
                    var outcome = paths.ExploreTomb(root.Session.Shards.BestTeam(CultivationRealm.QiRefinement, 3));
                    status.Text = outcome.Refusal ?? (outcome.Found ? $"L'expédition rapporte « {kept} »." : "Le tombeau garde son secret cette année.");
                    Refresh();
                };
                market.AddChild(explore);
            }
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
                var steal = new Button { Text = "Voler" };
                steal.Pressed += () =>
                {
                    var theft = paths.StealManual(offer.Power, offer.TechniqueId, root.Session.Shards.BestTeam(CultivationRealm.QiRefinement, 3));
                    status.Text = theft.Refusal ?? (theft.Taken ? $"Le manuel « {offer.Name} » est volé sans être vu."
                        : theft.Caught ? $"Le voleur est pris : {offer.Power} tient une preuve." : "Le vol échoue, sans être vu.");
                    Refresh();
                };
                line.AddChild(steal);
                var disciple = new Button { Text = "Envoyer un disciple", TooltipText = "Il pratiquera la méthode à son retour, mais un serment du Dao lui interdit de la transmettre." };
                disciple.Pressed += () =>
                {
                    var member = root.Session.Clan.LivingMembers.Where(m => m.ID != root.Session.Clan.PatriarchID && m.CaptorFaction == null
                        && m.DiscipleOf == null && m.Realm >= CultivationRealm.QiRefinement).OrderByDescending(m => m.SpiritualRoot).FirstOrDefault();
                    status.Text = member == null ? "Aucun cultivateur libre ne peut partir."
                        : paths.SendAsDisciple(member.ID, offer.Power) ?? $"{member.FullName} part servir {offer.Power} comme disciple.";
                    Refresh();
                };
                line.AddChild(disciple);
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
