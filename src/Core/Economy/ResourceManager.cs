using System;
using MirrorChronicles.Session;

namespace MirrorChronicles.Economy
{
    /// <summary>
    /// The clan's treasury: spirit stones, medicinal herbs, spiritual ores, prestige and technique fragments.
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

        private static bool TryConsume(int amount, int stock, Action<int> setStock)
        {
            if (amount <= 0) return true;
            if (stock < amount) return false;
            setStock(stock - amount);
            return true;
        }
    }
}
