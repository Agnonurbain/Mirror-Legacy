using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The secret operations screen (L2c.5): the mirror's ritual, the hunt's plan with its odds before launching it, the
    /// secret, and the captives on both sides (L6a). It only binds <see cref="OperationsView"/> and forwards the player's choices to the session.
    /// </summary>
    public partial class Operations : Control
    {
        public const string ScenePath = "res://scenes/Operations.tscn";
        private const string None = "—";

        private GameRoot root;
        private VBoxContainer ritual, hunt, secret, captives, probes, chosen;
        private Label status;

        // the plan being built
        private string target, diversionMember, diversionPlace, framed;
        private readonly Dictionary<string, HuntRole> team = new Dictionary<string, HuntRole>();
        private HuntTiming timing;
        private CoverStory cover;
        private MirrorAid aid;
        private string proofFrom;
        private string proofToward;
        // the probe being planned (2026-09-27)
        private string probeTarget, probeLeader, probeSecond, probePartner, sellTo;
        private ProbeApproach probeApproach = ProbeApproach.Infiltration;
        private int probeStones = 300;

        private readonly Dictionary<string, string> rescuers = new Dictionary<string, string>(); // captive → the member sent to free them

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            ritual = GetNode<VBoxContainer>("%Ritual");
            hunt = GetNode<VBoxContainer>("%Hunt");
            secret = GetNode<VBoxContainer>("%Secret");
            captives = GetNode<VBoxContainer>("%Captives");
            probes = GetNode<VBoxContainer>("%Probes");
            chosen = GetNode<VBoxContainer>("%Chosen");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            // OPS_TAB=<0-4> opens a tab (screenshots of a smoke run)
            if (int.TryParse(OS.GetEnvironment("OPS_TAB"), out int tab)) GetNode<TabContainer>("%Tabs").CurrentTab = tab;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            ShowRitual();
            ShowHunt();
            ShowSecret();
            ShowCaptives();
            ShowProbes();
            ShowChosen();
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

        /// <summary>The spouses the powers sent: the mirror sounds them; an unmasked spy is turned or executed.</summary>
        private void ShowSpouses(Session.GameSession session)
        {
            var spouses = OperationsView.Spouses(session);
            if (spouses.Count == 0) return;
            Add(secret, "Conjoints venus des puissances, et regards absents :");
            foreach (var spouse in spouses)
            {
                var line = new HBoxContainer();
                string known = !spouse.Sounded ? "non sondé(e)" : spouse.SpyFor == null ? "loyal(e)" : spouse.DoubleAgent ? $"agent double contre {spouse.SpyFor}" : $"espion(ne) de {spouse.SpyFor}";
                string from = spouse.From == null ? "un regard absent" : $"de {spouse.From}";
                if (spouse.Enthralled) known = $"envoûté(e) par le Vrai Monarque réincarné de {spouse.SpyFor}";
                line.AddChild(new Label { Text = $"{spouse.Name} ({from}) — {known}", CustomMinimumSize = new Vector2(420, 0) });
                if (!spouse.Sounded)
                    AddAction(line, $"Sonder (miroir, {session.Context.Content.Balance.Intrigues.UnmaskMirrorCost})",
                        () => Report(session.Intrigues.Unmask(spouse.Id) ?? "Le miroir manque de puissance."));
                else if (spouse.Enthralled)
                    AddAction(line, $"Briser l'envoûtement (miroir, {session.Context.Content.Balance.Enthrallment.BreakMirrorCost})",
                        () => Report(session.Enthrallment.Break(spouse.Id) is { } r ? $"Refusé : {r}." : $"{spouse.Name} est délivré(e)."));
                if (spouse.Sounded && spouse.SpyFor != null && !spouse.DoubleAgent)
                {
                    AddAction(line, "Retourner (agent double)", () => Report(session.Intrigues.Turn(spouse.Id) is { } r ? $"Refusé : {r}." : $"{spouse.Name} sert désormais le clan."));
                    AddAction(line, "Exécuter", () => Report(session.Intrigues.ExecuteSpy(spouse.Id) is { } r ? $"Refusé : {r}." : $"{spouse.Name} est exécuté(e)."));
                }
                secret.AddChild(line);
            }
        }

        private static void AddAction(Container box, string text, Action onPressed)
        {
            var button = new Button { Text = text };
            button.Pressed += onPressed;
            box.AddChild(button);
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
            ShowSpouses(session);

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

        // ---- The captives (L6a) ----

        private void ShowCaptives()
        {
            Clear(captives);
            var session = root.Session;
            var view = OperationsView.Captives(session);
            Add(captives, "Nos captifs :");
            if (view.Held.Count == 0) Add(captives, "Aucun membre n'est retenu.");
            foreach (var held in view.Held) ShowHeld(session, held, view.Agents);

            Add(captives, "Les agents que nous détenons :");
            if (view.Agents.Count == 0) Add(captives, "Aucun.");
            foreach (var agent in view.Agents) ShowAgent(session, agent);
        }

        /// <summary>A member held: ransom, rescue by a free member, exchange for one of the captor's agents, the mirror's blur.</summary>
        private void ShowHeld(Session.GameSession session, HeldLine held, IReadOnlyList<AgentLine> agents)
        {
            string years = held.Years == 0 ? "cette année" : $"depuis {held.Years} an{(held.Years > 1 ? "s" : "")}";
            Add(captives, $"{held.Name} ({held.Rank}) — retenu par {held.Captor}, {years}{(held.KnowsSecret ? " · connaît le secret du miroir" : "")}");
            var line = new HBoxContainer();
            captives.AddChild(line);
            Act(line, $"Payer la rançon ({held.Ransom} pierres)", () => session.Captives.PayRansom(held.Id), $"{held.Name} est libéré contre rançon.");
            if (held.KnowsSecret)
                Act(line, "Brouiller sa mémoire (miroir)", () => session.Captives.Silence(held.Id), $"Le miroir efface ce que {held.Name} savait.");
            foreach (var agent in agents.Where(a => a.Power == held.Captor))
                Act(line, $"Échanger contre l'agent ({agent.Strength})", () => session.Captives.Exchange(held.Id, agent.Id), $"{held.Name} est échangé.");

            var candidates = OperationsView.HuntCandidates(session).Select(c => (c.Id, $"{c.Name} ({c.Rank})")).ToList();
            if (candidates.Count == 0) return;
            if (!rescuers.TryGetValue(held.Id, out var rescuer) || candidates.All(c => c.Id != rescuer))
                rescuer = rescuers[held.Id] = candidates[0].Id;
            var picker = Picker(captives, "Sauvetage par", candidates, rescuer);
            picker.ItemSelected += _ => { rescuers[held.Id] = Selected(picker); ShowCaptives(); };
            Act(captives, "Tenter le sauvetage", () => session.Captives.Rescue(held.Id, new[] { rescuers[held.Id] }), $"{held.Name} est arraché à {held.Captor}.");
        }

        /// <summary>An agent held: sold back, released, interrogated and denounced once each, or executed.</summary>
        private void ShowAgent(Session.GameSession session, AgentLine agent)
        {
            Add(captives, $"Agent de {agent.Power} ({agent.Strength}){(agent.Interrogated ? " · interrogé" : "")}{(agent.Denounced ? " · montré aux autres" : "")}");
            var line = new HBoxContainer();
            captives.AddChild(line);
            Act(line, $"Le revendre ({agent.Price} pierres)", () => session.Captives.SellBack(agent.Id), $"{agent.Power} rachète son agent.");
            Act(line, "Le relâcher", () => session.Captives.Release(agent.Id), $"L'agent retourne auprès de {agent.Power}.");
            if (!agent.Interrogated)
            {
                var ask = new Button { Text = "L'interroger" };
                ask.Pressed += () => Report(session.Captives.Interrogate(agent.Id) ?? "Il n'a plus rien à dire.");
                line.AddChild(ask);
            }
            if (!agent.Denounced)
                Act(line, "Le montrer aux autres puissances", () => session.Captives.Denounce(agent.Id), $"Les autres puissances savent ce que {agent.Power} a tramé.");
            Act(line, "L'exécuter", () => session.Captives.Execute(agent.Id), "L'agent est exécuté.");
        }

        // ---- Probes and secrets (2026-09-27) ----

        private void ShowProbes()
        {
            Clear(probes);
            var session = root.Session;
            ShowProbePlan(session);
            ShowKnownSecrets(session);
            Add(probes, "Les secrets du clan :");
            var own = SecretsView.Clans(session);
            if (own.Count == 0) Add(probes, "Aucun, pour l'instant (outre le miroir).");
            foreach (var secret in own) Add(probes, $"   · {secret.Name} ({secret.Rank}) — {secret.Sign}");
        }

        private void ShowProbePlan(Session.GameSession session)
        {
            Add(probes, "Préparer un sondage :");
            var powers = session.Factions.Factions.Select(f => (f.Name, f.Name)).ToList();
            if (powers.All(p => p.Item1 != probeTarget)) probeTarget = powers.FirstOrDefault().Item1;
            var target = Picker(probes, "Cible", powers, probeTarget);
            target.ItemSelected += _ => { probeTarget = Selected(target); ShowProbes(); };
            var approaches = Enum.GetValues(typeof(ProbeApproach)).Cast<ProbeApproach>().Select(a => (a.ToString(), SecretsView.ApproachLabel(a))).ToList();
            var approach = Picker(probes, "Méthode", approaches, probeApproach.ToString());
            approach.ItemSelected += _ => { probeApproach = Enum.Parse<ProbeApproach>(Selected(approach)); ShowProbes(); };

            var free = OperationsView.HuntCandidates(session).Select(c => (c.Id, $"{c.Name} ({c.Rank})")).ToList();
            if (free.All(c => c.Item1 != probeLeader)) probeLeader = free.FirstOrDefault().Item1;
            var leader = Picker(probes, "Chef", free, probeLeader);
            leader.ItemSelected += _ => { probeLeader = Selected(leader); ShowProbes(); };
            var seconds = free.Where(c => c.Item1 != probeLeader).Prepend((None, "personne")).ToList();
            if (seconds.All(c => c.Item1 != probeSecond)) probeSecond = None;
            var second = Picker(probes, "Second", seconds, probeSecond);
            second.ItemSelected += _ => { probeSecond = Selected(second); ShowProbes(); };
            var partners = powers.Where(p => p.Item1 != probeTarget).Prepend((None, "aucun")).ToList();
            if (partners.All(p => p.Item1 != probePartner)) probePartner = None;
            var partner = Picker(probes, "Partenaire", partners, probePartner);
            partner.ItemSelected += _ => { probePartner = Selected(partner); ShowProbes(); };
            if (probeApproach == ProbeApproach.Bribery)
            {
                var sums = new[] { 100, 300, 600, 1000 }.Select(v => (v.ToString(), $"{v} pierres")).ToList();
                var stones = Picker(probes, "Pot-de-vin", sums, probeStones.ToString());
                stones.ItemSelected += _ => { probeStones = int.Parse(Selected(stones)); ShowProbes(); };
            }
            LaunchProbe(session);
        }

        private void LaunchProbe(Session.GameSession session)
        {
            var team = new[] { probeLeader, probeSecond }.Where(id => id != null && id != None).ToList();
            var allies = probePartner != null && probePartner != None ? new List<string> { probePartner } : new List<string>();
            var plan = new ProbePlan(probeTarget, probeApproach, team, allies, probeApproach == ProbeApproach.Bribery ? probeStones : 0);
            var preview = SecretsView.Preview(session, plan);
            Add(probes, preview.Refusal == null ? $"Chances : {preview.Chance} %" : $"Pas encore : {preview.Refusal}.");
            var send = new Button { Text = "Lancer le sondage", Disabled = preview.Refusal != null, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            send.Pressed += () =>
            {
                var outcome = session.Probes.Probe(plan);
                probeLeader = probeSecond = probePartner = null; // the team is spent for the year: plan anew
                Report(outcome.Refusal != null ? $"Refusé : {outcome.Refusal}."
                    : (outcome.Success ? "Le sondage porte ses fruits." : "Le sondage n'apprend rien.")
                      + (outcome.Revealed.Count > 0 ? " Un secret est percé !" : "")
                      + (outcome.Detected ? " Il a été éventé." : "")
                      + (outcome.Disaster ? " Désastre : un des nôtres est pris." : "")
                      + (outcome.LateAllies.Count > 0 ? $" ({string.Join(", ", outcome.LateAllies)} a tardé à venir en aide.)" : ""));
            };
            probes.AddChild(send);
        }

        private void ShowKnownSecrets(Session.GameSession session)
        {
            Add(probes, "Ce que le clan sait des autres :");
            var known = SecretsView.Known(session);
            if (known.Count == 0) Add(probes, "Rien encore.");
            var buyers = session.Factions.Factions.Select(f => (f.Name, f.Name)).ToList();
            foreach (var secret in known)
            {
                var line = new HBoxContainer();
                line.AddChild(new Label { Text = $"{secret.Holder} : {secret.Name} ({secret.Rank}){(secret.Spent ? " · déjà utilisé" : "")}", CustomMinimumSize = new Vector2(420, 0) });
                AddAction(line, "Chantage", () => Report(session.Dealings.Blackmail(secret.Id) is { } r ? $"Refusé : {r}." : $"{secret.Holder} paie pour votre silence."));
                AddAction(line, "Révéler", () => Report(session.Dealings.Expose(secret.Id) is { } r ? $"Refusé : {r}." : "Tous le savent désormais."));
                var sell = new Button { Text = "Vendre", Disabled = sellTo == secret.Holder, TooltipText = sellTo == secret.Holder ? "on ne vend pas un secret à celui qui le détient" : "" };
                sell.Pressed += () => Report(session.Dealings.Sell(secret.Id, sellTo) is { } r ? $"Refusé : {r}." : $"Le secret est vendu à {sellTo}.");
                line.AddChild(sell);
                probes.AddChild(line);
            }
            if (known.Count > 0)
            {
                if (buyers.All(b => b.Item1 != sellTo)) sellTo = buyers.FirstOrDefault().Item1;
                var buyer = Picker(probes, "Vendre à", buyers, sellTo);
                buyer.ItemSelected += _ => { sellTo = Selected(buyer); ShowProbes(); };
            }
        }

        /// <summary>A button for an action that answers with its refusal, or null when done.</summary>
        private void Act(Container box, string text, Func<string> action, string done)
        {
            var button = new Button { Text = text };
            button.Pressed += () =>
            {
                string refusal = action();
                Report(refusal == null ? done : $"Refusé : {refusal}.");
            };
            box.AddChild(button);
        }

        // ---- The powers' Chosen (audit §1.8) ----

        /// <summary>The powers' young Chosen a free Purple Mansion of the clan senses, and the harvests it may try.</summary>
        private void ShowChosen()
        {
            Clear(chosen);
            var session = root.Session;
            Add(chosen, "Les Élus du Destin des puissances : un Vrai Monarque renaît chez elles ; tant qu'il est jeune, un Manoir Pourpre peut le récolter.");
            var lines = OperationsView.Chosen(session);
            if (lines.Count == 0) Add(chosen, "Le clan ne sent aucun Élu (seul un Manoir Pourpre libre du clan lit le destin).");
            var bends = OperationsView.Bends(session);
            if (bends.Count > 0) Add(chosen, "Un ancêtre revenu du clan peut plier l'esprit d'un ancien moindre d'une puissance : ses yeux chez elle.");
            foreach (var bend in bends)
                Act(chosen, bend.Label, () => session.Enthrallment.Bend(bend.AncestorId, bend.Power), $"Un ancien de {bend.Power} sert désormais d'yeux au clan.");
            foreach (var line in lines)
            {
                Add(chosen, $"{line.Name}, l'Élu de {line.Power} — {line.Age} an{(line.Age > 1 ? "s" : "")}");
                foreach (var harvest in line.Harvests)
                    Act(chosen, harvest.Label, () => session.Rebirths.Harvest(line.Power, harvest.HarvesterId),
                        $"{line.Name} ne reviendra pas : l'Élu de {line.Power} est récolté.");
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
            if (root.SmokeEndsOnLibrary) root.GoTo(Library.ScenePath); // the library checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
