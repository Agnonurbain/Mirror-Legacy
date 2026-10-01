using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The Fruitions of the world (the living world, step D, user decisions 2026-10-01). The world's holders pass — death,
    /// or the Struggle of the Five Faces — save the eternal; a holder who is a power's elder passes with him. One may be
    /// reborn (LORE.md §5.5.2): its lineage is free meanwhile, and its own again only if nobody took it. A power's elder
    /// risen to the Golden Core asks Heaven for a free Realization. A lineage freed opens a race: a power's Grand
    /// Perfection dares sooner, and the clan may sabotage a contender — at the risk of being caught. The restoration of
    /// broken lineages is paused (the user's choice).
    /// </summary>
    public sealed class WorldFruitions
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly FruitionRegistry registry;
        private readonly ElderSystem elders;
        private readonly SuspicionLedger suspicion;
        private readonly List<FruitionRace> races = new List<FruitionRace>();

        public WorldFruitions(GameContext ctx, ClanManager clan, FactionManager factions, FruitionRegistry registry, ElderSystem elders,
            SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.registry = registry;
            this.elders = elders;
            this.suspicion = suspicion;
            ctx.Events.OnElderDied += (power, elder, demon) => { if (elder.FruitionId != null) Pass(elder.FruitionId, elder.Name); };
            ctx.Events.OnElderRose += AskForAPosition;
        }

        private WorldFruitionSettings Settings => ctx.Content.Balance.WorldFruitions;

        public IReadOnlyList<FruitionRace> Races => races;

        public void RestoreRaces(IEnumerable<FruitionRace> saved)
        {
            races.Clear();
            if (saved != null) races.AddRange(saved);
        }

        /// <summary>The powers' elders who hold a Realization at the world's start are bound to it (a figure such as the Venerable Lingxu).</summary>
        public void Link()
        {
            foreach (var (id, state) in registry.States.Where(p => p.Value.Status == FruitionStatus.Occupied))
                foreach (var elder in factions.Factions.SelectMany(f => f.Elders).Where(e => e.Name == state.Holder && e.FruitionId == null))
                    elder.FruitionId = id;
        }

        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            races.RemoveAll(r => r.UntilYear < year || registry.State(r.FruitionId)?.Status != FruitionStatus.Free);
            foreach (var (id, state) in registry.States.ToList())
            {
                if (state.Status == FruitionStatus.Free && state.ReturningHolder != null && state.ReturnYear <= year) Return(id, state);
                else if (state.Status == FruitionStatus.Occupied && IsTheWorlds(id, state.Holder) && ctx.Rng.Chance(Settings.HolderPassChance))
                    Pass(id, state.Holder);
            }
            Race();
        }

        /// <summary>A lineage the world's own holder keeps: not the clan's, not an elder's (he passes with the elder), not eternal.</summary>
        private bool IsTheWorlds(string fruitionId, string holder) =>
            !Settings.Eternal.Contains(fruitionId)
            && !clan.LivingMembers.Any(m => m.FullName == holder)
            && !factions.Factions.SelectMany(f => f.Elders).Any(e => e.FruitionId == fruitionId);

        private void Pass(string fruitionId, string holder)
        {
            bool reborn = ctx.Rng.Chance(Settings.ReincarnationChance);
            registry.Vacate(fruitionId, reborn ? holder : null, reborn ? ctx.Clock.Year + Settings.ReturnYears : null);
            OpenRace(fruitionId);
            ctx.Log.Warning($"[Fruitions] {holder} no longer holds {fruitionId}{(reborn ? "; reborn, he may come back" : "")}: the race is open.");
            ctx.Events.TriggerFruitionFreed(fruitionId, holder, reborn);
        }

        private void Return(string fruitionId, FruitionState state)
        {
            registry.Claim(fruitionId, state.ReturningHolder);
            ctx.Log.Info($"[Fruitions] {state.ReturningHolder}, reborn, takes {fruitionId} back.");
            ctx.Events.TriggerFruitionTaken(fruitionId, state.ReturningHolder);
        }

        /// <summary>A race opens on a free lineage for some years (or opens anew).</summary>
        public void OpenRace(string fruitionId)
        {
            races.RemoveAll(r => r.FruitionId == fruitionId);
            races.Add(new FruitionRace(fruitionId, ctx.Clock.Year + Settings.RaceYears));
        }

        /// <summary>A power's elder risen to the Golden Core asks Heaven for a free Realization, a raced one first.</summary>
        private void AskForAPosition(FactionData power, FactionElder elder)
        {
            if (elder.Realm < CultivationRealm.GoldenCore || elder.FruitionId != null) return;
            var free = registry.States.Where(p => p.Value.Status == FruitionStatus.Free).Select(p => p.Key)
                .OrderBy(id => races.Any(r => r.FruitionId == id) ? 0 : 1).ThenBy(id => id, StringComparer.Ordinal).ToList();
            if (free.Count == 0) return; // a True Monarch without position: it waits
            string chosen = races.Any(r => r.FruitionId == free[0]) ? free[0] : free[ctx.Rng.Next(free.Count)];
            if (!registry.Claim(chosen, elder.Name)) return;
            elder.FruitionId = chosen;
            races.RemoveAll(r => r.FruitionId == chosen);
            ctx.Log.Info($"[Fruitions] {elder.Name} of {power.Name} takes the Realization of {chosen}.");
            ctx.Events.TriggerFruitionTaken(chosen, elder.Name);
        }

        /// <summary>While a race is open, each power's best Grand Perfection dares from the race's lower bar.</summary>
        private void Race()
        {
            if (races.Count == 0) return;
            foreach (var power in factions.Factions.ToList())
            {
                var contender = Contender(power);
                if (contender != null) elders.Dare(power, contender, Settings.RaceOdds);
                if (races.Count == 0) return;
            }
        }

        private static FactionElder Contender(FactionData power) =>
            power.Elders.Where(e => e.Realm == CultivationRealm.PurpleMansion && e.Perfected).OrderByDescending(e => e.GoldenCoreOdds).FirstOrDefault();

        // ---- The clan's plot: a contender's preparation spoilt ----

        /// <summary>The odds of a sabotage on this power's contender with this team (0 without one).</summary>
        public double SabotageChance(string powerName, IReadOnlyList<string> teamIds)
        {
            var power = factions.GetFactionByName(powerName);
            var team = (teamIds ?? new List<string>()).Select(clan.FindById).Where(m => m != null).ToList();
            if (power == null || team.Count == 0) return 0;
            var s = Settings;
            var powers = team.Select(m => (double)HuntRules.Power(m.Realm, m.RealmStage)).OrderByDescending(p => p).ToList();
            double strength = powers[0] + powers.Skip(1).Sum() * 0.3;
            return Math.Clamp(s.SabotageBaseChance + (strength - HuntRules.Power(power.HighestRealm, 5)) * s.SabotageChancePerPower,
                s.SabotageMinChance, s.SabotageMaxChance);
        }

        /// <summary>
        /// The clan spoils a contender's preparation in a race: its odds of the Golden Core fall. Caught, the clan leaves proof
        /// and a grudge. Null when done; else why not, or « le sabotage échoue » (French).
        /// </summary>
        public string Sabotage(string powerName, IReadOnlyList<string> teamIds)
        {
            var power = factions.GetFactionByName(powerName);
            if (races.Count == 0) return "aucune lignée n'est en course";
            var contender = power == null ? null : Contender(power);
            if (contender == null) return "cette puissance n'a personne en course";
            var team = (teamIds ?? new List<string>()).Distinct().Select(clan.FindById).ToList();
            if (team.Count == 0 || team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null || m.Realm < CultivationRealm.QiRefinement
                || m.LastOperationYear == ctx.Clock.Year))
                return "un membre de l'équipe ne peut partir";
            double chance = SabotageChance(powerName, team.Select(m => m.ID).ToList());
            foreach (var m in team) m.LastOperationYear = ctx.Clock.Year;
            if (ctx.Rng.Chance(chance))
            {
                contender.GoldenCoreOdds = Math.Max(0, contender.GoldenCoreOdds - Settings.SabotageOddsLoss);
                ctx.Log.Info($"[Fruitions] The clan spoils {contender.Name}'s preparation, of {power.Name}.");
                return null;
            }
            suspicion.AddEvidence(power.Name, Settings.SabotageCaughtEvidence);
            factions.ChangeRelation(power.ID, Settings.SabotageCaughtRelation);
            ctx.Log.Warning($"[Fruitions] The clan's sabotage of {power.Name} fails, and is seen.");
            return "le sabotage échoue, et le clan est vu";
        }
    }
}
