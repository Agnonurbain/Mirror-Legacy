using System;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The ancestral bronze mirror, the player. Its power (0-100) recharges each year and with every
    /// breakthrough, and pays for divine interventions.
    /// </summary>
    public sealed class MirrorSystem
    {
        public const int MaxMirrorPower = 100;
        public const int StartingPower = 50;
        public const int YearlyRecharge = 1;
        public const int BreakthroughRecharge = 5;
        public const int AncestralShieldCost = 25;
        public const int MirrorJudgmentCost = 50;
        public const int TalismanSeedCost = 40;
        public const int QiPulseCost = 10;
        private const double QiPulseQiShare = 0.3;
        private const double QiPulseVitalityShare = 0.15;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly BreakthroughSystem breakthroughs;

        public int MirrorPower { get; private set; } = StartingPower;

        /// <summary>Restored shards of the mirror; the restoration axis itself arrives with phase L6.</summary>
        public int RestoredFragments { get; private set; }

        public int TalismanSeedCapacity => SpiritualOrificeRules.TalismanSeedCapacity(RestoredFragments, ctx.Content.Balance.Trials.BaseTalismanSeedCapacity);

        public MirrorSystem(GameContext ctx, ClanManager clan, BreakthroughSystem breakthroughs)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.breakthroughs = breakthroughs;

            ctx.Events.OnYearStarted += year => AddPower(YearlyRecharge);
            ctx.Events.OnBreakthroughSuccess += (c, realm) => AddPower(BreakthroughRecharge);
        }

        public void AddPower(int amount)
        {
            MirrorPower = Math.Clamp(MirrorPower + amount, 0, MaxMirrorPower);
        }

        public bool ConsumePower(int amount)
        {
            if (MirrorPower < amount)
            {
                ctx.Log.Warning($"[Mirror] Not enough power: need {amount}, have {MirrorPower}.");
                return false;
            }

            MirrorPower -= amount;
            return true;
        }

        /// <summary>Cost 10, in battle: 30% of the unit's Qi and 15% of its vitality flow back.</summary>
        public bool UseQiPulse(CombatUnit target)
        {
            if (target == null || !target.IsActive || !ConsumePower(QiPulseCost)) return false;
            target.RestoreQi((int)Math.Round(target.MaxQi * QiPulseQiShare));
            target.Heal((int)Math.Round(target.MaxVitality * QiPulseVitalityShare));
            ctx.Log.Info($"[Mirror] A pulse of Qi steadies {target.BaseData.FullName}.");
            return true;
        }

        /// <summary>Cost 25: +30% on the next breakthrough attempt.</summary>
        public bool UseAncestralShield()
        {
            if (!ConsumePower(AncestralShieldCost)) return false;
            breakthroughs.AncestralShieldActive = true;
            ctx.Log.Info("[Mirror] The Ancestral Shield watches over the next breakthrough.");
            return true;
        }

        /// <summary>Cost 50: strikes a traitor or an enemy down with a Qi deviation.</summary>
        public bool UseMirrorJudgment(CharacterData target)
        {
            if (target == null || !ConsumePower(MirrorJudgmentCost)) return false;
            ctx.Log.Info($"[Mirror] Judgment falls on {target.FullName}!");
            clan.Kill(target, DeathCause.QiDeviation);
            return true;
        }

        /// <summary>
        /// Cost 40: plants a Talisman Seed in a mortal's dantian so they can cultivate without an orifice
        /// (LORE.md §4, §11.5). The mirror sustains only a limited number of active seeds.
        /// </summary>
        public bool GrantTalismanSeed(CharacterData target)
        {
            if (target == null) return false;

            int activeSeeds = clan.LivingMembers.Count(m => m.HasTalismanSeed);
            if (!SpiritualOrificeRules.CanReceiveTalismanSeed(target, activeSeeds, TalismanSeedCapacity))
            {
                ctx.Log.Warning($"[Mirror] {target.FullName} cannot receive a Talisman Seed ({activeSeeds}/{TalismanSeedCapacity} active).");
                return false;
            }
            if (!ConsumePower(TalismanSeedCost)) return false;

            target.HasTalismanSeed = true;
            target.OrificeKnown = true; // the mirror knows what it planted
            ctx.Log.Info($"[Mirror] A Talisman Seed takes root in {target.FullName}.");
            return true;
        }

        public void Restore(int power, int restoredFragments)
        {
            MirrorPower = Math.Clamp(power, 0, MaxMirrorPower);
            RestoredFragments = Math.Max(0, restoredFragments);
        }
    }
}
