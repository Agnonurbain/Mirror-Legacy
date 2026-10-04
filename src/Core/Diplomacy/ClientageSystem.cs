using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// The clan as a sect's client (AUDIT_LORE.md §4.1, §5.1; the user's decisions 2026-10-04; 📚 wiki: the sect treats its
    /// families « as harvests »): a small tribute, never absorbed; the sect takes the most gifted child born with an orifice as
    /// its disciple (it never sees the mirror's seed — but a seeded cultivator in the open draws its eye); it raises levies for
    /// the frontier. Breaking free means war.
    /// </summary>
    public sealed class ClientageSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly SuspicionLedger suspicion;

        public ClientageSystem(GameContext ctx, ClanManager clan, ResourceManager resources, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.suspicion = suspicion;
        }

        private ClientageSettings Settings => ctx.Content.Balance.Clientage;

        /// <summary>The year's dues to the sect.</summary>
        public void Serve(FactionData sect)
        {
            var s = Settings;
            int tribute = (int)(resources.SpiritStones * s.TributeShare);
            resources.ConsumeSpiritStones(tribute);
            sect.Wealth += tribute;
            if (ctx.Rng.Chance(s.SelectionChance)) Select(sect);
            if (ctx.Rng.Chance(s.LevyChance)) Levy(sect);
            bool seededInTheOpen = clan.LivingMembers.Any(m => m.HasTalismanSeed && !m.HasSpiritualOrifice && m.CaptorFaction == null
                && (m.Realm > CultivationRealm.Embryonic || m.RealmStage > 0));
            if (seededInTheOpen && ctx.Rng.Chance(s.SeededClueChance))
                suspicion.AddMirrorClues(sect.Name, s.SeededClues); // a mortal who cultivates without an orifice: how?
        }

        /// <summary>The sect tests the children: the most gifted born with an orifice leaves as its disciple (📚 « their best talents »).</summary>
        private void Select(FactionData sect)
        {
            var s = Settings;
            var child = clan.LivingMembers.Where(m => m.HasSpiritualOrifice && m.Age >= s.SelectionMinAge && m.Age <= s.SelectionMaxAge
                    && m.CaptorFaction == null && m.DiscipleOf == null && !m.Departed && m.ID != clan.PatriarchID)
                .OrderByDescending(m => m.SpiritualRoot).FirstOrDefault();
            if (child == null) return;
            child.DiscipleOf = sect.Name;
            child.DiscipleUntil = ctx.Clock.Year + s.DiscipleYears;
            child.CurrentTask = TaskType.None;
            ctx.Log.Warning($"[Clientage] {sect.Name} takes {child.FullName} as its disciple.");
        }

        /// <summary>A levy for the frontier: a cultivator serves the year, and may not come back.</summary>
        private void Levy(FactionData sect)
        {
            var soldier = clan.LivingMembers.Where(m => m.Realm >= CultivationRealm.QiRefinement && m.CaptorFaction == null
                    && m.DiscipleOf == null && m.ID != clan.PatriarchID && m.LastOperationYear != ctx.Clock.Year)
                .OrderBy(m => (int)m.Realm).ThenBy(m => m.SpiritualRoot).FirstOrDefault(); // the least the clan can spare
            if (soldier == null) return;
            soldier.LastOperationYear = ctx.Clock.Year;
            ctx.Log.Info($"[Clientage] {soldier.FullName} serves a year at {sect.Name}'s frontier.");
            if (ctx.Rng.Chance(Settings.LevyDeathChance)) clan.Kill(soldier, DeathCause.Combat);
        }
    }
}
