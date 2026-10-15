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
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            powers = GetNode<VBoxContainer>("%Powers");
            proposal = GetNode<VBoxContainer>("%Proposal");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            if (OS.GetEnvironment("DIP_POWER") is { Length: > 0 } chosen) power = chosen; // DIP_POWER=<name> opens on a power (screenshots)
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
            ShowDebate(session);
            ShowConversions(session);
            ShowMonks(session);
            ShowDebateTargets(session);
            ShowPatrons(session);
            ShowDivineDao(session);
            ShowHauntings(session);
            ShowShamanism(session);
            ShowDemonicCores(session);
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

        /// <summary>The cultivators of any human way who may convert at a temple — dying, or their way closed — giving it up (R13-R14, L7).</summary>
        private void ShowConversions(Session.GameSession session)
        {
            var lines = DiplomacyView.Conversions(session);
            if (lines.Count == 0) return;
            Add(powers, "Conversion bouddhiste (aux portes de la mort ou sur un chemin fermé, au prix de sa voie ; à la Fondation et au Manoir Pourpre, le passage peut blesser le Dao) :");
            foreach (var line in lines)
            {
                Add(powers, $"  {line.Name} ({line.Rank}, {line.YearsLeft} an{(line.YearsLeft > 1 ? "s" : "")} à vivre) :");
                var row = Column();
                foreach (var temple in line.Temples)
                {
                    var convert = new Button { Text = $"Au {temple.Name}", Disabled = temple.Refusal != null, TooltipText = temple.Refusal ?? temple.School };
                    convert.Pressed += () =>
                    {
                        var member = session.Clan.FindById(line.MemberId);
                        Report(session.Buddhism.ConversionRefusal(member, temple.Name)
                            ?? (session.Buddhism.Convert(member, temple.Name) ? null : "la conversion échoue"), $"{line.Name} se convertit au {temple.Name}.");
                    };
                    row.AddChild(convert);
                }
                powers.AddChild(row);
            }
        }

        /// <summary>A temple's challenge to a Dao debate: a champion, or a refusal at the cost of face (R13-14b).</summary>
        private void ShowDebate(Session.GameSession session)
        {
            var pending = session.Debates.Pending;
            if (pending == null) return;
            Add(powers, $"Le {pending.Temple} défie le clan en débat du Dao ! Choisissez un champion, ou refusez (le clan perd la face) :");
            var row = Column();
            foreach (var d in DiplomacyView.Debaters(session, pending.Temple))
            {
                var send = new Button { Text = $"{d.Name} ({d.Odds} %)", TooltipText = d.Rank };
                send.Pressed += () => Report(session.Debates.Answer(session.Clan.FindById(d.MemberId)), DebateReport(session, d.Name, pending.Temple));
                row.AddChild(send);
            }
            var decline = new Button { Text = "Refuser le débat" };
            decline.Pressed += () => Report(session.Debates.Decline(), $"Le clan refuse le débat du {pending.Temple}.");
            row.AddChild(decline);
            powers.AddChild(row);
        }

        /// <summary>The clan's monks: a vow each may take, and what it forbids (R13-14b).</summary>
        private void ShowMonks(Session.GameSession session)
        {
            var monks = DiplomacyView.Monks(session);
            if (monks.Count == 0) return;
            Add(powers, "Moines du clan et leurs vœux (un vœu rompu blesse le mandat de vie) :");
            foreach (var monk in monks)
            {
                Add(powers, $"  {monk.Name} ({monk.Rank}) — {monk.Vow ?? "sans vœu"}");
                var row = Column();
                foreach (var choice in monk.Choices)
                {
                    var take = new Button { Text = $"Faire {choice.Label}", TooltipText = choice.Forbids };
                    take.Pressed += () => Report(session.Buddhism.TakeVow(session.Clan.FindById(monk.MemberId), choice.Vow), $"{monk.Name} fait {choice.Label}.");
                    row.AddChild(take);
                }
                powers.AddChild(row);
            }
        }

        /// <summary>The temples the clan may challenge to a Dao debate, with a chosen debater and its odds (R13-14b).</summary>
        private void ShowDebateTargets(Session.GameSession session)
        {
            var targets = DiplomacyView.DebateTargets(session);
            if (targets.Count == 0) return;
            Add(powers, "Débats du Dao (perdre coûte la face, et peut convertir, blesser ou briser le débatteur) :");
            foreach (var target in targets)
            {
                Add(powers, $"  {target.Temple} ({target.School}){(target.Renegade != null ? $" — son champion {target.Renegade}, renégat du clan, connaît les siens" : "")}{(target.Refusal != null ? $" — {target.Refusal}" : "")}");
                var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
                var debater = new OptionButton();
                foreach (var d in target.Debaters) debater.AddItem($"{d.Name} — {d.Odds} %");
                row.AddChild(debater);
                var go = new Button { Text = "Débattre", Disabled = target.Refusal != null, TooltipText = target.Refusal ?? "" };
                go.Pressed += () =>
                {
                    if (debater.Selected < 0 || debater.Selected >= target.Debaters.Count) return;
                    var d = target.Debaters[debater.Selected];
                    Report(session.Debates.Challenge(session.Clan.FindById(d.MemberId), target.Temple), DebateReport(session, d.Name, target.Temple));
                };
                row.AddChild(go);
                powers.AddChild(row);
            }
        }

        /// <summary>The Divine Dao (R15): who may bind itself to a Profundity, and how; the owners' Divine Core.</summary>
        private void ShowDivineDao(Session.GameSession session)
        {
            var attendants = DiplomacyView.Attendants(session);
            var cores = DiplomacyView.DivineCores(session);
            if (attendants.Count == 0 && cores.Count == 0) return;
            Add(powers, "Dao Divin (se lier à une Profondeur, cultivateur ou bête liée ; on garde son royaume, on ne revient pas en arrière ; à la Fondation et au Manoir Pourpre, le passage peut blesser le Dao) :");
            foreach (var line in attendants)
            {
                Add(powers, $"  {line.Name} ({line.Rank}) :");
                var column = Column();
                foreach (var lender in line.Lenders)
                {
                    var borrow = new Button { Text = $"Emprunter à {lender.Name}", TooltipText = "Pèse un royaume de plus tant que le pacte tient ; sans progrès propre ; le pacte rompu, sa puissance s'effondre." };
                    borrow.Pressed += () => Report(session.Divine.Borrow(session.Clan.FindById(line.MemberId), lender.Id), $"{line.Name} emprunte la puissance de {lender.Name}.");
                    column.AddChild(borrow);
                }
                var unite = new Button { Text = "S'unir au domaine du clan", TooltipText = "Ne meurt plus de vieillesse, mais ne quitte plus le domaine ; plafonne au Manoir Pourpre." };
                unite.Pressed += () => Report(session.Divine.Unite(session.Clan.FindById(line.MemberId)), $"{line.Name} s'unit au domaine du clan.");
                column.AddChild(unite);
                var own = new Button { Text = "Posséder une Profondeur", TooltipText = "Des rituels qui dépensent le prestige du clan chaque année ; mène au Noyau Divin." };
                own.Pressed += () => Report(session.Divine.Own(session.Clan.FindById(line.MemberId)), $"{line.Name} entreprend de posséder une Profondeur.");
                column.AddChild(own);
                powers.AddChild(column);
            }
            foreach (var core in cores)
            {
                var condense = new Button { Text = core.Label, Disabled = core.Refusal != null, TooltipText = core.Refusal ?? "" };
                condense.Pressed += () =>
                {
                    var member = session.Clan.FindById(core.MemberId);
                    string refusal = session.Divine.CondenseCore(member);
                    Report(refusal, member?.GoldenCore == GoldenCoreState.DivineCore ? $"{member.FullName} condense un Noyau Divin." : "Le Noyau Divin se dérobe : son Dao est blessé.");
                };
                var column = Column();
                column.AddChild(condense);
                powers.AddChild(column);
            }
        }

        /// <summary>The Shamanic Talisman Dao (R17): who may pass by the Grand Shaman's foundation, by which method; the rite of blood.</summary>
        private void ShowShamanism(Session.GameSession session)
        {
            var candidates = DiplomacyView.ShamanCandidates(session);
            var cores = DiplomacyView.ShamanCores(session);
            if (candidates.Count == 0 && cores.Count == 0) return;
            Add(powers, $"Dao chamanique (par la fondation du Grand Chaman ; on garde son royaume, repris au premier stade ; le passage peut blesser le Dao au Manoir Pourpre et à la Fondation) — essence de sang : {session.Resources.BloodEssence} :");
            var methods = new[]
            {
                (ShamanMethod.Transformation, "pèse un royaume de plus au combat"),
                (ShamanMethod.Concealment, "les embuscades, captures et chasses au Dao des puissances ne le trouvent plus"),
                (ShamanMethod.Sacrifice, "brûle de l'essence de sang pour cultiver bien plus vite")
            };
            foreach (var member in candidates)
            {
                Add(powers, $"  {member.Name} ({member.Rank}) :");
                var column = Column();
                foreach (var (method, effect) in methods)
                {
                    var pass = new Button { Text = $"Passer par {DiplomacyView.ShamanMethodLabel(method)}", TooltipText = effect };
                    pass.Pressed += () => Report(session.Shamans.Convert(session.Clan.FindById(member.Id), method), $"{member.Name} passe au Dao chamanique.");
                    column.AddChild(pass);
                }
                powers.AddChild(column);
            }
            foreach (var core in cores)
            {
                var rite = new Button { Text = core.Label, Disabled = core.Refusal != null, TooltipText = core.Refusal ?? "" };
                rite.Pressed += () =>
                {
                    var member = session.Clan.FindById(core.MemberId);
                    string refusal = session.Shamans.CondenseCore(member);
                    Report(refusal, member?.GoldenCore == GoldenCoreState.ShamanicEquivalent ? $"{member.FullName} forge un Noyau d'Or chamanique." : "Le rite échoue : son Dao est blessé.");
                };
                var column = Column();
                column.AddChild(rite);
                powers.AddChild(column);
            }
        }

        /// <summary>The hauntings of the dead Divine Attendants (L7): honour them for prestige, at a doubled danger, or exorcise them.</summary>
        private void ShowHauntings(Session.GameSession session)
        {
            var hauntings = DiplomacyView.Hauntings(session);
            if (hauntings.Count == 0) return;
            Add(powers, $"Hantises (un Serviteur Divin mort hante le clan : folie et calamités ; honoré, il donne du prestige mais la commémoration double le danger ; un moine du clan exorcise à moitié prix) — pierres : {session.Resources.SpiritStones} :");
            foreach (var line in hauntings)
            {
                Add(powers, $"  {line.Label} :");
                var column = Column();
                if (!line.Honoured)
                {
                    var honour = new Button { Text = "Honorer sa mémoire", TooltipText = "Du prestige chaque année, mais deux fois plus de folie et de calamités." };
                    honour.Pressed += () => Report(session.Haunting.Honour(line.Id), "Le clan honore sa mémoire.");
                    column.AddChild(honour);
                }
                var exorcise = new Button { Text = $"L'exorciser ({line.ExorcismCost} pierres)", Disabled = session.Resources.SpiritStones < line.ExorcismCost,
                    TooltipText = session.Resources.SpiritStones < line.ExorcismCost ? $"il faut {line.ExorcismCost} pierres spirituelles" : "La hantise prend fin." };
                exorcise.Pressed += () => Report(session.Haunting.Exorcise(line.Id), "La hantise est exorcisée.");
                column.AddChild(exorcise);
                powers.AddChild(column);
            }
        }

        /// <summary>The Demonic Dao (L7): the bound beasts at their apogee, whose core the clan's offering may awaken.</summary>
        private void ShowDemonicCores(Session.GameSession session)
        {
            var cores = DiplomacyView.DemonicCores(session);
            if (cores.Count == 0) return;
            Add(powers, "Dao Démoniaque (à l'apogée du Manoir Pourpre, la lignée d'une bête liée peut s'éveiller d'elle-même, rarement, en un Noyau d'Or démoniaque ; une offrande du clan la presse) :");
            foreach (var core in cores)
            {
                var offer = new Button { Text = core.Label, Disabled = core.Refusal != null, TooltipText = core.Refusal ?? "" };
                offer.Pressed += () =>
                {
                    var member = session.Clan.FindById(core.MemberId);
                    string refusal = session.DemonicDao.Offer(member);
                    Report(refusal, member?.GoldenCore == GoldenCoreState.DemonicEquivalent ? $"La lignée de {member.FullName} s'éveille : un Noyau d'Or démoniaque." : "L'éveil se dérobe : son Dao est blessé.");
                };
                var column = Column();
                column.AddChild(offer);
                powers.AddChild(column);
            }
        }

        /// <summary>A column of buttons under its line: each stays within the narrow list, never off to the right.</summary>
        private static VBoxContainer Column() => new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };

        private static string DebateReport(Session.GameSession session, string name, string temple) => session.Debates.LastFate switch
        {
            DebateFate.Won => $"{name} l'emporte contre le {temple}.",
            DebateFate.Converted => $"{name} perd, et le {temple} impose sa conversion.",
            DebateFate.Wounded => $"{name} perd : son mandat de vie est blessé.",
            DebateFate.Fell => $"{name} perd : sa voie du Dharma se brise.",
            DebateFate.Inducted => $"{name} perd, et le {temple} le fait entrer dans sa doctrine.",
            _ => $"Débat contre le {temple} terminé."
        };

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
                Add(powers, $"✉ {demand.Faction} exige {demand.Stones} pierres {demand.Reason} (sans réponse cette année : un refus).");
                var demandRow = new HBoxContainer();
                var pay = new Button { Text = $"Payer ({demand.Stones} pierres)" };
                pay.Pressed += () => Report(session.Intrigues.Pay(demand.Faction), $"{demand.Faction} est payée : elle se tient tranquille, pour un temps.");
                var defy = new Button { Text = $"Refuser ({demand.Refusal})" };
                defy.Pressed += () => Report(session.Intrigues.Refuse(demand.Faction), $"Refusé : {demand.Faction} {demand.Refusal}.");
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
                ? (treaty.ClanIsSuzerain ? $" (le clan suzerain, emprise {treaty.Grip}, absorption {treaty.Absorptions}/{treaty.AbsorptionSteps})"
                    : $" (le clan vassal, emprise {treaty.Grip}, absorption {treaty.Absorptions}/{treaty.AbsorptionSteps})")
                : "";
            string term = treaty.YearsLeft.HasValue ? $", encore {treaty.YearsLeft} an(s)" : "";
            row.AddChild(new Label
            {
                Text = $"      ↳ {treaty.Kind}{side}{(treaty.Secret ? " · secret" : "")}{(treaty.Sealed ? " · scellé par serment" : "")}{term}"
            });
            var breakIt = treaty.SpouseId != null ? new Button { Text = "Répudier le conjoint" }
                : treaty.Client ? new Button { Text = "Rompre la tutelle", TooltipText = "La secte fera la guerre au clan." }
                : new Button { Text = "Rompre" };
            breakIt.Pressed += () => Report(treaty.SpouseId != null ? root.Session.Matches.Repudiate(treaty.SpouseId) : root.Session.Treaties.Break(treaty.Id),
                treaty.SpouseId != null ? "Le conjoint retourne auprès des siens." : $"Le clan rompt le traité ({treaty.Kind}).");
            row.AddChild(breakIt);
            if (treaty.ClanIsSuzerain && root.Session.Treaties.All.FirstOrDefault(t => t.Id == treaty.Id)?.Faction is { } vassal)
            {
                string refusal = root.Session.Absorption.Refusal(vassal);
                var absorb = new Button { Text = "Absorber", Disabled = refusal != null, TooltipText = refusal ?? $"{vassal} rejoint le clan ; les autres puissances s'en méfieront." };
                absorb.Pressed += () => Report(root.Session.Absorption.Absorb(vassal), $"Le clan absorbe {vassal} : ses cultivateurs prennent son nom.");
                row.AddChild(absorb);
            }
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
            Add(proposal, $"Les aînés de {power} :"); // as the world knows them (the living world, 2026-10-01)
            foreach (var elder in DiplomacyView.Elders(session, power)) Add(proposal, $"  {elder}");
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
            if (root.SmokeEndsOnMirror) root.GoTo(MirrorScreen.ScenePath); // the mirror checks itself
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
