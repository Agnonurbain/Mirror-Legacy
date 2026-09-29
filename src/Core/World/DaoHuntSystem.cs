using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// A ripe Dao is prey (LORE.md §5.3.3; user decision 2026-09-29: a plot like the others). Only the powers of a higher
    /// realm hunt it, a treaty partner never; first they must learn it is ripe — a member away is seen, one in seclusion
    /// hardly; a year's rumour reaches one hunter at most. One who knows strikes in time (never the year it learns: a blow
    /// takes planning). The clan's defences may foil it: the hunter is then named in the chronicle and remembered, and
    /// withdraws — it must learn anew before it comes again. A Dao
    /// devoured dies, and nobody is named. What the hunters know stays hidden from the player (D7).
    /// </summary>
    public sealed class DaoHuntSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TreatySystem treaties;
        private readonly BuildingSystem buildings;
        private readonly List<DaoPrey> known = new List<DaoPrey>();

        public DaoHuntSystem(GameContext ctx, ClanManager clan, FactionManager factions, TreatySystem treaties, BuildingSystem buildings)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.treaties = treaties;
            this.buildings = buildings;
            ctx.Events.OnPowerAbsorbed += (vassal, _) => known.RemoveAll(k => k.Faction == vassal);
        }

        private DaoHuntSettings Settings => ctx.Content.Balance.DaoHunts;

        public IReadOnlyList<DaoPrey> Known => known;

        public void Restore(IEnumerable<DaoPrey> saved)
        {
            known.Clear();
            if (saved != null) known.AddRange(saved.Where(k => k != null).Distinct());
        }

        public void ProcessYear()
        {
            var preys = clan.LivingMembers.Where(m => m.CaptorFaction == null && FoundationRules.IsPrey(m, ctx.Content)).ToList();
            known.RemoveAll(k => preys.All(p => p.ID != k.MemberId)); // no longer ripe, or gone
            Strike(preys);
            Learn(preys.Where(p => p.IsAlive).ToList());
        }

        private IEnumerable<FactionData> Hunters() =>
            factions.Factions.Where(f => f.HighestRealm >= Settings.HunterMinRealm && !treaties.Spares(f.Name));

        /// <summary>A year's rumour of a ripe Dao reaches one hunter at most.</summary>
        private void Learn(IReadOnlyList<CharacterData> preys)
        {
            foreach (var prey in preys)
            {
                var ignorant = Hunters().Where(h => !known.Any(k => k.Faction == h.Name && k.MemberId == prey.ID)).ToList();
                if (ignorant.Count == 0 || !ctx.Rng.Chance(DaoHuntRules.LearnChance(prey, Settings))) continue;
                known.Add(new DaoPrey(ctx.Rng.Pick(ignorant).Name, prey.ID));
            }
        }

        private void Strike(IReadOnlyList<CharacterData> preys)
        {
            var s = Settings;
            bool guardian = clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.PurpleMansion);
            int patrols = clan.LivingMembers.Count(m => m.CaptorFaction == null && m.CurrentTask == TaskType.Patrol);
            bool ally = treaties.All.Any(t => t.Kind == TreatyKind.Defence);
            foreach (var prey in preys)
            {
                var hunter = known.Where(k => k.MemberId == prey.ID).Select(k => factions.GetFactionByName(k.Faction))
                    .FirstOrDefault(f => f != null && !treaties.Spares(f.Name) && ctx.Rng.Chance(s.StrikeChance));
                if (hunter == null) continue; // one blow a year at most
                double success = DaoHuntRules.StrikeSuccess(guardian, patrols, buildings.FormationLevel, ally,
                    prey.CurrentTask == TaskType.Seclusion, s, ctx.Content.Balance.RipeDaoGuardedFactor);
                if (ctx.Rng.Chance(success))
                {
                    ctx.Log.Info($"[DaoHunt] {prey.FullName}'s ripe Dao is harvested by a stronger cultivator.");
                    known.RemoveAll(k => k.MemberId == prey.ID);
                    clan.Kill(prey, DeathCause.RipeDaoHarvested);
                    continue;
                }
                ctx.Log.Warning($"[DaoHunt] {hunter.Name} strikes at {prey.FullName}'s ripe Dao, and is driven off.");
                known.RemoveAll(k => k.Faction == hunter.Name && k.MemberId == prey.ID); // it withdraws, and must learn anew
                ctx.Events.TriggerDaoHuntFoiled(hunter.Name);
            }
        }
    }
}
