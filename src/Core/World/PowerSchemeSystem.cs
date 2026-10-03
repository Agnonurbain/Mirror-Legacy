using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers scheme against each other (the user's rule, 2026-10-03: what befalls the clan befalls the world). Each year
    /// a power — more often an aggressive or manipulative one — may turn on a neighbour no stronger than itself and not its
    /// ally nor bound to it: it steals (an artifact, else a copy of a technique, else wealth; caught, the victim feuds back),
    /// ambushes one of its elders for a ransom (unpaid in time, the captive is put to death), or harvests its ripe Dao — a
    /// peak Foundation, killed by a stronger power.
    /// </summary>
    public sealed class PowerSchemeSystem
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly PowerPoliticsSystem politics;
        private readonly List<PowerCaptive> captives = new List<PowerCaptive>();

        public PowerSchemeSystem(GameContext ctx, FactionManager factions, PowerPoliticsSystem politics)
        {
            this.ctx = ctx;
            this.factions = factions;
            this.politics = politics;
        }

        private PowerSchemeSettings Settings => ctx.Content.Balance.PowerSchemes;

        public IReadOnlyList<PowerCaptive> Captives => captives;

        public void Restore(IEnumerable<PowerCaptive> saved)
        {
            captives.Clear();
            if (saved != null) captives.AddRange(saved);
        }

        /// <summary>A neighbour no stronger than itself, neither its ally nor bound to it by vassalage.</summary>
        public bool MayScheme(FactionData thief, FactionData victim) =>
            thief != null && victim != null && thief != victim && victim.HighestRealm <= thief.HighestRealm
            && (thief.RegionId == victim.RegionId || factions.AreNeighbours(thief, victim))
            && !politics.AlliesOf(thief.Name).Contains(victim.Name)
            && politics.SuzerainOf(victim.Name) != thief.Name && politics.SuzerainOf(thief.Name) != victim.Name;

        public void ProcessYear()
        {
            Ransoms();
            foreach (var thief in factions.Factions.OrderBy(_ => ctx.Rng.Next()).ToList())
            {
                double factor = Settings.PersonalityFactor.TryGetValue(thief.Personality, out var f) ? f : 1.0;
                if (!ctx.Rng.Chance(Settings.SchemeChance * factor)) continue;
                var targets = factions.Factions.Where(v => MayScheme(thief, v)).ToList();
                if (targets.Count == 0) continue;
                var victim = targets[ctx.Rng.Next(targets.Count)];
                double roll = ctx.Rng.NextDouble();
                if (roll < Settings.TheftWeight) Steal(thief, victim);
                else if (roll < Settings.TheftWeight + Settings.AmbushWeight) Ambush(thief, victim);
                else Harvest(thief, victim);
            }
        }

        /// <summary>A theft: an artifact first, else a copy of a technique, else wealth; caught, the victim feuds back.</summary>
        public void Steal(FactionData thief, FactionData victim)
        {
            string taken;
            var artifact = victim.Artifacts.Where(a => a.LentBy != ArtifactTrade.ClanLender).OrderByDescending(a => a.Rank).FirstOrDefault();
            var art = victim.Techniques.FirstOrDefault(t => !thief.Techniques.Contains(t));
            if (artifact != null)
            {
                victim.Artifacts.Remove(artifact);
                thief.Artifacts.Add(artifact);
                taken = artifact.Name;
            }
            else if (art != null)
            {
                thief.Techniques.Add(art); // a copy: the victim keeps its own
                taken = $"une copie de {art}";
            }
            else
            {
                int wealth = (int)(System.Math.Max(0, victim.Wealth) * Settings.TheftWealthShare);
                victim.Wealth -= wealth;
                thief.Wealth += wealth;
                taken = $"{wealth} pierres";
            }
            ctx.Log.Info($"[PowerSchemes] {thief.Name} steals from {victim.Name}: {taken}.");
            ctx.Events.TriggerPowerScheme("vol", thief.Name, victim.Name);
            if (ctx.Rng.Chance(Settings.CaughtChance)) politics.Feud(victim, thief); // caught: it strikes back
        }

        /// <summary>An ambush on an elder below the Golden Core the thief can master; false when none is within reach.</summary>
        public bool Ambush(FactionData thief, FactionData victim)
        {
            var prey = victim.Elders.Where(e => e.Realm < CultivationRealm.GoldenCore && e.Realm <= thief.HighestRealm).ToList();
            if (prey.Count == 0) return false;
            var elder = prey[ctx.Rng.Next(prey.Count)];
            victim.Elders.Remove(elder);
            ElderSystem.Sync(victim);
            captives.Add(new PowerCaptive(thief.Name, victim.Name, elder, ctx.Clock.Year));
            ctx.Log.Info($"[PowerSchemes] {thief.Name} takes {elder.Name} of {victim.Name}, for a ransom.");
            ctx.Events.TriggerPowerScheme("enlèvement", thief.Name, victim.Name);
            return true;
        }

        /// <summary>A ripe Dao — a peak Foundation — harvested by a stronger power; false when none is ripe.</summary>
        public bool Harvest(FactionData thief, FactionData victim)
        {
            if (thief.HighestRealm <= CultivationRealm.Foundation) return false;
            var ripe = victim.Elders.FirstOrDefault(e => e.Realm == CultivationRealm.Foundation && e.Stage >= PowerLadder.StageCount(CultivationRealm.Foundation));
            if (ripe == null) return false;
            victim.Elders.Remove(ripe);
            ElderSystem.Sync(victim);
            thief.PowerLevel += Settings.HarvestPowerGain;
            ctx.Log.Warning($"[PowerSchemes] {thief.Name} harvests {ripe.Name}'s ripe Dao, of {victim.Name}.");
            ctx.Events.TriggerElderDied(victim, ripe, false);
            ctx.Events.TriggerPowerScheme("moisson d'un Dao mûr", thief.Name, victim.Name);
            return true;
        }

        /// <summary>A captive's power pays its ransom when it can; unpaid in time, the captive is put to death.</summary>
        private void Ransoms()
        {
            int year = ctx.Clock.Year;
            foreach (var c in captives.Where(c => c.Year < year).ToList())
            {
                var victim = factions.GetFactionByName(c.Victim);
                var captor = factions.GetFactionByName(c.Captor);
                int ransom = Settings.RansomPerRealm * System.Math.Max(1, (int)c.Elder.Realm);
                if (victim != null && victim.Wealth >= ransom)
                {
                    victim.Wealth -= ransom;
                    if (captor != null) captor.Wealth += ransom;
                    victim.Elders.Add(c.Elder);
                    ElderSystem.Sync(victim);
                    captives.Remove(c);
                    ctx.Log.Info($"[PowerSchemes] {c.Victim} ransoms {c.Elder.Name} from {c.Captor}.");
                    continue;
                }
                if (victim != null && year - c.Year <= Settings.CaptiveYears) continue;
                captives.Remove(c);
                ctx.Log.Warning($"[PowerSchemes] {c.Captor} puts {c.Elder.Name} of {c.Victim} to death.");
                if ((victim ?? captor) is { } where) ctx.Events.TriggerElderDied(where, c.Elder, false); // both gone: no one is left to mourn it
            }
        }
    }
}
