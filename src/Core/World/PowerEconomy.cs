using System;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers' economy and growth (the living world, step B, user decision 2026-10-01). Each year a power earns by its
    /// size (more for a merchant) and pays its disciples; its size tends toward what its elders can lead — the world as
    /// drawn at first, weighed by their realms: more when an elder rises, less when a great one dies — a little faster for
    /// an expansionist, never without bound (the old « +100 a year » is gone). In debt, it loses disciples.
    /// </summary>
    public sealed class PowerEconomy
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;

        public PowerEconomy(GameContext ctx, FactionManager factions)
        {
            this.ctx = ctx;
            this.factions = factions;
        }

        private PowerEconomySettings Settings => ctx.Content.Balance.PowerEconomy;

        /// <summary>What a power's elders weigh, by their realms.</summary>
        public double Weight(FactionData power) =>
            power.Elders.Sum(e => Settings.RealmWeight[Math.Min((int)e.Realm, Settings.RealmWeight.Length - 1)]);

        /// <summary>The size its elders can lead: its first size, scaled by their weight now against then.</summary>
        public int Capacity(FactionData power) =>
            power.BaselineWeight <= 0 ? power.PowerLevel : (int)(power.BaselinePower * Weight(power) / power.BaselineWeight);

        public void ProcessYear()
        {
            var s = Settings;
            foreach (var power in factions.Factions)
            {
                if (power.BaselineWeight <= 0) // first weighed: the world as drawn is at rest
                {
                    power.BaselineWeight = Math.Max(1, Weight(power));
                    power.BaselinePower = Math.Max(1, power.PowerLevel);
                }
                double temper = s.TemperIncome.TryGetValue(power.Personality, out var t) ? t : 1.0;
                power.Wealth += (int)(power.PowerLevel * (s.IncomePerPower * temper - s.UpkeepPerPower));
                if (power.Wealth < 0)
                {
                    power.PowerLevel -= (int)Math.Ceiling(power.PowerLevel * s.DebtDecline); // it cannot keep its disciples
                    continue;
                }
                double rate = s.GrowthRate * (s.TemperGrowth.TryGetValue(power.Personality, out var g) ? g : 1.0);
                power.PowerLevel += (int)Math.Round((Capacity(power) - power.PowerLevel) * rate);
            }
        }
    }
}
