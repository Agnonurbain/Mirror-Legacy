using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// The mirror's screen (G6, L2b): its power and seeds, its interventions with their cost and why not now, the
    /// Talisman Seed, the Judgment (confirmed twice: a life is taken) and the deduction of fragments. It only binds
    /// <see cref="MirrorView"/> and forwards the choices to the session.
    /// </summary>
    public partial class MirrorScreen : Control
    {
        public const string ScenePath = "res://scenes/MirrorScreen.tscn";

        private GameRoot root;
        private Label power;
        private VBoxContainer interventions;
        private VBoxContainer seeds;
        private VBoxContainer judgment;
        private VBoxContainer deduction;
        private VBoxContainer shards;
        private Label status;
        private readonly HashSet<string> chosenFragments = new HashSet<string>();
        private string judgmentArmed; // the member whose judgment awaits its confirmation

        public override void _Ready()
        {
            root = GetNode<GameRoot>("/root/GameRoot");
            if (root.RedirectWithoutSession(this)) return; // reached without a game
            power = GetNode<Label>("%Power");
            interventions = GetNode<VBoxContainer>("%Interventions");
            seeds = GetNode<VBoxContainer>("%Seeds");
            judgment = GetNode<VBoxContainer>("%Judgment");
            deduction = GetNode<VBoxContainer>("%Deduction");
            status = GetNode<Label>("%Status");
            GetNode<Button>("%Back").Pressed += () => root.GoTo(ClanDomain.ScenePath);
            // MIR_TAB=<0-3> opens a tab (screenshots of a smoke run)
            var tabs = GetNode<TabContainer>("%Tabs");
            var shardsTab = new ScrollContainer { Name = "Éclats" }; // B3e: the seven shards and the ways to bring them back
            shards = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            shardsTab.AddChild(shards);
            tabs.AddChild(shardsTab);
            if (int.TryParse(OS.GetEnvironment("MIR_TAB"), out int tab) && tab >= 0 && tab < tabs.GetTabCount()) tabs.CurrentTab = tab;
            Refresh();
            if (root.IsSmokeRun) Callable.From(RunSmoke).CallDeferred();
        }

        private void Refresh()
        {
            var header = MirrorView.Header(root.Session);
            power.Text = $"Clair de Lune du Yin Suprême {header.Power}/{header.MaxPower} (+{root.Session.Mirror.Tier.MoonlightPerYear}/an) · Graines de Sceau {header.Seeds}/{header.SeedCapacity}";
            var all = MirrorView.Interventions(root.Session);
            ShowInterventions(all);
            ShowSeeds();
            ShowJudgment(all.Single(i => i.Id == MirrorView.Judgment).Refusal);
            ShowDeduction();
            ShowShards();
        }

        /// <summary>The seven shards: where each lies as far as the clan knows, and the ways at hand to bring it back.</summary>
        private void ShowShards()
        {
            Clear(shards);
            Add(shards, ShardsView.Header(root.Session));
            foreach (var line in ShardsView.Lines(root.Session))
            {
                Add(shards, $"{char.ToUpper(line.Name[0])}{line.Name[1..]} — {line.State}"); // « le Jade du Lac » opens a line
                foreach (var action in line.Actions)
                {
                    var button = new Button { Text = action.Label, Disabled = action.Refusal != null, TooltipText = action.Refusal ?? "" };
                    button.Pressed += () => Take(line.Id, action);
                    shards.AddChild(button);
                }
            }
        }

        private void Take(string shardId, ShardActionLine action)
        {
            var s = root.Session;
            status.Text = action.Kind switch
            {
                ShardAction.Expedition => Told(s.Shards.Expedition(shardId, action.TeamIds), "L'expédition revient avec l'éclat.", "L'expédition revient les mains vides."),
                ShardAction.VoidSearch => Told(s.Shards.VoidSearch(action.TeamIds.FirstOrDefault()), "Le dernier éclat sort du Grand Vide.", "Le Grand Vide garde son éclat cette année."),
                ShardAction.Steal => s.PowerShards.Steal(shardId, action.TeamIds) is var theft && theft.Refusal != null ? theft.Refusal
                    : theft.Taken ? "L'éclat est volé sans que personne ne sache par qui."
                    : theft.Caught ? "Le voleur est pris : la puissance se demande ce que le clan cherchait." : "Le vol échoue, sans être vu.",
                ShardAction.Demand => s.PowerShards.DemandOfVassal(shardId) ?? "Le vassal livre l'éclat, non sans s'interroger.",
                ShardAction.Trade => s.PowerShards.Trade(shardId) ?? "L'éclat est échangé contre des pierres.",
                _ => ""
            };
            Refresh();
        }

        private static string Told(Mirror.ExpeditionOutcome outcome, string found, string missed) =>
            outcome.Refusal ?? (outcome.Found ? found : missed);

        private void ShowInterventions(IEnumerable<InterventionLine> all)
        {
            Clear(interventions);
            foreach (var line in all)
            {
                string text = $"{line.Name} — {line.Cost} de Clair de Lune — {line.Effect}{(line.Refusal == null ? "" : $" ({line.Refusal})")}";
                if (line.Id != MirrorView.Shield)
                {
                    Add(interventions, text); // the others are used from their own tab, or in battle
                    continue;
                }
                var row = new HBoxContainer();
                row.AddChild(Text(text));
                var use = new Button { Text = "Invoquer", Disabled = line.Refusal != null, TooltipText = line.Refusal ?? "" };
                use.Pressed += () =>
                {
                    status.Text = root.Session.Mirror.UseAncestralShield()
                        ? "Le Bouclier ancestral veille sur la prochaine percée." : "Le miroir n'a pu invoquer le bouclier.";
                    Refresh();
                };
                row.AddChild(use);
                interventions.AddChild(row);
            }
            ShowMoonlightGift();
        }

        /// <summary>The Supreme Yin Moonlight given to the clan (audit §3.7): a portion of Qi sealed, or a member nourished.</summary>
        private void ShowMoonlightGift()
        {
            var s = root.Session;
            var t = s.Context.Content.Balance.MirrorTiers;
            string refusal = s.Moonlight.Refusal();
            Add(interventions, $"Donner du Clair de Lune au clan ({t.GiftCost} chacun) — un Qi si rare ne passe pas inaperçu des espions :");
            var seal = new Button { Text = "Sceller une portion de Qi du Yin Suprême", Disabled = refusal != null, TooltipText = refusal ?? "" };
            seal.Pressed += () => { status.Text = s.Moonlight.SealQi() is { } r ? $"Refusé : {r}." : "Le miroir scelle une portion de Clair de Lune pour le clan."; Refresh(); };
            interventions.AddChild(seal);
            foreach (var m in s.Clan.LivingMembers.Where(m => m.CaptorFaction == null && MirrorChronicles.Characters.SpiritualOrificeRules.CanCultivate(m)).OrderByDescending(m => m.Realm).Take(8))
            {
                var nourish = new Button { Text = $"Nourrir la cultivation de {m.FullName}", Disabled = refusal != null, TooltipText = refusal ?? "" };
                nourish.Pressed += () => { status.Text = s.Moonlight.Nourish(m.ID) is { } r ? $"Refusé : {r}." : $"Le Clair de Lune nourrit {m.FullName}."; Refresh(); };
                interventions.AddChild(nourish);
            }
        }

        private void ShowSeeds()
        {
            Clear(seeds);
            var candidates = MirrorView.SeedCandidates(root.Session);
            if (candidates.Count == 0) Add(seeds, "Aucun mortel du clan n'attend de Graine de Sceau.");
            foreach (var candidate in candidates)
            {
                var row = new HBoxContainer();
                row.AddChild(Text($"{candidate.Name}, {candidate.Age} ans{(candidate.Refusal == null ? "" : $" ({candidate.Refusal})")}"));
                var plant = new Button { Text = "Planter la graine", Disabled = candidate.Refusal != null, TooltipText = candidate.Refusal ?? "" };
                plant.Pressed += () =>
                {
                    bool planted = root.Session.Mirror.GrantTalismanSeed(root.Session.Clan.FindById(candidate.Id));
                    status.Text = planted ? $"Une Graine de Sceau prend racine en {candidate.Name}." : "La graine n'a pu prendre racine.";
                    Refresh();
                };
                row.AddChild(plant);
                seeds.AddChild(row);
            }
        }

        private void ShowJudgment(string refusal)
        {
            Clear(judgment);
            if (refusal != null) Add(judgment, $"Le jugement est hors de portée : {refusal}.");
            foreach (var target in MirrorView.JudgmentTargets(root.Session))
            {
                var row = new HBoxContainer();
                string effect = target.Effect == "tue" ? " — la Lumière tuerait" : target.Effect == "blesse" ? " — la Lumière blesserait" : "";
                row.AddChild(Text($"{target.Name} — {target.Realm}{(target.Origin == null ? "" : $" — {target.Origin}")}{effect}"));
                var strike = new Button { Text = judgmentArmed == target.Id ? "Confirmer le jugement" : "Juger",
                    Disabled = refusal != null || target.Refusal != null, TooltipText = target.Refusal ?? "" };
                strike.Pressed += () => Judge(target);
                row.AddChild(strike);
                judgment.AddChild(row);
            }
        }

        private void Judge(JudgmentTarget target)
        {
            if (judgmentArmed != target.Id)
            {
                judgmentArmed = target.Id;
                status.Text = $"Juger {target.Name} ? Sa vie sera prise. Confirmez.";
            }
            else
            {
                judgmentArmed = null;
                bool struck = target.Power == null
                    ? root.Session.Mirror.UseMirrorJudgment(root.Session.Clan.FindById(target.Id))
                    : root.Session.Light.StrikeElder(target.Power, target.Id) == null;
                status.Text = !struck ? "Le jugement n'a pu tomber." : target.Effect == "tue" ? $"Le jugement tombe sur {target.Name}." : $"La Lumière blesse {target.Name}.";
            }
            Refresh();
        }

        private void ShowDeduction()
        {
            Clear(deduction);
            string ascentRefusal = root.Session.Deduction.AscentRefusal(); // LORE.md §11.10: from three shards, of its own will
            var ascent = new Button { Text = "Déduire une méthode du Manoir Pourpre", Disabled = ascentRefusal != null, TooltipText = ascentRefusal ?? "" };
            ascent.Pressed += () =>
            {
                var method = root.Session.Deduction.DeduceAscentMethod();
                status.Text = method == null ? "Le miroir n'a pu déduire la méthode." : $"Le miroir déduit le {method.Name} : une méthode qui mène au Manoir Pourpre.";
                Refresh();
            };
            deduction.AddChild(ascent);
            foreach (var s in root.Session.Sponsorships.Active.Where(x => !x.Cleansed))
            {
                string method = root.Session.Context.Content.Techniques.FirstOrDefault(t => t.ID == s.TechniqueId)?.Name ?? s.TechniqueId;
                var cleanse = new Button { Text = $"Nettoyer le {method}, don de {s.Power}" };
                cleanse.Pressed += () => { status.Text = root.Session.Sponsorships.Cleanse(s.Id) ?? "Le miroir lave le manuel de ce qu'y avait laissé son donateur."; Refresh(); };
                deduction.AddChild(cleanse);
            }
            var fragments = MirrorView.Fragments(root.Session);
            chosenFragments.IntersectWith(fragments.Select(f => f.Id)); // those spent are gone
            if (fragments.Count == 0)
            {
                Add(deduction, "Le miroir ne détient aucun fragment.");
                return;
            }
            foreach (var fragment in fragments)
            {
                var pick = new CheckBox
                {
                    Text = $"{fragment.Name} — {fragment.Element}, qualité {fragment.Quality}",
                    ButtonPressed = chosenFragments.Contains(fragment.Id)
                };
                pick.Toggled += on =>
                {
                    if (on) chosenFragments.Add(fragment.Id); else chosenFragments.Remove(fragment.Id);
                    Callable.From(Refresh).CallDeferred(); // not while the box is still handling its toggle
                };
                deduction.AddChild(pick);
            }
            var preview = MirrorView.DeductionPreview(root.Session, chosenFragments.ToList());
            var row = new HBoxContainer();
            row.AddChild(Text(preview.Refusal == null ? $"Coût : {preview.Cost} de Clair de Lune" : $"({preview.Refusal})"));
            var deduce = new Button { Text = "Déduire", Disabled = preview.Refusal != null };
            deduce.Pressed += () =>
            {
                var inputs = root.Session.Deduction.Fragments.Where(f => chosenFragments.Contains(f.ID)).ToList();
                var technique = root.Session.Deduction.AttemptDeduction(inputs);
                chosenFragments.Clear();
                status.Text = technique != null ? $"Le miroir déduit « {technique.Name} »." : "La déduction a échoué.";
                Refresh();
            };
            row.AddChild(deduce);
            deduction.AddChild(row);
        }

        private static Label Text(string text) =>
            new Label { Text = text, CustomMinimumSize = new Vector2(620, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart };

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
            var header = MirrorView.Header(root.Session);
            GD.Print($"[Smoke] Mirror: power {header.Power}, {MirrorView.SeedCandidates(root.Session).Count} seed candidates, {MirrorView.Fragments(root.Session).Count} fragments.");
            if (root.SmokeEndsOnBuildings) root.GoTo(BuildingsScreen.ScenePath); // the buildings check themselves
            else if (root.ScreenshotPath != null) Screenshot.CaptureAndQuit(this, root.ScreenshotPath);
            else GetTree().Quit();
        }
    }
}
