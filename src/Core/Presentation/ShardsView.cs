using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>What can be done to bring a shard back from where it lies.</summary>
    public enum ShardAction { Expedition, Steal, Demand, Trade, VoidSearch }

    /// <summary>An action offered on a shard: its label (with its odds or price), the team proposed, and why not now.</summary>
    public sealed record ShardActionLine(ShardAction Kind, string Label, IReadOnlyList<string> TeamIds, string Refusal);

    /// <summary>A shard as the mirror's screen shows it: its name, where it is as far as the clan knows, what can be done.</summary>
    public sealed record ShardLine(string Id, string Name, string State, IReadOnlyList<ShardActionLine> Actions);

    /// <summary>
    /// The mirror's shards on its screen (B3e, LORE.md §11.5): how far it is restored, whether it sleeps, and each shard with
    /// the ways at hand — an expedition to known ruins, a theft, a vassal's due or a trade from a power the clan has pierced,
    /// the search of the Great Void. A team is proposed: the strongest free members.
    /// </summary>
    public static class ShardsView
    {
        public static string Header(GameSession s)
        {
            string header = $"Éclats retrouvés : {s.Mirror.RestoredFragments}/{s.Context.Content.Shards.Count}";
            return s.Mirror.IsAsleep ? $"{header} · le miroir dort jusqu'à l'an {s.Mirror.AsleepUntil}" : header;
        }

        public static IReadOnlyList<ShardLine> Lines(GameSession s) =>
            s.Context.Content.Shards.Select(shard => Line(s, shard)).ToList();

        private static ShardLine Line(GameSession s, ShardDefinition shard)
        {
            if (s.Shards.IsRecovered(shard.Id)) return new ShardLine(shard.Id, shard.Name, "retrouvé", new List<ShardActionLine>());
            switch (shard.Source)
            {
                case ShardSource.Lake:
                    return new ShardLine(shard.Id, shard.Name, "au fond du lac : un cultivateur peut le sonder (tâche « Sonder le lac »)", new List<ShardActionLine>());
                case ShardSource.Ruins when s.Shards.RevealedRuins.Contains(shard.Id):
                    return new ShardLine(shard.Id, shard.Name, "dans des ruines anciennes, que le clan connaît", new[] { Expedition(s, shard) });
                case ShardSource.Power when s.PowerShards.KnownByClan(shard.Id):
                    return new ShardLine(shard.Id, shard.Name, $"tenu par {s.PowerShards.HolderOf(shard.Id)}, qui le prend pour un simple trésor", Takings(s, shard));
                case ShardSource.GreatVoid when s.Context.Content.Shards.All(x => x.Id == shard.Id || s.Shards.IsRecovered(x.Id)):
                    return new ShardLine(shard.Id, shard.Name, "dans le Grand Vide : le miroir le sent", new[] { VoidSearch(s) });
                default:
                    return new ShardLine(shard.Id, shard.Name, "inconnu", new List<ShardActionLine>());
            }
        }

        private static ShardActionLine Expedition(GameSession s, ShardDefinition shard)
        {
            var team = BestTeam(s, CultivationRealm.QiRefinement, s.Context.Content.Balance.Shards.ExpeditionMaxTeam);
            string refusal = s.Shards.ExpeditionRefusal(shard.Id, team, out var members);
            string odds = refusal == null ? $" ({Percent(s.Shards.ExpeditionChance(members, shard))})" : "";
            return new ShardActionLine(ShardAction.Expedition, $"Expédition : {Names(members)}{odds}", team, refusal);
        }

        private static IReadOnlyList<ShardActionLine> Takings(GameSession s, ShardDefinition shard)
        {
            var team = BestTeam(s, CultivationRealm.QiRefinement, s.Context.Content.Balance.Shards.ExpeditionMaxTeam);
            var members = team.Select(s.Clan.FindById).ToList();
            var holder = s.Factions.GetFactionByName(s.PowerShards.HolderOf(shard.Id));
            string theftRefusal = team.Count == 0 ? "aucun cultivateur libre cette année" : null;
            string theftOdds = team.Count == 0 ? "" : $" ({Percent(s.PowerShards.TheftChance(members, holder))})";
            bool vassal = s.Treaties.All.Any(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain && t.Faction == holder?.Name);
            int price = s.PowerShards.TradePrice(shard.Id);
            var trade = s.Context.Content.Balance.Shards;
            return new[]
            {
                new ShardActionLine(ShardAction.Steal, $"Le voler : {Names(members)}{theftOdds}", team, theftRefusal),
                new ShardActionLine(ShardAction.Demand, "L'exiger de son vassal", new List<string>(),
                    vassal ? null : $"{holder?.Name} n'est pas un vassal du clan"),
                new ShardActionLine(ShardAction.Trade, $"L'échanger ({price} pierres)", new List<string>(),
                    holder == null || holder.RelationWithPlayer < trade.TradeMinRelation ? $"{holder?.Name} ne veut pas s'en défaire"
                    : s.Resources.SpiritStones < price ? $"il faut {price} pierres" : null)
            };
        }

        private static ShardActionLine VoidSearch(GameSession s)
        {
            var seeker = BestTeam(s, CultivationRealm.PurpleMansion, 1);
            string noSeeker = seeker.Count == 0 ? "aucun membre du Manoir Pourpre n'est libre" : null;
            string label = seeker.Count == 0 ? "Chercher dans le Grand Vide"
                : $"Chercher dans le Grand Vide : {s.Clan.FindById(seeker[0]).FullName} ({Percent(s.Context.Content.Balance.Shards.VoidSearchChance)})";
            string refusal = noSeeker ?? (s.Mirror.IsAsleep ? "le miroir dort : il intègre un éclat" : null);
            return new ShardActionLine(ShardAction.VoidSearch, label, seeker, refusal);
        }

        /// <summary>The strongest free members of at least <paramref name="minRealm"/>, none already out on an operation this year.</summary>
        public static IReadOnlyList<string> BestTeam(GameSession s, CultivationRealm minRealm, int size) =>
            s.Clan.LivingMembers
                .Where(m => m.CaptorFaction == null && m.Realm >= minRealm && m.LastOperationYear != s.Clock.Year)
                .OrderByDescending(m => HuntRules.Power(m.Realm, m.RealmStage)).Take(size).Select(m => m.ID).ToList();

        private static string Names(IEnumerable<CharacterData> members) =>
            string.Join(", ", members.Where(m => m != null).Select(m => m.FullName)) is { Length: > 0 } names ? names : "personne";

        private static string Percent(double chance) => $"{System.Math.Round(chance * 100)} %";
    }
}
