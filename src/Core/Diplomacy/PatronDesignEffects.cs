using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// What a patron's design does when it falls due (LORE.md §11.10, C; first wave, 2026-10-01; 🔎 balance.json
    /// « patronDesigns »). The harvest binds the clan as a vassal of a stronger patron (📚 the sect that reaps its clans), or
    /// turns a weaker one against it; the refining consumes the practitioner's foundation (📚 the master who refines the
    /// disciples whose foundation suits him); the mark lets the patron see into the clan; the hidden flaw wounds and caps the
    /// practitioner; the hostage takes the clan's most gifted child; the sincere patron forgives its debt and gives. Second wave
    /// (2026-10-01): every other design of designs.json — see <see cref="Apply"/>.
    /// </summary>
    public sealed class PatronDesignEffects
    {
        private const int ChildhoodYears = 16;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TreatySystem treaties;
        private readonly WarSystem wars;
        private readonly SuspicionLedger suspicion;
        private readonly WoundSystem wounds;
        private readonly KnowledgeAccords accords;
        private readonly ResourceManager resources;
        private readonly MirrorLore lore;
        private readonly PatronSystem patrons;

        private static readonly System.Collections.Generic.HashSet<string> Designs = new System.Collections.Generic.HashSet<string>
        {
            "harvest", "refining", "human-pills", "vessel", "fated-one", "fruition-vassal", "borrowing-pyramid", "mark", "hidden-flaw",
            "pawn", "scapegoat", "bulwark", "atmosphere", "imperial-blood", "vacant-fruition", "intercalary-bridge", "underworld-essence",
            "demon-to-feed", "void-key", "mirror-bait", "broken-pact", "heir-hostage", "sincere"
        };

        /// <summary>Whether a design has its consequence here.</summary>
        public static bool Handles(string designId) => Designs.Contains(designId);

        public PatronDesignEffects(GameContext ctx, ClanManager clan, FactionManager factions, TreatySystem treaties, WarSystem wars,
            SuspicionLedger suspicion, WoundSystem wounds, KnowledgeAccords accords, ResourceManager resources, MirrorLore lore, PatronSystem patrons)
        {
            this.lore = lore;
            this.patrons = patrons;
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.treaties = treaties;
            this.wars = wars;
            this.suspicion = suspicion;
            this.wounds = wounds;
            this.accords = accords;
            this.resources = resources;
            ctx.Events.OnPatronDesignDue += Apply;
        }

        private PatronDesignSettings Settings => ctx.Content.Balance.PatronDesigns;

        /// <summary>The clan member furthest along the patron's method.</summary>
        private CharacterData Practitioner(Sponsorship s) =>
            clan.LivingMembers.Where(m => m.CultivationMethodId == s.TechniqueId)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).FirstOrDefault();

        private void Apply(Sponsorship s, PatronDesign design)
        {
            var patron = factions.GetFactionByName(s.Power);
            if (patron == null) return; // the patron is no more: its design dies with it
            switch (design.Id)
            {
                case "harvest": Harvest(patron); break;
                case "refining":
                    if (Practitioner(s) is { } refined) clan.Kill(refined, DeathCause.FoundationDevoured);
                    break;
                case "mark": suspicion.AddMirrorClues(patron.Name, Settings.MarkClues); break;
                case "hidden-flaw":
                    if (Practitioner(s) is { } flawed)
                    {
                        for (int i = 0; i < Settings.FlawWounds; i++) wounds.ApplyDaoWound(flawed);
                        flawed.ProgressionSealed = true;
                    }
                    break;
                case "heir-hostage":
                    if (MostGiftedChild() is { } gifted) clan.Depart(gifted); // gone to the patron, a lever for ever
                    break;
                case "sincere":
                    accords.ForgiveDebts(patron.Name);
                    resources.AddSpiritStones(Settings.SincereGift);
                    break;
                // ---- Second wave ----
                case "human-pills":
                    foreach (var taken in clan.LivingMembers.Where(m => m.Realm >= CultivationRealm.QiRefinement && m.ID != clan.PatriarchID && m.CaptorFaction == null)
                        .OrderBy(m => m.SpiritualRoot).Take(Settings.HumanPillsTaken).ToList())
                        clan.Kill(taken, DeathCause.Assassination); // they vanish: ingredients of a longer life
                    break;
                case "vessel":
                    if (Practitioner(s) is { } vessel) clan.Kill(vessel, DeathCause.SoulReplaced);
                    break;
                case "fated-one":
                    if (MostGiftedChild() is { } fated) fated.SpiritualRoot = (int)(fated.SpiritualRoot * Settings.FatedRootShare); // the destiny reaped
                    break;
                case "fruition-vassal":
                    if (Practitioner(s) is { } bound) bound.ProgressionSealed = true; // no step further without the patron's leave
                    break;
                case "borrowing-pyramid":
                    if (Practitioner(s) is { } lent)
                    {
                        lent.MentalStability = System.Math.Max(0, lent.MentalStability - Settings.PyramidStability);
                        suspicion.AddMirrorClues(patron.Name, Settings.MarkClues); // the soul it holds tells it what it saw
                    }
                    break;
                case "pawn":
                    var turned = factions.Factions.Where(f => f.Name != patron.Name).OrderByDescending(f => f.PowerLevel).FirstOrDefault();
                    if (turned != null)
                    {
                        suspicion.AddToClan(turned.Name, Settings.PawnSuspicion);
                        suspicion.AddDistrust(turned.Name, SecretBook.ClanHolder, Settings.PawnSuspicion);
                    }
                    break;
                case "scapegoat":
                    foreach (var f in factions.Factions.Where(f => f.Name != patron.Name)) suspicion.AddToClan(f.Name, Settings.ScapegoatSuspicion);
                    break;
                case "bulwark":
                    var guard = clan.LivingMembers.Where(m => m.CaptorFaction == null).OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).FirstOrDefault();
                    if (guard != null) wounds.ApplyDaoWound(guard);
                    resources.AddPrestige(Settings.BulwarkPrestige);
                    break;
                case "atmosphere":
                    resources.ConsumeSpiritStones((int)(resources.SpiritStones * Settings.AtmosphereStonesShare));
                    break;
                case "imperial-blood":
                    foreach (var state in factions.Factions.Where(f => f.Kind == FactionKind.State)) suspicion.AddToClan(state.Name, Settings.ImperialSuspicion);
                    break;
                case "vacant-fruition":
                    BindAsVassal(patron);
                    break;
                case "intercalary-bridge":
                    if (Practitioner(s) is { DivineAbilities.Count: > 0 } bridge) bridge.DivineAbilities.RemoveAt(bridge.DivineAbilities.Count - 1);
                    break;
                case "underworld-essence":
                case "demon-to-feed":
                    factions.ChangeRelation(patron.ID, -Settings.HarvestRelationLoss); // the ascent succeeded: the plot failed
                    break;
                case "void-key":
                    if (Practitioner(s) is { } opener)
                    {
                        if (ctx.Rng.Chance(Settings.VoidKeyDeathChance)) clan.Kill(opener, DeathCause.Combat);
                        else resources.AddSpiritStones(Settings.VoidKeyTreasure);
                    }
                    break;
                case "mirror-bait":
                    int doubt = ctx.Content.Balance.Plots.DoubtClues;
                    suspicion.AddMirrorClues(patron.Name, lore.Knows(patron.Name) ? System.Math.Max(0, doubt - suspicion.MirrorClues(patron.Name)) : Settings.MarkClues);
                    break;
                case "broken-pact":
                    patrons.End("qingyan-vixen");
                    break;
            }
            ctx.Log.Info($"[Patrons] {patron.Name}'s design « {design.Id} » takes effect.");
        }

        private CharacterData MostGiftedChild() =>
            clan.LivingMembers.Where(m => m.Age < ChildhoodYears && m.CaptorFaction == null && m.ID != clan.PatriarchID)
                .OrderByDescending(m => m.SpiritualRoot).FirstOrDefault();

        /// <summary>A stronger patron reaps the clan as its vassal; a weaker one can only resent it.</summary>
        private void Harvest(FactionData patron)
        {
            if (WarRules.Strength(patron, ctx.Content.Balance.Wars) > wars.ClanWarStrength())
            {
                BindAsVassal(patron);
                return;
            }
            factions.ChangeRelation(patron.ID, -Settings.HarvestRelationLoss);
        }

        private void BindAsVassal(FactionData patron) =>
            treaties.Conclude(new Treaty($"design-{patron.Name}-{ctx.Clock.Year}", TreatyKind.Vassalage, patron.Name, ctx.Clock.Year, null,
                false, false, false));
    }
}
