using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// What a pierced secret is worth (user decision 2026-09-27; D7: profit). The clan may blackmail its holder — a power
    /// that fears the clan and can pay does, once per secret, and resents it; one that towers over it defies it — expose it
    /// to all (every power learns it and distrusts the holder by its rank; the holder hates the clan), or sell it to a
    /// third power (it pays by the rank; the holder may learn who sold it). A power that pierced a secret of the clan and
    /// bears it ill will may expose it: every power learns it, and suspects the clan the more. Each action answers with
    /// its refusal, or null when done.
    /// </summary>
    public sealed class SecretDealings
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly SecretBook book;
        private readonly HashSet<string> spent = new HashSet<string>();

        public SecretDealings(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            SecretBook book)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.book = book;
        }

        private DealingSettings Settings => ctx.Content.Balance.Dealings;
        private static string Clan => SecretBook.ClanHolder;

        /// <summary>What was already spent: a blackmail paid, a secret exposed (saved).</summary>
        public IReadOnlyCollection<string> Spent => spent;

        public void RestoreSpent(IEnumerable<string> saved)
        {
            spent.Clear();
            foreach (var key in saved ?? Enumerable.Empty<string>()) spent.Add(key);
        }

        public bool IsSpent(string secretId) => spent.Contains($"blackmail:{secretId}") || spent.Contains($"exposed:{secretId}");

        private (Secret Secret, FactionData Holder, string Refusal) KnownSecret(string secretId)
        {
            var secret = book.All.FirstOrDefault(s => s.Id == secretId);
            if (secret == null || !book.Knows(Clan, secretId)) return (null, null, "le clan ne connaît pas ce secret");
            var holder = factions.GetFactionByName(secret.Holder);
            return holder == null ? (null, null, "son détenteur n'est plus") : (secret, holder, null);
        }

        private static int ByRank(List<int> table, int rank) => table.Count == 0 ? 0 : table[System.Math.Clamp(rank, 1, table.Count) - 1];

        public string Blackmail(string secretId)
        {
            var (secret, holder, refusal) = KnownSecret(secretId);
            if (refusal != null) return refusal;
            if (IsSpent(secretId)) return "ce secret a déjà servi";
            var s = Settings;
            var strongest = clan.LivingMembers.Where(m => m.CaptorFaction == null).Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if ((int)holder.HighestRealm > (int)strongest + s.FearMargin)
            {
                suspicion.AddToClan(holder.Name, ByRank(s.ResentmentByRank, secret.Rank));
                return $"{holder.Name} vous défie de parler";
            }
            int price = ByRank(s.BlackmailPriceByRank, secret.Rank);
            if (holder.Wealth < price) return $"{holder.Name} ne peut pas payer {price} pierres";
            holder.Wealth -= price;
            resources.AddSpiritStones(price);
            suspicion.AddToClan(holder.Name, ByRank(s.ResentmentByRank, secret.Rank)); // it pays, and resents
            spent.Add($"blackmail:{secretId}");
            ctx.Log.Info($"[Dealings] {holder.Name} buys the clan's silence.");
            return null;
        }

        public string Expose(string secretId)
        {
            var (secret, holder, refusal) = KnownSecret(secretId);
            if (refusal != null) return refusal;
            if (spent.Contains($"exposed:{secretId}")) return "ce secret est déjà connu de tous";
            var s = Settings;
            foreach (var other in factions.Factions.Where(f => f.Name != holder.Name))
            {
                book.Grant(other.Name, secretId);
                suspicion.AddDistrust(other.Name, holder.Name, ByRank(s.ExposeDistrustByRank, secret.Rank));
            }
            factions.ChangeRelation(holder.ID, s.ExposeRelation);
            spent.Add($"exposed:{secretId}");
            ctx.Log.Warning($"[Dealings] The clan exposes a secret of {holder.Name}.");
            return null;
        }

        public string Sell(string secretId, string buyerName)
        {
            var (secret, holder, refusal) = KnownSecret(secretId);
            if (refusal != null) return refusal;
            var buyer = factions.GetFactionByName(buyerName);
            if (buyer == null || buyer == holder) return "acheteur inconnu";
            if (book.Knows(buyer.Name, secretId)) return $"{buyer.Name} le sait déjà";
            int price = ByRank(Settings.SellPriceByRank, secret.Rank);
            if (buyer.Wealth < price) return $"{buyer.Name} ne peut pas payer {price} pierres";
            buyer.Wealth -= price;
            resources.AddSpiritStones(price);
            book.Grant(buyer.Name, secretId);
            if (ctx.Rng.Chance(Settings.SellLeakChance)) suspicion.AddToClan(holder.Name, Settings.SoldResentment); // it learns who sold it
            ctx.Log.Info($"[Dealings] The clan sells a secret of {holder.Name} to {buyer.Name}.");
            return null;
        }

        /// <summary>A power that pierced a secret of the clan and bears it ill will may expose it to all.</summary>
        public void ProcessYear()
        {
            var s = Settings;
            foreach (var secret in book.Of(Clan).Where(x => !spent.Contains($"exposed:{x.Id}")).ToList())
            {
                var teller = factions.Factions.FirstOrDefault(f => f.RelationWithPlayer <= s.AiExposeRelation && book.Knows(f.Name, secret.Id));
                if (teller == null || !ctx.Rng.Chance(s.AiExposeChance)) continue;
                foreach (var other in factions.Factions.Where(f => f != teller))
                {
                    book.Grant(other.Name, secret.Id);
                    suspicion.AddToClan(other.Name, ByRank(s.ExposeDistrustByRank, secret.Rank));
                }
                spent.Add($"exposed:{secret.Id}");
                ctx.Log.Warning($"[Dealings] {teller.Name} exposes a secret of the clan: {secret.KindId}.");
            }
        }
    }
}
