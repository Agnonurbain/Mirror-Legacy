using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The secret operations screen (L2c.5): the mirror's ritual, the hunt's plan with its odds before launching it, and
    /// the secret. It only binds <see cref="OperationsView"/> and forwards the player's choices to the session.
    /// </summary>
    public partial class Operations : Control
    {
        public const string ScenePath = "res://scenes/Operations.tscn";
        private const string None = "—";

        private GameRoot root;
        private VBoxContainer ritual, hunt, secret;
        private Label status;

        // the plan being built
        private string target, diversionMember, diversionPlace, framed;
        private readonly Dictionary<string, HuntRole> team = new Dictionary<string, HuntRole>();
        private HuntTiming timing;
        private CoverStory cover;
        private MirrorAid aid;
        private string proofFrom;
        private string proofToward;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            ritual = GetNode<VBoxContainer>("%Ritual");
            hunt = GetNode<VBoxContainer>("%Hunt");
            secret = GetNode<VBoxContainer>("%Secret");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            // OPS_TAB=<0-2> opens a tab (screenshots of a smoke run)
            if (int.TryParse(OS.GetEnvironment("OPS_TAB"), out int tab)) GetNode<TabContainer>("%Tabs").CurrentTab = tab;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            ShowRitual();
            ShowHunt();
            ShowSecret();
        }

        // ---- The ritual ----

        private void ShowRitual()
        {
            Clear(ritual);
            var session = root.Session;
            var view = OperationsView.Ritual(session);
            Add(ritual, $"Rituel du miroir : an {view.Year} — chasse {(view.HuntOpen ? "ouverte" : "fermée")}");
            Add(ritual, $"Prières : {view.Prayers} / {view.PrayersNeeded}");

            if (view.Offer != null)
            {
                Add(ritual, $"Le miroir offre à {view.Offer.Bearer} :");
                foreach (var choice in view.Offer.Choices)
                    AddButton(ritual, $"{choice.Name} — {choice.Notes}", () => Report(session.Talismans.Choose(choice.Id)
                        ? $"{view.Offer.Bearer} reçoit « {choice.Name} »." : "Ce choix n'est plus possible."));
                return;
            }

            Add(ritual, view.Beasts.Count == 0 ? "Aucune bête captive." : "Bêtes captives :");
            var bearer = Picker(ritual, "Porteur", view.Bearers.Select(b => (b.Id, $"{b.Name} ({b.Rank})")).ToList());
            var beast = Picker(ritual, "Bête offerte", view.Beasts.Select(b => (b.Id, $"{b.Strength} — {b.Owner}")).ToList());
            var perform = AddButton(ritual, "Accomplir le rituel", () =>
            {
                var chosen = session.Resources.Beasts.FirstOrDefault(b => b.Id == Selected(beast));
                var member = session.Clan.FindById(Selected(bearer));
                Report(session.Talismans.PerformRitual(member, chosen) ? "Le miroir a reçu l'offrande." : "Le rituel échoue à s'accomplir.");
            });
            perform.Disabled = view.Refusal != null;
            if (view.Refusal != null) Add(ritual, $"Impossible pour l'instant : {view.Refusal}.");
        }

        // ---- The hunt ----

        private void ShowHunt()
        {
            Clear(hunt);
            var session = root.Session;
            var targets = OperationsView.HuntTargets(session);
            if (targets.Count == 0)
            {
                Add(hunt, "Aucune bête repérée : envoyez des éclaireurs (tâche « Repérage des bêtes ») sur un terrain de chasse choisi sur la carte.");
                return;
            }
            if (targets.All(t => t.Id != target)) target = targets[0].Id; // a beast taken or gone: the first one left

            var targetPicker = Picker(hunt, "Cible", targets.Select(t => (t.Id, $"{t.Species} ({t.Strength}) — {t.Owner}, {t.Place}")).ToList(), target);
            targetPicker.ItemSelected += _ => { target = Selected(targetPicker); ShowHunt(); };
            ShowTeam(session);
            EnumPicker(hunt, "Moment", timing, OperationsView.TimingLabel, v => timing = v);
            EnumPicker(hunt, "Couverture", cover, OperationsView.CoverLabel, v => cover = v);
            EnumPicker(hunt, "Aide du miroir", aid, OperationsView.AidLabel, v => aid = v);
            ShowDiversion(session);
            ShowFalseTrail(session);
            ShowLaunch(session);
        }

        /// <summary>Each free member with a role, or absent.</summary>
        private void ShowTeam(Session.GameSession session)
        {
            Add(hunt, "Équipe :");
            foreach (var member in OperationsView.HuntCandidates(session))
            {
                var roles = new List<(string, string)> { (None, "— absent") };
                roles.AddRange(Enum.GetValues(typeof(HuntRole)).Cast<HuntRole>().Select(r => (r.ToString(), OperationsView.RoleLabel(r))));
                string current = team.TryGetValue(member.Id, out var role) ? role.ToString() : None;
                var picker = Picker(hunt, $"{member.Name} ({member.Rank})", roles, current);
                picker.ItemSelected += _ =>
                {
                    string value = Selected(picker);
                    if (value == None) team.Remove(member.Id);
                    else team[member.Id] = Enum.Parse<HuntRole>(value);
                    ShowHunt();
                };
            }
        }

        /// <summary>A free member outside the team, seen on another place of the map.</summary>
        private void ShowDiversion(Session.GameSession session)
        {
            var others = OperationsView.HuntCandidates(session).Where(c => !team.ContainsKey(c.Id)).Select(c => (c.Id, c.Name)).ToList();
            if (others.All(o => o.Item1 != diversionMember)) diversionMember = null; // gone, or now in the team
            others.Insert(0, (None, "aucune"));
            var decoy = Picker(hunt, "Diversion (vu ailleurs)", others, diversionMember ?? None);
            decoy.ItemSelected += _ => { diversionMember = Selected(decoy) == None ? null : Selected(decoy); ShowHunt(); };
            if (diversionMember == null) return;

            var places = session.Context.Content.Regions.Where(r => r.ParentId != null).Select(r => (r.Id, r.Name)).ToList();
            if (places.Count == 0) return;
            if (places.All(p => p.Id != diversionPlace)) diversionPlace = places[0].Id;
            var place = Picker(hunt, "… à", places, diversionPlace);
            place.ItemSelected += _ => { diversionPlace = Selected(place); ShowHunt(); };
        }

        /// <summary>A power to blame, or none.</summary>
        private void ShowFalseTrail(Session.GameSession session)
        {
            var powers = session.Factions.Factions.Select(f => (f.Name, f.Name)).ToList();
            if (powers.All(p => p.Item1 != framed)) framed = null;
            powers.Insert(0, (None, "aucune"));
            var frame = Picker(hunt, "Fausse piste (accuser)", powers, framed ?? None);
            frame.ItemSelected += _ => { framed = Selected(frame) == None ? null : Selected(frame); ShowHunt(); };
        }

        /// <summary>The plan's odds and costs, and the launch (disabled, with its reason, when it cannot be).</summary>
        private void ShowLaunch(Session.GameSession session)
        {
            var plan = Plan();
            var preview = OperationsView.HuntPreview(session, plan);
            Add(hunt, $"Approche {preview.Approach} % · capture {preview.Capture} % · traces si tout va bien : {preview.Exposure} · coût : {preview.Stones} pierres, {preview.MirrorPower} de puissance du miroir");
            var launch = AddButton(hunt, "Lancer l'opération", () =>
            {
                var outcome = session.Hunts.Execute(plan);
                team.Clear();
                target = null;
                diversionMember = null;
                diversionPlace = null;
                framed = null;
                Report(outcome.Refusal != null ? $"Refusé : {outcome.Refusal}."
                    : outcome.Captured ? $"La bête est prise.{(outcome.Blamed != null ? $" On accuse {outcome.Blamed}." : "")}"
                    : outcome.Approached ? $"La bête s'échappe.{(outcome.Casualties.Count > 0 ? " Des frappeurs sont tombés." : "")}"
                    : "L'équipe a été vue et a fui.");
            });
            launch.Disabled = preview.Refusal != null;
            if (preview.Refusal != null) Add(hunt, $"Pas encore : {preview.Refusal}.");
        }

        /// <summary>A false proof planted in one power's hands against another (disabled, with its reason, when it cannot be).</summary>
        private void ShowFalseProof(Session.GameSession session)
        {
            var powers = session.Factions.Factions.Select(f => (f.Name, f.Name)).ToList();
            if (powers.Count < 2) return;
            if (powers.All(p => p.Item1 != proofFrom)) proofFrom = powers[0].Item1;   // a power gone: the first one left
            if (powers.All(p => p.Item1 != proofToward)) proofToward = powers[1].Item1;
            var from = Picker(secret, "Fausse preuve : chez", powers, proofFrom);
            from.ItemSelected += _ => { proofFrom = Selected(from); ShowSecret(); };
            var toward = Picker(secret, "… contre", powers, proofToward);
            toward.ItemSelected += _ => { proofToward = Selected(toward); ShowSecret(); };

            string why = OperationsView.FalseProofRefusal(session, proofFrom, proofToward);
            var plant = AddButton(secret, "Fabriquer une fausse preuve", () => Report(session.Secrets.PlantFalseProof(proofFrom, proofToward)
                ? $"Une fausse preuve tourne les yeux de {proofFrom} vers {proofToward}." : "La fausse preuve n'a pu être placée."));
            plant.Disabled = why != null;
            if (why != null) Add(secret, $"Pas encore : {why}.");
        }

        private HuntPlan Plan() => new HuntPlan
        {
            TargetBeastId = target,
            Team = new Dictionary<string, HuntRole>(team),
            Timing = timing,
            Cover = cover,
            Aid = aid,
            DiversionMemberId = diversionMember,
            DiversionRegionId = diversionMember == null ? null : diversionPlace,
            FramedFaction = framed
        };

        // ---- The secret ----

        private void ShowSecret()
        {
            Clear(secret);
            var session = root.Session;
            Add(secret, "Ce que le miroir perçoit :");
            foreach (var sign in OperationsView.Signs(session).Where(p => p.Sign != "calme"))
            {
                var line = new HBoxContainer();
                line.AddChild(new Label { Text = $"{sign.Power} : {sign.Sign}", CustomMinimumSize = new Vector2(420, 0) });
                var blur = new Button { Text = "Brouiller leurs souvenirs" };
                blur.Pressed += () => Report(session.Secrets.BlurMemories(sign.Power)
                    ? $"Le miroir brouille ce que {sign.Power} se rappelle." : "Le miroir manque de puissance.");
                line.AddChild(blur);
                secret.AddChild(line);
            }
            if (OperationsView.Signs(session).All(p => p.Sign == "calme")) Add(secret, "Tout est calme.");

            ShowFalseProof(session);

            Add(secret, "Dans la confidence :");
            var patriarch = session.Clan.GetPatriarch();
            foreach (var keeper in OperationsView.Keepers(session))
            {
                var line = new HBoxContainer();
                line.AddChild(new Label { Text = $"{keeper.Name} — {(keeper.Sworn ? "a juré le secret" : "n'a rien juré")}", CustomMinimumSize = new Vector2(420, 0) });
                if (!keeper.Sworn && patriarch != null && keeper.Id != patriarch.ID)
                {
                    var swear = new Button { Text = "Faire jurer le secret au patriarche" };
                    swear.Pressed += () => Report(session.Oaths.Swear(session.Clan.FindById(keeper.Id), patriarch, new[] { "keep-secret" }) != null
                        ? $"{keeper.Name} jure sur son chemin de garder le secret." : "Le serment n'a pu être prêté.");
                    line.AddChild(swear);
                }
                secret.AddChild(line);
            }
        }

        // ---- Widgets ----

        private void Report(string text)
        {
            status.Text = text;
            Refresh();
        }

        private static void Add(Container box, string text) =>
            box.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart });

        private static Button AddButton(Container box, string text, Action onPressed)
        {
            var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            button.Pressed += onPressed;
            box.AddChild(button);
            return button;
        }

        /// <summary>A labelled drop-down of (value, label) items; item metadata holds the value.</summary>
        private static OptionButton Picker(Container box, string label, IReadOnlyList<(string Value, string Label)> items, string selected = null)
        {
            var line = new HBoxContainer();
            line.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(260, 0) });
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            for (int i = 0; i < items.Count; i++)
            {
                picker.AddItem(items[i].Label, i);
                picker.SetItemMetadata(i, items[i].Value);
                if (items[i].Value == selected) picker.Select(i);
            }
            line.AddChild(picker);
            box.AddChild(line);
            return picker;
        }

        private void EnumPicker<T>(Container box, string label, T current, Func<T, string> name, Action<T> set) where T : struct, Enum
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToList();
            var picker = Picker(box, label, values.Select(v => (v.ToString(), name(v))).ToList(), current.ToString());
            picker.ItemSelected += _ => { set(Enum.Parse<T>(Selected(picker))); ShowHunt(); };
        }

        private static string Selected(OptionButton picker) =>
            picker.Selected < 0 ? null : picker.GetItemMetadata(picker.Selected).AsString();

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
            GD.Print($"[Smoke] Operations: ritual in year {OperationsView.Ritual(root.Session).Year}, {OperationsView.Signs(root.Session).Count} powers watched.");
            if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
