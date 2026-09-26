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

        private static string Key(string holder, string toward) => $"{holder}→{toward}";
    }
}
