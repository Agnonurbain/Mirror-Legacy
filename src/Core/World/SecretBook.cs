using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The book of secrets (user decision 2026-09-27; D7): besides the mirror, the clan and every power hold secrets of
    /// graded importance — the clan's born of its deeds, the powers' drawn with the world. Each watcher gathers clues on a
    /// holder; they peel its secrets from the surface, the least grave first, each rank taking more. A power that pierces
    /// a secret of the clan holds proof against it (the graver, the more). An absorbed power's knowledge passes to its
    /// suzerain; its own secrets die with it. Never shown as figures: the screens show what is known, and signs.
    /// </summary>
    public sealed class SecretBook
    {
        /// <summary>The clan, as a holder or a watcher of secrets.</summary>
        public const string ClanHolder = "@clan";

        private readonly GameContext ctx;
        private readonly SuspicionLedger suspicion;
        private readonly List<Secret> secrets = new List<Secret>();
        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();

        public SecretBook(GameContext ctx, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.suspicion = suspicion;
            ctx.Events.OnDeed += (kind, subject) => Create(kind, ClanHolder, subject);
            ctx.Events.OnPowerAbsorbed += Absorbed;
        }

        private SecretSettings Settings => ctx.Content.Balance.Secrets;

        public IReadOnlyList<Secret> All => secrets;
        public IReadOnlyDictionary<string, int> AllProgress => progress;

        public IReadOnlyList<Secret> Of(string holder) => secrets.Where(s => s.Holder == holder).ToList();

        public int Progress(string watcher, string secretId) => progress.TryGetValue(Key(watcher, secretId), out int v) ? v : 0;

        public bool Knows(string watcher, string secretId)
        {
            var secret = secrets.FirstOrDefault(s => s.Id == secretId);
            return secret != null && Progress(watcher, secretId) >= Threshold(secret.Rank);
        }

        /// <summary>The least grave secret of a holder the watcher does not know yet; null when it knows them all.</summary>
        public Secret NextUnknown(string watcher, string holder) =>
            Of(holder).Where(s => !Knows(watcher, s.Id)).OrderBy(s => s.Rank).ThenBy(s => s.Id, StringComparer.Ordinal).FirstOrDefault();

        public Secret Create(string kindId, string holder, string subject)
        {
            var kind = ctx.Content.SecretKinds.FirstOrDefault(k => k.Id == kindId);
            if (kind == null || holder == null) return null;
            var secret = new Secret($"{kindId}:{holder}:{ctx.Clock.Year}:{secrets.Count}", kindId, holder, kind.Rank, ctx.Clock.Year, subject);
            secrets.Add(secret);
            return secret;
        }

        /// <summary>Clues on a holder, peeling its secrets the least grave first; returns those the watcher comes to know.</summary>
        public IReadOnlyList<Secret> AddClues(string watcher, string holder, int amount)
        {
            var known = new List<Secret>();
            while (amount > 0 && watcher != holder && NextUnknown(watcher, holder) is { } next)
            {
                int missing = Threshold(next.Rank) - Progress(watcher, next.Id);
                int given = Math.Min(missing, amount);
                progress[Key(watcher, next.Id)] = Progress(watcher, next.Id) + given;
                amount -= given;
                if (given < missing) break;
                known.Add(next);
                Learned(watcher, next);
            }
            return known;
        }

        /// <summary>A power that learns a secret of the clan holds proof: the graver, the more.</summary>
        private void Learned(string watcher, Secret secret)
        {
            if (secret.Holder != ClanHolder || watcher == ClanHolder) return;
            int proof = secret.Rank - 1 < Settings.KnownEvidenceByRank.Count ? Settings.KnownEvidenceByRank[secret.Rank - 1] : 0;
            suspicion.AddEvidence(watcher, proof);
            suspicion.AddToClan(watcher, proof);
            ctx.Log.Warning($"[Secrets] {watcher} learns a secret of the clan: {secret.KindId}.");
        }

        public int Threshold(int rank) =>
            Settings.RankThreshold.Count == 0 ? 1 : Settings.RankThreshold[Math.Clamp(rank, 1, Settings.RankThreshold.Count) - 1];

        /// <summary>The world's own source for the powers' secrets (separate from the game's draws).</summary>
        public static Random WorldRandom(int seed) => new Random(unchecked(seed * 7919 + 104729));

        /// <summary>Every power holds a few secrets from the start, drawn in the powers' order: the same seed, the same secrets.</summary>
        public void DrawPowerSecrets(Random worldRng, IEnumerable<FactionData> powers)
        {
            var kinds = ctx.Content.SecretKinds.Where(k => k.Holder != SecretHolder.Clan).ToList();
            if (kinds.Count == 0) return;
            foreach (var power in powers)
            {
                int count = worldRng.Next(Settings.PowerSecretsMin, Settings.PowerSecretsMax + 1);
                foreach (var kind in kinds.OrderBy(_ => worldRng.Next()).Take(count).ToList())
                    secrets.Add(new Secret($"{kind.Id}:{power.Name}:0", kind.Id, power.Name, kind.Rank, 0, null));
            }
        }

        public void Restore(IEnumerable<Secret> saved, IReadOnlyDictionary<string, int> savedProgress)
        {
            secrets.Clear();
            if (saved != null) secrets.AddRange(saved.Where(s => s != null));
            progress.Clear();
            foreach (var pair in savedProgress ?? new Dictionary<string, int>()) progress[pair.Key] = pair.Value;
        }

        /// <summary>An absorbed power: its secrets die with it; what it knew passes to its suzerain.</summary>
        private void Absorbed(string vassal, string suzerain)
        {
            var gone = secrets.Where(s => s.Holder == vassal).Select(s => s.Id).ToHashSet();
            secrets.RemoveAll(s => s.Holder == vassal);
            foreach (var key in progress.Keys.ToList())
            {
                var (watcher, secretId) = Split(key);
                int value = progress[key];
                if (gone.Contains(secretId)) { progress.Remove(key); continue; }
                if (watcher != vassal) continue;
                progress.Remove(key);
                if (secrets.Any(s => s.Id == secretId && s.Holder != suzerain))
                    progress[Key(suzerain, secretId)] = Math.Max(value, Progress(suzerain, secretId));
            }
        }

        private static string Key(string watcher, string secretId) => $"{watcher}→{secretId}";

        private static (string Watcher, string SecretId) Split(string key)
        {
            int arrow = key.IndexOf('→');
            return arrow < 0 ? (key, "") : (key.Substring(0, arrow), key.Substring(arrow + 1));
        }
    }
}
