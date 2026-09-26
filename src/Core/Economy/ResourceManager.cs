using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// The clan's treasury: spirit stones, medicinal herbs, spiritual ores, prestige, technique fragments
    /// and portions of spiritual Qi.
    /// </summary>
    public sealed class ResourceManager
    {
        private readonly GameContext ctx;

        public int SpiritStones { get; private set; } = 1000;
        public int MedicinalHerbs { get; private set; } = 50;
        public int SpiritualOres { get; private set; } = 30;
        public int Prestige { get; private set; } = 10;
        public int TechniqueFragments { get; private set; }

        public ResourceManager(GameContext ctx)
        {
            this.ctx = ctx;
        }

        public void AddSpiritStones(int amount)
        {
            if (amount <= 0) return;
            SpiritStones += amount;
            ctx.Events.TriggerSpiritStonesChanged(SpiritStones);
        }

        public bool ConsumeSpiritStones(int amount)
        {
            if (amount <= 0) return true;
            if (SpiritStones < amount)
            {
                ctx.Log.Warning($"[Resources] Not enough spirit stones: need {amount}, have {SpiritStones}.");
                return false;
            }

            SpiritStones -= amount;
            ctx.Events.TriggerSpiritStonesChanged(SpiritStones);
            return true;
        }

        public void SetSpiritStones(int amount)
        {
            SpiritStones = Math.Max(0, amount);
            ctx.Events.TriggerSpiritStonesChanged(SpiritStones);
        }

        public void AddHerbs(int amount) { if (amount > 0) MedicinalHerbs += amount; }
        public bool ConsumeHerbs(int amount) => TryConsume(amount, MedicinalHerbs, v => MedicinalHerbs = v);

        public void AddOres(int amount) { if (amount > 0) SpiritualOres += amount; }
        public bool ConsumeOres(int amount) => TryConsume(amount, SpiritualOres, v => SpiritualOres = v);

        public void AddPrestige(int amount) => Prestige += amount;

        /// <summary>Prayers offered to the mirror (LORE.md §11.5): ten thousand pay for a talisman Qi's ritual.</summary>
        public int Prayers { get; private set; }

        public void AddPrayers(int amount) { if (amount > 0) Prayers += amount; }

        public bool ConsumePrayers(int amount) => TryConsume(amount, Prayers, v => Prayers = v);

        public void RestorePrayers(int prayers) => Prayers = Math.Max(0, prayers);

        private readonly List<CapturedBeast> beasts = new List<CapturedBeast>();

        /// <summary>The spirit beasts the clan captured, awaiting the mirror's ritual.</summary>
        public IReadOnlyList<CapturedBeast> Beasts => beasts;

        public void AddBeast(CapturedBeast beast) { if (beast != null) beasts.Add(beast); }

        public bool ConsumeBeast(CapturedBeast beast) => beast != null && beasts.Remove(beast);

        public void RestoreBeasts(IEnumerable<CapturedBeast> saved)
        {
            beasts.Clear();
            beasts.AddRange((saved ?? Enumerable.Empty<CapturedBeast>()).Where(b => b != null));
        }

        public void AddTechniqueFragments(int amount) { if (amount > 0) TechniqueFragments += amount; }
        public bool ConsumeTechniqueFragments(int amount) => TryConsume(amount, TechniqueFragments, v => TechniqueFragments = v);

        /// <summary>Restores every stock from a save.</summary>
        public void Restore(int spiritStones, int herbs, int ores, int prestige, int techniqueFragments)
        {
            SpiritStones = Math.Max(0, spiritStones);
            MedicinalHerbs = Math.Max(0, herbs);
            SpiritualOres = Math.Max(0, ores);
            Prestige = prestige;
            TechniqueFragments = Math.Max(0, techniqueFragments);
            ctx.Events.TriggerSpiritStonesChanged(SpiritStones);
        }

        // ---- Spiritual Qi (LORE.md §2.5): portions by Qi, and the harvest condensing towards the next one ----

        private readonly Dictionary<string, int> spiritualQi = new Dictionary<string, int>();
        private readonly Dictionary<string, int> qiHarvestProgress = new Dictionary<string, int>();

        /// <summary>Portions in store, by Qi.</summary>
        public IReadOnlyDictionary<string, int> SpiritualQi => spiritualQi;

        /// <summary>Years of harvest work gathered towards the next portion, by Qi.</summary>
        public IReadOnlyDictionary<string, int> QiHarvestProgress => qiHarvestProgress;

        public int QiPortions(string qiId) => qiId != null && spiritualQi.TryGetValue(qiId, out int portions) ? portions : 0;

        public void AddQi(string qiId, int portions)
        {
            if (qiId == null || portions <= 0) return;
            spiritualQi[qiId] = QiPortions(qiId) + portions;
        }

        public bool ConsumeQi(string qiId, int portions)
        {
            if (portions <= 0) return true;
            if (QiPortions(qiId) < portions) return false;
            spiritualQi[qiId] -= portions;
            return true;
        }

        /// <summary>
        /// One year of harvest work on a Qi: wisps accumulate and condense into a portion every
        /// <paramref name="yearsPerPortion"/> years. Returns the portions condensed this year.
        /// </summary>
        public int AddHarvestWork(string qiId, int yearsPerPortion)
        {
            int years = Math.Max(1, yearsPerPortion);
            int progress = (qiHarvestProgress.TryGetValue(qiId, out int p) ? p : 0) + 1;
            qiHarvestProgress[qiId] = progress % years;
            int portions = progress / years;
            AddQi(qiId, portions);
            return portions;
        }

        /// <summary>Restores the Qi in store and the harvests under way from a save.</summary>
        public void RestoreQi(IReadOnlyDictionary<string, int> portions, IReadOnlyDictionary<string, int> harvestProgress)
        {
            spiritualQi.Clear();
            foreach (var kv in portions ?? new Dictionary<string, int>())
                if (kv.Value >= 0) spiritualQi[kv.Key] = kv.Value;
            qiHarvestProgress.Clear();
            foreach (var kv in harvestProgress ?? new Dictionary<string, int>())
                if (kv.Value >= 0) qiHarvestProgress[kv.Key] = kv.Value;
        }

        private static bool TryConsume(int amount, int stock, Action<int> setStock)
        {
            if (amount <= 0) return true;
            if (stock < amount) return false;
            setStock(stock - amount);
            return true;
        }
    }
}
