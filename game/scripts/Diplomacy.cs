using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The diplomacy screen (G6; D7): the powers, the treaties that bind them to the clan (broken here, at a price), and a
    /// proposal — kind, secret, sealed by the patriarch's oath, the clan as suzerain, a term — refused with its reason
    /// before it is made; tribute and war. It only binds <see cref="DiplomacyView"/> and forwards the choices.
    /// </summary>
    public partial class Diplomacy : Control
    {
        public const string ScenePath = "res://scenes/Diplomacy.tscn";
        private const int TributeStones = 300;
        private static readonly int?[] Terms = { null, 5, 10, 20 };

        private GameRoot root;
        private VBoxContainer powers;
        private VBoxContainer proposal;
        private Label status;

        // the proposal being built
        private string power;
        private TreatyKind kind;
        private bool secret;
        private bool sealedByOath;
        private bool clanAsSuzerain;
        private string groom; // the member offered in marriage
        private int term;

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            powers = GetNode<VBoxContainer>("%Powers");
            proposal = GetNode<VBoxContainer>("%Proposal");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => GetTree().ChangeSceneToFile(ClanDomain.ScenePath);
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            ShowPowers();
            ShowProposal();
        }

        private void ShowPowers()
        {
            Clear(powers);
            var session = root.Session;
            ShowCoalitionAndCall(session);
            ShowPatrons(session);
            ShowWars(session);
            foreach (var line in DiplomacyView.Powers(session))
            {
                var row = new HBoxContainer();
                var pick = new Button { Text = line.Name, Flat = line.Name != power, CustomMinimumSize = new Vector2(260, 0) };
                pick.Pressed += () => { power = line.Name; Refresh(); };
                row.AddChild(pick);
                string bonds = (line.Allies?.Count > 0 ? $" · alliée de {string.Join(", ", line.Allies)}" : "")
                    + (line.Suzerain != null ? $" · vassale de {line.Suzerain}" : "")
                    + (line.ClanWatch != null ? $" · {line.ClanWatch}" : "");
                row.AddChild(new Label { Text = $"{line.Kind} · {line.HighestRealm} · relation {line.Relation:+#;-#;0}{bonds}" });
                powers.AddChild(row);
                foreach (var treaty in line.Treaties) ShowTreaty(treaty);
            }
        }

        /// <summary>The wars under way; the clan's may end with a tribute.</summary>
        private void ShowWars(Session.GameSession session)
        {
            var wars = DiplomacyView.Wars(session);
            if (wars.Count == 0) return;
            Add(powers, "Guerres :");
            foreach (var war in wars)
            {
                var row = new HBoxContainer();
                row.AddChild(new Label { Text = $"⚔ {war.Description}", CustomMinimumSize = new Vector2(460, 0) });
                if (war.ClansWar)
                {
                    var peace = new Button { Text = $"Demander la paix ({war.PeaceCost} pierres)" };
                    peace.Pressed += () => Report(session.Wars.SuePeace(war.Enemy), $"La paix est faite avec {war.Enemy}.");
                    row.AddChild(peace);
                }
                powers.AddChild(row);
            }
        }

        /// <summary>The great partners of the very high level: a pact, its favour, its end.</summary>
        private void ShowPatrons(Session.GameSession session)
        {
            Add(powers, "Grands partenaires :");
            foreach (var patron in DiplomacyView.Patrons(session))
            {
                var row = new HBoxContainer();
                row.AddChild(new Label { Text = $"{patron.Name} ({patron.Kind}, {patron.Boon}, tribut {patron.Tribute}){(patron.Bound ? $" · {patron.Favor}" : "")}", CustomMinimumSize = new Vector2(460, 0) });
                var act = patron.Bound
                    ? new Button { Text = "Mettre fin au pacte", TooltipText = patron.EndWarning ?? "sans danger : elle vous est favorable" }
                    : new Button { Text = "Proposer un pacte", Disabled = patron.Refusal != null, TooltipText = patron.Refusal ?? "" };
                act.Pressed += () => Report(patron.Bound ? session.Patrons.End(patron.Id) : session.Patrons.Propose(patron.Id),
                    patron.Bound ? $"Le pacte avec {patron.Name} prend fin." : $"{patron.Name} accepte le pacte.");
                row.AddChild(act);
                if (!patron.Bound && patron.Refusal != null) row.AddChild(new Label { Text = $"({patron.Refusal})" });
                powers.AddChild(row);
            }
        }

        /// <summary>A coalition against the clan, the blackmail awaiting an answer, and an ally's call to arms.</summary>
        private void ShowCoalitionAndCall(Session.GameSession session)
        {
            foreach (var demand in DiplomacyView.Demands(session))
            {
                Add(powers, $"✉ {demand.Faction} exige {demand.Stones} pierres pour son silence (sans réponse cette année : un refus).");
                var demandRow = new HBoxContainer();
                var pay = new Button { Text = $"Payer ({demand.Stones} pierres)" };
                pay.Pressed += () => Report(session.Intrigues.Pay(demand.Faction), $"{demand.Faction} se tait, pour un temps.");
                var defy = new Button { Text = "Refuser (elle répand ses preuves)" };
                defy.Pressed += () => Report(session.Intrigues.Refuse(demand.Faction), $"{demand.Faction} répand ce qu'elle sait.");
                demandRow.AddChild(pay);
                demandRow.AddChild(defy);
                powers.AddChild(demandRow);
            }
            if (DiplomacyView.Coalition(session) is { } coalition)
                Add(powers, $"⚠ Coalition contre le clan : {string.Join(", ", coalition.Members)} — encore {coalition.YearsLeft} an(s).");
            if (DiplomacyView.Call(session) is not { } call) return;
            Add(powers, $"⚔ {call.Ally}, attaquée par {call.Attacker}, appelle le clan aux armes (sans réponse cette année : un refus).");
            var row = new HBoxContainer();
            var answer = new Button { Text = $"Répondre ({call.Cost} pierres)" };
            answer.Pressed += () => Report(session.Politics.AnswerCall(), $"Le clan se tient aux côtés de {call.Ally}.");
            var refuse = new Button { Text = "Refuser (le traité est rompu)" };
            refuse.Pressed += () => Report(session.Politics.RefuseCall(), $"Le clan laisse {call.Ally} seule.");
            row.AddChild(answer);
            row.AddChild(refuse);
            powers.AddChild(row);
        }

        private void ShowTreaty(TreatyLine treaty)
        {
            var row = new HBoxContainer();
            string side = treaty.Kind == DiplomacyView.KindLabel(TreatyKind.Vassalage)
                ? (treaty.ClanIsSuzerain ? " (le clan suzerain)" : $" (le clan vassal, emprise {treaty.Grip}, absorption {treaty.Absorptions}/{treaty.AbsorptionSteps})")
                : "";
            string term = treaty.YearsLeft.HasValue ? $", encore {treaty.YearsLeft} an(s)" : "";
            row.AddChild(new Label
            {
                Text = $"      ↳ {treaty.Kind}{side}{(treaty.Secret ? " · secret" : "")}{(treaty.Sealed ? " · scellé par serment" : "")}{term}"
            });
            var breakIt = treaty.SpouseId != null ? new Button { Text = "Répudier le conjoint" } : new Button { Text = "Rompre" };
            breakIt.Pressed += () => Report(treaty.SpouseId != null ? root.Session.Matches.Repudiate(treaty.SpouseId) : root.Session.Treaties.Break(treaty.Id),
                treaty.SpouseId != null ? "Le conjoint retourne auprès des siens." : $"Le clan rompt le traité ({treaty.Kind}).");
            row.AddChild(breakIt);
            powers.AddChild(row);
        }

        private void ShowProposal()
        {
            Clear(proposal);
            var session = root.Session;
            if (power == null)
            {
                Add(proposal, "Choisissez une puissance à gauche.");
                return;
            }
            Add(proposal, $"Proposer à {power} :");
            var kinds = Enum.GetValues(typeof(TreatyKind)).Cast<TreatyKind>().ToList();
            var kindPicker = new OptionButton();
            foreach (var k in kinds) kindPicker.AddItem(DiplomacyView.KindLabel(k));
            kindPicker.Select(kinds.IndexOf(kind));
            kindPicker.ItemSelected += i => { kind = kinds[(int)i]; ShowProposal(); };
            proposal.AddChild(kindPicker);
            Check("Secret (les autres n'en savent rien)", secret, v => secret = v);
            Check("Scellé par le serment du patriarche", sealedByOath, v => sealedByOath = v);
            if (kind == TreatyKind.Vassalage) Check("Le clan comme suzerain", clanAsSuzerain, v => clanAsSuzerain = v);
            var termPicker = new OptionButton();
            foreach (var t in Terms) termPicker.AddItem(t.HasValue ? $"{t} ans" : "sans terme");
            termPicker.Select(term);
            termPicker.ItemSelected += i => { term = (int)i; ShowProposal(); };
            proposal.AddChild(termPicker);

            bool suzerain = kind == TreatyKind.Vassalage && clanAsSuzerain;
            string why;
            Button propose;
            if (kind == TreatyKind.Marriage)
            {
                var candidates = DiplomacyView.MarriageCandidates(session);
                if (candidates.All(c => c.Id != groom)) groom = candidates.FirstOrDefault()?.Id;
                var picker = new OptionButton();
                foreach (var c in candidates) picker.AddItem($"{c.Name} ({c.Rank})");
                if (groom != null) picker.Select(candidates.ToList().FindIndex(c => c.Id == groom));
                picker.ItemSelected += i => { groom = candidates[(int)i].Id; ShowProposal(); };
                proposal.AddChild(picker);
                why = groom == null ? "aucun membre à marier" : session.Matches.Refusal(power, groom);
                propose = new Button { Text = "Proposer le mariage", Disabled = why != null, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
                propose.Pressed += () => Report(session.Matches.Propose(power, groom), $"{power} accepte : un lien de sang unit désormais les deux maisons.");
            }
            else
            {
                why = DiplomacyView.ProposalRefusal(session, power, kind, suzerain, sealedByOath);
                propose = new Button { Text = "Proposer le traité", Disabled = why != null, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
                propose.Pressed += () => Report(session.Treaties.Propose(power, kind, secret, sealedByOath, suzerain, Terms[term]),
                    $"{power} accepte le traité ({DiplomacyView.KindLabel(kind)}).");
            }
            proposal.AddChild(propose);
            if (why != null) Add(proposal, $"Pas encore : {why}.");

            Add(proposal, "Autres actes :");
            var faction = session.Factions.GetFactionByName(power);
            if (faction == null)
            {
                Add(proposal, "Cette puissance a disparu.");
                return;
            }
            var tribute = new Button { Text = $"Offrir un tribut ({TributeStones} pierres)", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            tribute.Pressed += () => Report(session.Alliances.OfferTribute(faction.ID, TributeStones) ? null : "pierres insuffisantes", $"{power} reçoit le tribut.");
            proposal.AddChild(tribute);
            var war = new Button { Text = "Déclarer la guerre", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            war.Pressed += () => Report(session.Wars.DeclareOn(power), $"Le clan déclare la guerre à {power}.");
            proposal.AddChild(war);
        }

        private void Check(string text, bool value, Action<bool> set)
        {
            var box = new CheckBox { Text = text, ButtonPressed = value };
            box.Toggled += on => { set(on); ShowProposal(); };
            proposal.AddChild(box);
        }

        /// <summary>An action answers with its refusal, or null when done.</summary>
        private void Report(string refusal, string done)
        {
            status.Text = refusal == null ? done : $"Refusé : {refusal}.";
            Refresh();
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
            power ??= DiplomacyView.Powers(root.Session).FirstOrDefault()?.Name; // shown with a proposal open
            Refresh();
            GD.Print($"[Smoke] Diplomacy: {DiplomacyView.Powers(root.Session).Count} powers, {root.Session.Treaties.All.Count} treaties.");
            if (root.SmokeEndsOnMirror) GetTree().ChangeSceneToFile(MirrorScreen.ScenePath); // the mirror checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
