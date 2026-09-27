using System;
using System.Collections.Generic;

namespace MirrorChronicles.World
{
    /// <summary>
    /// What the powers hold against the clan and against each other, never shown (LORE.md D7): the suspicion each
    /// power bears the clan (0-100), and the distrust one power bears another. Acting without proof is possible, but
    /// the doubt stays and weighs on future alliances and betrayals.
    /// </summary>
    public sealed class SuspicionLedger
    {
        public const int Max = 100;

        private readonly Dictionary<string, int> ofClan = new Dictionary<string, int>();
        private readonly Dictionary<string, int> distrust = new Dictionary<string, int>();

        public int OfClan(string faction) => faction != null && ofClan.TryGetValue(faction, out int v) ? v : 0;

        public void AddToClan(string faction, int amount)
        {
            if (faction == null || amount == 0) return;
            ofClan[faction] = Math.Clamp(OfClan(faction) + amount, 0, Max);
        }

        private readonly Dictionary<string, int> evidence = new Dictionary<string, int>();

        /// <summary>The proof a power holds against the clan (0-100): enough of it makes striking its right.</summary>
        public int Evidence(string faction) => faction != null && evidence.TryGetValue(faction, out int v) ? v : 0;

        public void AddEvidence(string faction, int amount)
        {
            if (faction == null || amount == 0) return;
            evidence[faction] = Math.Clamp(Evidence(faction) + amount, 0, Max);
        }

        public IReadOnlyDictionary<string, int> Evidences => evidence;

        public void RestoreEvidence(IReadOnlyDictionary<string, int> saved)
        {
            evidence.Clear();
            foreach (var pair in saved ?? new Dictionary<string, int>()) evidence[pair.Key] = Math.Clamp(pair.Value, 0, Max);
        }

        private readonly Dictionary<string, int> clues = new Dictionary<string, int>();

        /// <summary>What a power has pieced together about a hidden treasure behind the clan (0-100): the mirror's secret.</summary>
        public int MirrorClues(string faction) => faction != null && clues.TryGetValue(faction, out int v) ? v : 0;

        public void AddMirrorClues(string faction, int amount)
        {
            if (faction == null || amount == 0) return;
            clues[faction] = Math.Clamp(MirrorClues(faction) + amount, 0, Max);
        }

        public IReadOnlyDictionary<string, int> AllMirrorClues => clues;

        public void RestoreMirrorClues(IReadOnlyDictionary<string, int> saved)
        {
            clues.Clear();
            foreach (var pair in saved ?? new Dictionary<string, int>()) clues[pair.Key] = Math.Clamp(pair.Value, 0, Max);
        }

        public int Distrust(string holder, string toward) => distrust.TryGetValue(Key(holder, toward), out int v) ? v : 0;

        public void AddDistrust(string holder, string toward, int amount)
        {
            if (holder == null || toward == null || holder == toward || amount == 0) return;
            distrust[Key(holder, toward)] = Math.Clamp(Distrust(holder, toward) + amount, 0, Max);
        }

        /// <summary>For saves: the clan's suspicions by power, and distrust keyed « holder→toward ».</summary>
        public IReadOnlyDictionary<string, int> ClanSuspicions => ofClan;
        public IReadOnlyDictionary<string, int> Distrusts => distrust;

        public void Restore(IReadOnlyDictionary<string, int> clan, IReadOnlyDictionary<string, int> between)
        {
            ofClan.Clear();
            distrust.Clear();
            foreach (var pair in clan ?? new Dictionary<string, int>()) ofClan[pair.Key] = Math.Clamp(pair.Value, 0, Max);
            foreach (var pair in between ?? new Dictionary<string, int>()) distrust[pair.Key] = Math.Clamp(pair.Value, 0, Max);
        }

        /// <summary>
        /// A power absorbed: its suzerain seizes its archives — the stronger of the two for what each held against the clan,
        /// and the vassal's distrust of others — and nothing is left under the vanished name.
        /// </summary>
        public void Inherit(string from, string to)
        {
            if (from == null || to == null || from == to) return;
            foreach (var ledger in new[] { ofClan, evidence, clues })
            {
                if (ledger.TryGetValue(from, out int held)) ledger[to] = Math.Max(held, ledger.TryGetValue(to, out int own) ? own : 0);
                ledger.Remove(from);
            }
            foreach (var key in new List<string>(distrust.Keys))
            {
                var (holder, toward) = Split(key);
                if (holder != from && toward != from) continue;
                int value = distrust[key];
                distrust.Remove(key);
                if (holder == from && toward != to) distrust[Key(to, toward)] = Math.Max(value, Distrust(to, toward));
            }
        }

        private static string Key(string holder, string toward) => $"{holder}→{toward}";

        private static (string Holder, string Toward) Split(string key)
        {
            int arrow = key.IndexOf('→');
            return arrow < 0 ? (key, null) : (key.Substring(0, arrow), key.Substring(arrow + 1));
        }
    }
}
