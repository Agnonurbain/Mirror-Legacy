using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror senses a shard near enough (user decision 2026-10-01). Near is its holder seated in the domain's region or
    /// a neighbouring one, or a bearer of a Talisman Seed come to the holder — a probe, an embassy, a disciple, a captive.
    /// The mirror speaks through the seeds: with no living bearer it is mute, and asleep (integrating a shard) it senses
    /// nothing. Each year, a chance per shard near (more with more bearers); it gives only a direction — the region where
    /// the shard lies — and the clan must still find its holder there.
    /// </summary>
    public sealed class ShardSense
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly ShardSystem shards;
        private readonly PowerShards powerShards;
        private readonly MirrorSystem mirror;
        private readonly Dictionary<string, string> directions = new Dictionary<string, string>();

        public ShardSense(GameContext ctx, ClanManager clan, FactionManager factions, ShardSystem shards, PowerShards powerShards, MirrorSystem mirror)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.shards = shards;
            this.powerShards = powerShards;
            this.mirror = mirror;
            ctx.Events.OnClanProbe += Probed;
        }

        private ShardSettings Settings => ctx.Content.Balance.Shards;

        /// <summary>The shards the mirror has sensed: shard → the region where it lies.</summary>
        public IReadOnlyDictionary<string, string> Directions => directions;

        public void Restore(IReadOnlyDictionary<string, string> saved)
        {
            directions.Clear();
            foreach (var pair in saved ?? new Dictionary<string, string>()) directions[pair.Key] = pair.Value;
        }

        private List<CharacterData> Bearers => clan.LivingMembers.Where(m => m.HasTalismanSeed).ToList();

        /// <summary>A year's sense: every shard near the domain, or near a bearer sent to its holder.</summary>
        public void ProcessYear()
        {
            var bearers = Bearers;
            if (mirror.IsAsleep || bearers.Count == 0) return;
            foreach (var shard in Unsensed())
            {
                string holder = powerShards.HolderOf(shard.Id);
                if ((NearTheDomain(holder) || bearers.Any(b => Visits(b, holder))) && ctx.Rng.Chance(Chance(bearers.Count)))
                    Sense(shard, holder);
            }
        }

        /// <summary>A probe of the clan's: a bearer in its team stands at the holder's door.</summary>
        private void Probed(string target, IReadOnlyList<string> teamIds)
        {
            var bearers = Bearers;
            if (mirror.IsAsleep || !bearers.Any(b => teamIds.Contains(b.ID))) return;
            foreach (var shard in Unsensed().Where(s => powerShards.HolderOf(s.Id) == target).ToList())
                if (ctx.Rng.Chance(Chance(bearers.Count))) Sense(shard, target);
        }

        private IEnumerable<ShardDefinition> Unsensed() =>
            ctx.Content.Shards.Where(s => s.Source == ShardSource.Power && !shards.IsRecovered(s.Id) && !directions.ContainsKey(s.Id)
                && powerShards.HolderOf(s.Id) is { } holder && factions.GetFactionByName(holder) != null && !powerShards.KnownByClan(s.Id)).ToList();

        private double Chance(int bearers) =>
            System.Math.Min(Settings.SenseMaxChance, Settings.SenseChance + Settings.SenseChancePerSeed * (bearers - 1));

        /// <summary>Whether a shard of a power still lies unfound and unsensed: worth sending a bearer abroad.</summary>
        public bool AnyToSeek => Unsensed().Any();

        /// <summary>The power sits in the domain's region or a neighbouring one (or within them): the mirror reaches it from home.</summary>
        public bool NearTheDomain(string holder)
        {
            string home = ctx.Content.Clan.HomeRegion;
            var near = new HashSet<string>(ctx.Content.Regions.FirstOrDefault(r => r.Id == home)?.Neighbours ?? new List<string>()) { home };
            string place = factions.GetFactionByName(holder)?.RegionId;
            for (int hops = 0; place != null && hops < 16; hops++)
            {
                if (near.Contains(place)) return true;
                place = ctx.Content.Regions.FirstOrDefault(r => r.Id == place)?.ParentId;
            }
            return false;
        }

        /// <summary>A bearer at the holder's: its envoy, its disciple, its captive.</summary>
        private static bool Visits(CharacterData bearer, string holder) =>
            bearer.CaptorFaction == holder || bearer.DiscipleOf == holder
            || (bearer.CurrentTask == TaskType.Diplomacy && bearer.DiplomacyTarget == holder);

        private void Sense(ShardDefinition shard, string holder)
        {
            string region = factions.GetFactionByName(holder).RegionId;
            directions[shard.Id] = region;
            ctx.Log.Info($"[Shards] The mirror senses {shard.Name} toward {region}; the bearers of its seeds hear it.");
            ctx.Events.TriggerShardSensed(shard, region);
        }
    }
}
