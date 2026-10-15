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

        /// <summary>Diagnostics (balance, 2026-10-01): stones came in (positive) or went out (negative), and from which system.</summary>
        public event Action<int, string> OnStonesFlow;

        private static string Source(string file, string member) => $"{System.IO.Path.GetFileNameWithoutExtension(file)}.{member}";

        public void AddSpiritStones(int amount,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "")
        {
            if (amount <= 0) return;
            SpiritStones += amount;
            OnStonesFlow?.Invoke(amount, Source(file, member));
            ctx.Events.TriggerSpiritStonesChanged(SpiritStones);
        }

        public bool ConsumeSpiritStones(int amount,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "")
        {
            if (amount <= 0) return true;
            if (SpiritStones < amount)
            {
                ctx.Log.Warning($"[Resources] Not enough spirit stones: need {amount}, have {SpiritStones}.");
                return false;
            }

            SpiritStones -= amount;
            OnStonesFlow?.Invoke(-amount, Source(file, member));
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

        /// <summary>Blood essence for the Heavenly Embryo Devil Dao (R12): from beasts bled and foes struck down.</summary>
        public int BloodEssence { get; private set; }
        public void AddBlood(int amount) { if (amount > 0) BloodEssence += amount; }
        public bool ConsumeBlood(int amount) => TryConsume(amount, BloodEssence, v => BloodEssence = v);
        public void RestoreBlood(int blood) => BloodEssence = Math.Max(0, blood);

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

        /// <summary>The harvest under way is counted in hundredths of a year (save 2.57), so a lineage's sway is not rounded.</summary>
        public const int HarvestUnitsPerYear = 100;

        /// <summary>
        /// One year of harvest work on a Qi: wisps accumulate and condense into a portion every
        /// <paramref name="yearsPerPortion"/> years, the year's work swayed by the Qi's lineage (audit §7.9). Returns the
        /// portions condensed this year.
        /// </summary>
        public int AddHarvestWork(string qiId, int yearsPerPortion, double sway = 1.0)
        {
            int cost = Math.Max(1, yearsPerPortion) * HarvestUnitsPerYear;
            int progress = (qiHarvestProgress.TryGetValue(qiId, out int p) ? p : 0) + (int)Math.Round(Math.Max(0, sway) * HarvestUnitsPerYear);
            qiHarvestProgress[qiId] = progress % cost;
            int portions = progress / cost;
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
