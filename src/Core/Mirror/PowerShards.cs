using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The shards held by powers (LORE.md §11.5, B3c3; the user's decisions of 2026-09-30): three shards lie with three
    /// powers under a « hidden treasure » secret the clan must pierce first. Every means is good — steal it, take it by war,
    /// demand it of a vassal, trade for it — so long as no one suspects anything: an open taking makes the holder wonder why
    /// the clan wanted that treasure (clues on the mirror), a caught thief leaves proof, and a power that knows the mirror
    /// understands at once. A holder absorbed passes its shard to its suzerain, under a secret to pierce again.
    /// </summary>
    public sealed class PowerShards
    {
        private const string TreasureKind = "hidden-treasure";

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SecretBook secrets;
        private readonly SuspicionLedger suspicion;
        private readonly MirrorLore lore;
        private readonly TreatySystem treaties;
        private readonly ResourceManager resources;
        private readonly ShardSystem shards;

        public PowerShards(GameContext ctx, ClanManager clan, FactionManager factions, SecretBook secrets, SuspicionLedger suspicion,
            MirrorLore lore, TreatySystem treaties, ResourceManager resources, ShardSystem shards)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.secrets = secrets;
            this.suspicion = suspicion;
            this.lore = lore;
            this.treaties = treaties;
            this.resources = resources;
            this.shards = shards;

            ctx.Events.OnClanWarWon += TakeAsLoot;
            // The secret book, built first, has already dropped the absorbed power's secrets: a shard left without one was its
            ctx.Events.OnPowerAbsorbed += (vassal, suzerain) =>
            {
                foreach (var shard in PowerShardIds().Where(id => !shards.IsRecovered(id) && SecretOf(id) == null).ToList()) Hide(shard, suzerain);
            };
        }

        private ShardSettings Settings => ctx.Content.Balance.Shards;

        public static Random WorldRandom(int seed) => new Random(unchecked(seed * 6151 + 17));

        private IEnumerable<string> PowerShardIds() => ctx.Content.Shards.Where(s => s.Source == ShardSource.Power).Select(s => s.Id);

        /// <summary>Each power's shard not placed yet goes to a power holding none, drawn by the world's seed.</summary>
        public void Place(Random worldRng)
        {
            foreach (var shard in PowerShardIds().Where(id => SecretOf(id) == null && !shards.IsRecovered(id)).ToList())
            {
                var holders = PowerShardIds().Select(HolderOf).Where(h => h != null).ToHashSet();
                var candidates = factions.Factions.Where(f => !holders.Contains(f.Name)).ToList();
                if (candidates.Count == 0) return;
                Hide(shard, candidates[worldRng.Next(candidates.Count)].Name);
            }
        }

        /// <summary>A shard now lies with a power, under a new « hidden treasure » secret.</summary>
        public Secret Hide(string shardId, string power) => secrets.Create(TreasureKind, power, shardId);

        /// <summary>The newest secret hiding a shard with a power still in the world.</summary>
        private Secret SecretOf(string shardId) =>
            secrets.All.LastOrDefault(s => s.Subject == shardId && s.KindId == TreasureKind && factions.GetFactionByName(s.Holder) != null);

        /// <summary>The power that holds a shard (hidden from the player until the clan pierces its secret), or null.</summary>
        public string HolderOf(string shardId) => shards.IsRecovered(shardId) ? null : SecretOf(shardId)?.Holder;

        public bool KnownByClan(string shardId) =>
            !shards.IsRecovered(shardId) && SecretOf(shardId) is { } secret && secrets.Knows(SecretBook.ClanHolder, secret.Id);

        /// <summary>The shards whose holder the clan knows.</summary>
        public IReadOnlyList<string> KnownToClan() => PowerShardIds().Where(KnownByClan).ToList();

        // ---- Stealing it ----

        public ShardTaking Steal(string shardId, IReadOnlyList<string> teamIds)
        {
            var ids = teamIds ?? new List<string>();
            var team = ids.Distinct().Select(clan.FindById).ToList();
            string refusal = !KnownByClan(shardId) ? "le clan ne sait pas où repose cet éclat"
                : ids.Distinct().Count() != ids.Count ? "un membre ne compte qu'une fois dans l'équipe"
                : team.Count == 0 || team.Count > Settings.ExpeditionMaxTeam ? $"un vol se mène à 1 à {Settings.ExpeditionMaxTeam} membres"
                : team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null) ? "un membre de l'équipe n'est pas libre"
                : team.Any(m => m.Realm < CultivationRealm.QiRefinement) ? "un vol demande des cultivateurs de la Culture du Qi"
                : team.Any(m => m.LastOperationYear == ctx.Clock.Year) ? "un membre de l'équipe a déjà mené une opération cette année"
                : World.TravelRules.Refusal(team, ctx.Content.Clan.HomeRegion, factions.GetFactionByName(HolderOf(shardId))?.RegionId,
                    ctx.Content.Regions, ctx.Content.Balance.Travel);
            if (refusal != null) return ShardTaking.Refused(refusal);

            string holder = HolderOf(shardId);
            foreach (var member in team) member.LastOperationYear = ctx.Clock.Year;
            if (ctx.Rng.Chance(TheftChance(team, factions.GetFactionByName(holder))))
            {
                Take(shardId, holder, 0); // unseen: the loss is a mystery to the holder
                return new ShardTaking(true, true, false, null);
            }
            if (!ctx.Rng.Chance(Settings.TheftCaughtChance)) return new ShardTaking(true, false, false, null);

            suspicion.AddEvidence(holder, Settings.TheftEvidence);
            Wonder(holder, Settings.CaughtClues);
            ctx.Log.Warning($"[Shards] A thief of the clan is caught by {holder}.");
            return new ShardTaking(true, false, true, null);
        }

        /// <summary>A theft's odds against the holder's guard (🔎 balance.json « shards »).</summary>
        public double TheftChance(IReadOnlyList<CharacterData> team, FactionData holder)
        {
            var s = Settings;
            var guardRealm = holder?.HighestRealm ?? CultivationRealm.QiRefinement;
            var gap = ctx.Content.Balance.RealmGap;
            if (!Characters.RealmGap.Reaches(team, guardRealm, gap)) return 0; // numbers do not cross a realm (audit §1)
            double strength = Characters.RealmGap.TeamStrength(team, guardRealm, s.ExpeditionHelpShare, gap);
            double guard = HuntRules.Power(guardRealm, 5);
            return Math.Clamp(s.TheftBaseChance + (strength - guard) * s.TheftChancePerPower, s.TheftMinChance, s.TheftMaxChance);
        }

        // ---- Demanding it of a vassal ----

        /// <summary>A vassal hands the shard over, resents it, and wonders why. Null when done, else why not (French).</summary>
        public string DemandOfVassal(string shardId)
        {
            if (!KnownByClan(shardId)) return "le clan ne sait pas où repose cet éclat";
            string holder = HolderOf(shardId);
            if (!treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain && t.Faction == holder))
                return $"seul un vassal du clan peut être contraint de le livrer, et {holder} ne l'est pas";

            var power = factions.GetFactionByName(holder);
            factions.ChangeRelation(power.ID, -Settings.VassalRelationLoss);
            Take(shardId, holder, Settings.VassalClues);
            return null;
        }

        // ---- Trading for it ----

        public int TradePrice(string shardId) =>
            Math.Max(Settings.TradeMinPrice, (int)(Math.Max(0, factions.GetFactionByName(HolderOf(shardId) ?? "")?.Wealth ?? 0) * Settings.TradePriceShare));

        /// <summary>
        /// The holder sells what it takes for a mere treasure — unless it knows the mirror: then it refuses, and understands.
        /// Null when done, else why not (French).
        /// </summary>
        public string Trade(string shardId)
        {
            if (!KnownByClan(shardId)) return "le clan ne sait pas où repose cet éclat";
            string holder = HolderOf(shardId);
            var power = factions.GetFactionByName(holder);
            if (lore.Knows(holder))
            {
                Wonder(holder, Settings.TradeClues);
                return $"{holder} refuse, et semble soudain très intéressé par le clan";
            }
            if (power.RelationWithPlayer < Settings.TradeMinRelation) return $"{holder} ne veut pas s'en défaire";
            int price = TradePrice(shardId);
            if (!resources.ConsumeSpiritStones(price)) return $"il faut {price} pierres";

            power.Wealth += price;
            Take(shardId, holder, Settings.TradeClues);
            return null;
        }

        // ---- Taking it by war ----

        /// <summary>An enemy that yields hands the shards the clan knows it holds over, drowned in the loot.</summary>
        private void TakeAsLoot(string enemy)
        {
            foreach (var shard in PowerShardIds().Where(id => HolderOf(id) == enemy && KnownByClan(id)).ToList())
                Take(shard, enemy, Settings.WarClues);
        }

        // ---- The mirror's secret ----

        private void Take(string shardId, string holder, int clues)
        {
            if (clues > 0) Wonder(holder, clues);
            ctx.Log.Info($"[Shards] The clan takes the shard {shardId} from {holder}.");
            shards.Recover(shardId);
        }

        /// <summary>The holder wonders why the clan wanted that treasure; one that knows the mirror understands at once.</summary>
        private void Wonder(string holder, int clues)
        {
            int doubt = ctx.Content.Balance.Plots.DoubtClues;
            int now = suspicion.MirrorClues(holder);
            suspicion.AddMirrorClues(holder, lore.Knows(holder) ? Math.Max(clues, doubt - now) : clues);
        }
    }

    /// <summary>What an attempt to take a shard did: whether it happened, whether the shard was taken, whether the clan was caught.</summary>
    public sealed record ShardTaking(bool Launched, bool Taken, bool Caught, string Refusal)
    {
        public static ShardTaking Refused(string why) => new ShardTaking(false, false, false, why);
    }
}
