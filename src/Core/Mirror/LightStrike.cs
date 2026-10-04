using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>A power's elder the Light could strike: the power, the elder, what the Light would do to it.</summary>
    public sealed record ElderTarget(string Power, string ElderId, string ElderName, CultivationRealm Realm, string Effect);

    /// <summary>
    /// The Supreme Yin Profound Light against the clan's enemies (📚 the wolf demon, the intruders; AUDIT_LORE.md §3.2, the
    /// user's decision 2026-10-04): an elder of a power within the mirror's perception, killed within its tier or wounded a
    /// realm above — its life shortened. Its power wonders what struck it: clues toward the hidden treasure.
    /// </summary>
    public sealed class LightStrike
    {
        private readonly GameContext ctx;
        private readonly MirrorSystem mirror;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;

        public LightStrike(GameContext ctx, MirrorSystem mirror, FactionManager factions, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.mirror = mirror;
            this.factions = factions;
            this.suspicion = suspicion;
        }

        private MirrorTierSettings Settings => ctx.Content.Balance.MirrorTiers;

        /// <summary>The powers' elders within the mirror's perception and the Light's reach.</summary>
        public IReadOnlyList<ElderTarget> ElderTargets()
        {
            var tier = mirror.Tier;
            return factions.Factions.Where(p => mirror.Perceives(p.RegionId))
                .SelectMany(p => p.Elders.Select(e => new ElderTarget(p.Name, e.Id, e.Name, e.Realm, MirrorTiers.Effect(tier, e.Realm))))
                .Where(t => t.Effect != null).ToList();
        }

        /// <summary>Strikes a power's elder. Null when done; else why not (French).</summary>
        public string StrikeElder(string powerName, string elderId)
        {
            var power = factions.GetFactionByName(powerName);
            var elder = power?.Elders.FirstOrDefault(e => e.Id == elderId);
            if (elder == null) return "cible introuvable";
            if (!mirror.Perceives(power.RegionId)) return $"{power.Name} est hors de la perception du miroir";
            string effect = MirrorTiers.Effect(mirror.Tier, elder.Realm);
            if (effect == null) return "la Lumière ne l'atteint pas encore";
            if (mirror.PayRefusal(MirrorSystem.MirrorJudgmentCost) is { } refusal) return refusal;
            mirror.ConsumePower(MirrorSystem.MirrorJudgmentCost);
            suspicion.AddMirrorClues(power.Name, Settings.StrikeClues); // what struck its elder? it wonders
            if (effect == "tue")
            {
                power.Elders.Remove(elder);
                ElderSystem.Sync(power);
                ctx.Log.Info($"[Mirror] The Light strikes {elder.Name} of {power.Name} down.");
                ctx.Events.TriggerElderDied(power, elder, false);
            }
            else
            {
                elder.MaxLifespan = System.Math.Max(elder.Age(ctx.Clock.Year) + 1, elder.MaxLifespan - Settings.ElderWoundYears);
                ctx.Log.Info($"[Mirror] The Light wounds {elder.Name} of {power.Name}.");
            }
            return null;
        }
    }
}
