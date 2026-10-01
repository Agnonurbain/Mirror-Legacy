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
    /// breakthrough, and pays for divine interventions — except while its spirit sleeps to integrate a shard
    /// (LORE.md §11.5, B3c).
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

        /// <summary>Restored shards of the mirror (B3c: <see cref="ShardSystem"/>).</summary>
        public int RestoredFragments { get; private set; }

        /// <summary>The year the spirit wakes from integrating a shard; it sleeps before it.</summary>
        public int AsleepUntil { get; private set; }

        public bool IsAsleep => ctx.Clock.Year < AsleepUntil;

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

        /// <summary>Why the mirror cannot pay <paramref name="amount"/> now (French, for the screens), or null when it can.</summary>
        public string PayRefusal(int amount)
        {
            if (amount <= 0) return null;
            if (IsAsleep) return "le miroir dort : il intègre un éclat";
            return MirrorPower < amount ? $"il faut {amount} de puissance du miroir" : null;
        }

        public bool ConsumePower(int amount,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "")
        {
            string refusal = PayRefusal(amount);
            if (refusal != null)
            {
                ctx.Log.Warning($"[Mirror] Cannot pay {amount} (power {MirrorPower}): {refusal}.");
                return false;
            }

            MirrorPower -= amount;
            OnPowerSpent?.Invoke(amount, $"{System.IO.Path.GetFileNameWithoutExtension(file)}.{member}");
            return true;
        }

        /// <summary>Diagnostics (balance, 2026-10-01): the mirror spent its power — how much, and on what.</summary>
        public event System.Action<int, string> OnPowerSpent;

        /// <summary>Cost 10, in battle: 30% of the unit's Qi and 15% of its vitality flow back.</summary>
        public bool UseQiPulse(CombatUnit target)
        {
            if (target == null || !target.IsActive || !ConsumePower(QiPulseCost)) return false;
            target.RestoreQi((int)Math.Round(target.MaxQi * QiPulseQiShare));
            target.Heal((int)Math.Round(target.MaxVitality * QiPulseVitalityShare));
            ctx.Log.Info($"[Mirror] A pulse of Qi steadies {target.BaseData.FullName}.");
            return true;
        }

        /// <summary>Cost 25: +30% on the next breakthrough attempt; once until that attempt.</summary>
        public bool UseAncestralShield()
        {
            if (breakthroughs.AncestralShieldActive || !ConsumePower(AncestralShieldCost)) return false;
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

        /// <summary>A shard restored: the mirror grows, and its spirit sleeps <paramref name="sleepYears"/> to integrate it.</summary>
        public void RestoreShard(int sleepYears)
        {
            RestoredFragments++;
            AsleepUntil = Math.Max(AsleepUntil, ctx.Clock.Year + sleepYears);
        }

        public void Restore(int power, int restoredFragments, int asleepUntil = 0)
        {
            MirrorPower = Math.Clamp(power, 0, MaxMirrorPower);
            RestoredFragments = Math.Max(0, restoredFragments);
            AsleepUntil = asleepUntil;
        }
    }
}
