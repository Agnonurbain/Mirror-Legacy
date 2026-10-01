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
    /// practitioner; the hostage takes the clan's most gifted child; the sincere patron forgives its debt and gives. The other
    /// designs only announce themselves for now.
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

        public PatronDesignEffects(GameContext ctx, ClanManager clan, FactionManager factions, TreatySystem treaties, WarSystem wars,
            SuspicionLedger suspicion, WoundSystem wounds, KnowledgeAccords accords, ResourceManager resources)
        {
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
                    var gifted = clan.LivingMembers.Where(m => m.Age < ChildhoodYears && m.CaptorFaction == null && m.ID != clan.PatriarchID)
                        .OrderByDescending(m => m.SpiritualRoot).FirstOrDefault();
                    if (gifted != null) clan.Depart(gifted); // gone to the patron, a lever for ever
                    break;
                case "sincere":
                    accords.ForgiveDebts(patron.Name);
                    resources.AddSpiritStones(Settings.SincereGift);
                    break;
            }
            ctx.Log.Info($"[Patrons] {patron.Name}'s design « {design.Id} » takes effect.");
        }

        /// <summary>A stronger patron reaps the clan as its vassal; a weaker one can only resent it.</summary>
        private void Harvest(FactionData patron)
        {
            if (WarRules.Strength(patron, ctx.Content.Balance.Wars) > wars.ClanWarStrength())
            {
                treaties.Conclude(new Treaty($"harvest-{patron.Name}-{ctx.Clock.Year}", TreatyKind.Vassalage, patron.Name, ctx.Clock.Year, null,
                    false, false, false));
                return;
            }
            factions.ChangeRelation(patron.ID, -Settings.HarvestRelationLoss);
        }
    }
}
